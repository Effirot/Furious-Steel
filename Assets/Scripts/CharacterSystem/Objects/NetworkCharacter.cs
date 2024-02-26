using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using CharacterSystem.Blocking;
using CharacterSystem.DamageMath;
using JetBrains.Annotations;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.TextCore.Text;
using UnityEngine.VFX;

namespace CharacterSystem.Objects
{
    [Flags]
    public enum CharacterPermission 
    {
        All                 = 0b_1111_1111_0000_0000,
        None                = 0b_0000_0000_0000_0000,

        Untouchable         = 0b_0000_0000_1000_0000,

        AllowMove           = 0b_0000_0001_0000_0000,
        AllowRotate         = 0b_0000_0010_0000_0000,
        AllowGravity        = 0b_0000_0100_0000_0000,
        AllowDash           = 0b_0000_1000_0000_0000,
        
        AllowAttacking      = 0b_0001_0000_0000_0000,
        AllowBlocking       = 0b_0010_0000_0000_0000,
        AllowPowerUps       = 0b_0100_0000_0000_0000,
        AllowUltimate       = 0b_1000_0000_0000_0000,
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class NetworkCharacter : NetworkBehaviour,
        IDamagable
    {
        public delegate void OnCharacterStateChangedDelegate (NetworkCharacter character);
        public delegate void OnCharacterSendDamageDelegate (NetworkCharacter character, Damage damage);
        
        public static event OnCharacterStateChangedDelegate OnCharacterDead = delegate { };
        public static event OnCharacterStateChangedDelegate OnCharacterSpawn = delegate { };


        public const float RotateInterpolationTime = 0.2f;
        public const float ServerPositionInterpolationTime = 0.07f;
        public const float VelocityReducingMultipliyer = 0.85f;

        [Header("Stats")]
        [SerializeField]
        public float maxHealth = 150;
        
        public float regenerationPerSecond = 5;
        

        [field : SerializeField]
        public virtual float Speed { get; set; } = 11;

        [field : SerializeField]
        public float Mass { get; set; } = 1.2f;


        [field : Space]
        [field : Header("Effects")]
        [field : SerializeField]
        public VisualEffect OnHitEffect { get; private set; } = null;

        [field : SerializeField]
        public AudioSource OnHitSound { get; private set; } = null;

        [field : SerializeField]
        public VisualEffect OnHealEffect { get; private set; } = null;
        
        [field : SerializeField]
        public AudioSource OnHealSound { get; private set; } = null;

        [field : SerializeField]
        public VisualEffect StulockEffect { get; private set; } = null;


        public event Action<float> OnHealthChanged = delegate { };
        
        public event Action<float> OnStunlockChanged = delegate { };

        public event Action<bool> IsGrounded = delegate { };

        public Animator animator { get; private set; }
        public CharacterController characterController { get; private set; }

        public CharacterPermission permissions
        {
            get => network_permissions.Value;
            set
            {
                if (IsServer)
                {
                    if (!network_permissions.Value.HasFlag(CharacterPermission.AllowRotate))
                    {   
                        SetAngle(transform.eulerAngles.y);
                    }

                    network_permissions.Value = value;
                }
            }
        }

        public float health 
        { 
            get => network_health.Value; 
            set 
            { 
                if (IsServer)
                {
                    if (network_health.Value > value)
                    {
                        if (regenerationCoroutine != null)
                        {
                            StopCoroutine(regenerationCoroutine);
                        }

                        regenerationCoroutine = StartCoroutine(Regeneration());
                    }

                    network_health.Value = Mathf.Clamp(value, 0, maxHealth);
                }
            } 
        }
        public Vector2 movementVector 
        { 
            get => network_movementVector.Value.normalized;
            set
            {
                if (IsOwner)
                {
                    network_movementVector.Value = value;
                }
            }
        }      
        public Vector2 lookVector 
        { 
            get => network_lookVector.Value.normalized;
            set 
            {
                if (IsOwner)
                {
                    network_lookVector.Value = value;
                }
            }
        }
        
        public bool isStunned => stunlock > 0 || !NetworkManager.Singleton.IsListening;
        public float stunlock 
        {
            get => network_stunlock.Value;
            set 
            {
                if (IsServer)
                {
                    network_stunlock.Value = value;

                    if (value > 0)
                    {
                        speed_Multipliyer = 0;

                        SetAngle(transform.rotation.eulerAngles.y);
                    }
                    else
                    {
                        network_stunlock.Value = 0;
                    }
                }
            }
        }
        public Vector3 velocity 
        {
            get => network_velocity.Value;
            set
            {
                if (IsServer)
                {
                    network_velocity.Value = value;
                }
            }
        }
        

        public bool isInWater { get; private set; } = false;
        public bool isGrounded { get; private set; } = true;

        private NetworkVariable<float> network_stunlock = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> network_health = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        private NetworkVariable<CharacterPermission> network_permissions = new (CharacterPermission.All, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        private NetworkVariable<Vector3> network_position = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);    
        private NetworkVariable<Vector2> network_movementVector = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector2> network_lookVector = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> network_velocity = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);   

        private Coroutine regenerationCoroutine;

        private float speed_Multipliyer = 0;

        // private bool ground_checker => !Physics.OverlapSphere(transform.position, characterController.radius, LayerMask.GetMask("Ground")).Any();


        private List<Collider> collision_buffer = new();

        public void Kill()
        {
            if (IsServer && IsSpawned)
            {
                Dead_ClientRpc();
    
                foreach (var item in GetComponentsInChildren<NetworkObject>()) 
                {
                    if (item != this.NetworkObject && item.IsSpawned)
                    {
                        item.Despawn(true);
                    }
                }

                this.NetworkObject.Despawn(false);
                
                Destroy(gameObject, 5);
            }
        }
        public virtual bool Hit(Damage damage)
        {
            if (!IsSpawned)
                return false;
            
            health -= damage.value;

            if (health <= 0)
            {
                Kill();
            }
            
            if (OnHitEffect != null)
            {
                if (OnHitEffect.HasVector3("Direction"))
                {
                    OnHitEffect.SetVector3("Direction", damage.pushDirection);
                }

                OnHitEffect.Play();
            }

            if (OnHitSound != null)
            {
                OnHitSound.Play();
            }
            
            stunlock = Mathf.Max(damage.stunlock, stunlock); 

            return false;
        }
        public virtual bool Heal(Damage damage)
        {
            health += damage.value;

            if (OnHealEffect != null)
            {
                OnHealEffect.Play();
            }

            if (OnHealSound != null)
            {
                OnHealSound.Play();
            }

            return true;
        }
        public void Push(Vector3 direction)
        {
            if (velocity.magnitude < direction.magnitude)
            {
                velocity = direction;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                health = maxHealth;

                SetAngle(transform.rotation.eulerAngles.y);
                SetPosition(transform.position);
                
                Spawn_ClientRpc();
            }
            else
            {
                transform.position = network_position.Value;
            }

            network_permissions.OnValueChanged += OnPermissionsChanged;
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            network_permissions.OnValueChanged -= OnPermissionsChanged;
            
            StopAllCoroutines();
        }

        public void SetAngle(float angle)
        {
            SetAngle_ClientRpc(angle);
        }
        public void SetPosition(Vector3 position)
        {
            network_position.Value = position;
            transform.position = position;

            SetPosition_ClientRpc(position);
        }

        protected virtual void Awake()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();

            network_health.OnValueChanged += (Old, New) => OnHealthChanged.Invoke(New);
            network_stunlock.OnValueChanged += (Old, New) => OnStunlockChanged.Invoke(New);

        }
        
        protected virtual void FixedUpdate()
        {
            CharacterMove(CalculateMovement() + CalculatePhysicsSimulation()); 
            
            var newGroundedValue = isGrounded;
            if (newGroundedValue != isGrounded)
            {
                IsGrounded.Invoke(newGroundedValue);
            }
            isGrounded = newGroundedValue;

            SetAnimationStates();

            if (isStunned)
            {
                stunlock -= Time.fixedDeltaTime;

                if (stunlock < 0)
                {
                    stunlock = 0;
                }
            }
            else
            {
                RotateCharacter();
            }
        }
        protected virtual void LateUpdate()
        {
            InterpolateToServerPosition();
        }
        protected virtual void OnTriggerEnter(Collider collider)
        {
            if (collider.gameObject.layer == 4)
            {
                isInWater = true;
            }
        }
        protected virtual void OnTriggerExit(Collider collider)
        {
            if (collider.gameObject.layer == 4)
            {
                isInWater = false;
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(network_position.Value, 0.1f);
            Gizmos.DrawRay(network_position.Value, velocity);
            Gizmos.DrawRay(network_position.Value, movementVector);
        }

        protected virtual void OnPermissionsChanged(CharacterPermission Old, CharacterPermission New)
        {
            if (New.HasFlag(CharacterPermission.Untouchable))
            {
                gameObject.layer = LayerMask.NameToLayer("Untouchable");
            }
            else
            {
                gameObject.layer = LayerMask.NameToLayer("Character");
            }
        }

        protected virtual void Dead()
        {

        }
        protected virtual void Spawn()
        {
        }

        private Vector3 CalculateMovement()
        {
            var characterMovement = Vector3.zero;

            if (isStunned)
            {
                speed_Multipliyer = 0;
            }
            else
            {
                if (permissions.HasFlag(CharacterPermission.AllowMove))
                {
                    var waterSpeedReducing = isInWater ? 1.5f : 1;

                    speed_Multipliyer = Mathf.Lerp(speed_Multipliyer, (movementVector.magnitude * Speed) / waterSpeedReducing, 0.12f);

                    if (IsServer)
                    {
                        characterMovement = isStunned ? Vector3.zero : new Vector3(movementVector.x, 0, movementVector.y) * (speed_Multipliyer / 100);
                    }
                }
                else
                {
                    speed_Multipliyer = Mathf.Lerp(speed_Multipliyer, 0, 0.8f);
                }
            }

            return characterMovement;
        }
        private Vector3 CalculatePhysicsSimulation()
        {
            velocity *= VelocityReducingMultipliyer;
            velocity /= Mass;

            Vector3 gravity = Vector3.zero; 
            
            if (permissions.HasFlag(CharacterPermission.AllowGravity))
            {
                gravity = Physics.gravity + (Vector3.up * characterController.velocity.y);
                gravity /= 1.6f;
                gravity *= Time.fixedDeltaTime;
            }

            return velocity + gravity;
        }
        private void CharacterMove(Vector3 vector)
        {
            if (IsSpawned)
            {
                characterController.Move(vector);
            }
        }
        private void RotateCharacter()
        {
            if (permissions.HasFlag(CharacterPermission.AllowRotate))
            {
                var LookVector = new Vector3 (lookVector.x, 0, lookVector.y);

                if (LookVector.magnitude > 0.1f)
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(LookVector), RotateInterpolationTime);
                }
            }
        }
        private void InterpolateToServerPosition()
        {
            if (IsSpawned)
            {
                if(IsServer)
                {
                    network_position.Value = transform.position;
                }
                else
                {
                    characterController.Move(Vector3.Lerp(Vector3.zero, network_position.Value - transform.position, 15f * Time.deltaTime));
                }
            }
        }
        private void SetAnimationStates()
        {
            animator.SetFloat("Walk_Speed", speed_Multipliyer);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsStunned", isStunned);
        }

        private IEnumerator Regeneration()
        {
            yield return new WaitForSeconds(7f);

            var waitForFixedUpdateRoutine = new WaitForFixedUpdate();
            while (health < maxHealth)
            {
                health = Mathf.Clamp(health + regenerationPerSecond * Time.fixedDeltaTime, 0, maxHealth);

                yield return waitForFixedUpdateRoutine;
            }

            regenerationCoroutine = null;
        }

        [ClientRpc]
        private void Spawn_ClientRpc()
        {
            OnCharacterSpawn.Invoke(this);

            Spawn();
        }
        [ClientRpc]
        private void Dead_ClientRpc()
        {            
            OnCharacterDead.Invoke(this);

            Dead();

            gameObject.layer = LayerMask.NameToLayer("Untouchable");
        }

        [ClientRpc]
        private void SetAngle_ClientRpc(float angle)
        {
            transform.eulerAngles = new Vector3(0, angle, 0);
        }
        [ClientRpc]
        private void SetPosition_ClientRpc(Vector3 position)
        {
            transform.position = position;
        }
    }
}