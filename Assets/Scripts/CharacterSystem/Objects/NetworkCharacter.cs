using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Blocking;
using CharacterSystem.DamageMath;
using JetBrains.Annotations;
using Unity.Netcode;
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
    public class NetworkCharacter : 
        NetworkBehaviour,
        IDamagable
    {


        public delegate void OnCharacterStateChangedDelegate (NetworkCharacter character);
        public delegate void OnCharacterSendDamageDelegate (NetworkCharacter character, Damage damage);
        
        public static event OnCharacterStateChangedDelegate OnCharacterDead = delegate { };
        public static event OnCharacterStateChangedDelegate OnCharacterSpawn = delegate { };


        public const float RotateInterpolationTime = 0.2f;
        public const float ServerPositionInterpolationTime = 0.07f;
        public const float VelocityReducingMultipliyer = 0.85f;

        [Header("Movement")]
        [SerializeField]
        public float maxHealth = 150;

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


        public DamageBlocker Blocker { get; set; }
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
        public Vector2 movementVector => network_movementVector.Value.normalized;      
        public bool isStunned => stunlock > 0 || !NetworkManager.Singleton.IsListening;
        public float stunlock 
        {
            get => network_stunlock.Value;
            set 
            {
                if (IsServer && !permissions.HasFlag(CharacterPermission.All))
                {
                    network_stunlock.Value = value;

                    if (value > 0)
                    {
                        speed_Multipliyer = 0;

                        SetAngle(transform.rotation.eulerAngles.y);
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

        public bool isGrounded { get; private set; } = false;

        private NetworkVariable<float> network_stunlock = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> network_health = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        private NetworkVariable<CharacterPermission> network_permissions = new (CharacterPermission.All, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        private NetworkVariable<Vector3> network_position = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);    
        private NetworkVariable<Vector2> network_movementVector = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> network_velocity = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);   

        private Coroutine regenerationCoroutine;

        private float speed_Multipliyer = 0;

        private bool ground_checker => true;
        // private bool ground_checker => !Physics.OverlapSphere(transform.position, characterController.radius, LayerMask.GetMask("Ground")).Any();


        public void Kill()
        {
            if (IsServer)
            {
                Dead_ClientRpc();
    
                foreach (var item in GetComponentsInChildren<NetworkObject>()) 
                {
                    if (item != NetworkObject && item.IsSpawned)
                    {
                        item.Despawn(true);
                    }
                }

                if (NetworkObject.IsSpawned)
                {
                    NetworkObject.Despawn(false);
                }
    
                StartCoroutine(DisolveCorpse());
            }
        }
        public virtual bool Hit(Damage damage)
        {
            var isBlocked = Blocker != null && Blocker.Block(ref damage);
            

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

            return isBlocked;
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

        protected virtual void SetMovementVector(Vector2 vector)
        {
            if (IsOwner)
            {
                vector.Normalize();

                network_movementVector.Value = vector;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                health = maxHealth;

                StartCoroutine(StunlockReduceProcess());

                network_position.Value = transform.position;

                SetAngle(transform.rotation.eulerAngles.y);
                SetPosition_ClientRpc(transform.position);
                Spawn_ClientRpc();
            }

            transform.position = network_position.Value;

        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            StopAllCoroutines();
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
            
            var newGroundedValue = ground_checker;
            if (newGroundedValue != isGrounded)
            {
                IsGrounded.Invoke(newGroundedValue);
            }
            isGrounded = newGroundedValue;

            CalculateAnimations();

            if (!isStunned)
            {
                RotateCharacter();
            }
        }
        protected virtual void LateUpdate()
        {
            InterpolateToServerPosition();
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(network_position.Value, 0.1f);
            Gizmos.DrawRay(network_position.Value, network_velocity.Value);
            Gizmos.DrawRay(network_position.Value, network_movementVector.Value);
        }

        protected virtual void Dead()
        {
            OnCharacterDead.Invoke(this);
        }
        protected virtual void Spawn()
        {
            OnCharacterSpawn.Invoke(this);
        }

        protected void SetAngle(float angle)
        {
            SetAngle_ClientRpc(angle);
        }
        protected void SetPosition(Vector3 position)
        {
            SetPosition_ClientRpc(position);
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
                    speed_Multipliyer = Mathf.Lerp(speed_Multipliyer, movementVector.magnitude * Speed, 0.12f);

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
                var lookVector = new Vector3 (movementVector.x, 0, movementVector.y);

                if (lookVector.magnitude > 0.1f)
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(lookVector), RotateInterpolationTime);
                }
            }
        }
        private void InterpolateToServerPosition()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                if(IsServer)
                {
                    network_position.Value = transform.position;
                }
                else
                {
                    characterController.Move(Vector3.Lerp(Vector3.zero, network_position.Value - transform.position, 9f * Time.deltaTime));
                }
            }
        }
        private void CalculateAnimations()
        {
            animator.SetBool("Stunned", isStunned);
            animator.SetFloat("Walk_Speed", speed_Multipliyer);
            animator.SetBool("IsGrounded", isGrounded);
        }

        private IEnumerator StunlockReduceProcess()
        {
            while (true)
            {
                stunlock = Mathf.Clamp(stunlock - 0.1f, 0, float.MaxValue);

                if (StulockEffect != null)
                {
                    if (isStunned)
                    {
                        StulockEffect.Play();
                    }
                    else
                    {
                        StulockEffect.Stop();
                    }
                }
                
                yield return new WaitForSeconds(0.1f);

            }
        }
        private IEnumerator DisolveCorpse()
        {
            yield return new WaitForSeconds(5f);

            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
        private IEnumerator Regeneration()
        {
            yield return new WaitForSeconds(7f);

            while (health < maxHealth)
            {
                health = Mathf.Clamp(health + 10, 0, maxHealth);

                yield return new WaitForSeconds(1f);
            }

            regenerationCoroutine = null;
        }

        [ClientRpc]
        private void Spawn_ClientRpc()
        {
            Spawn();
        }
        [ClientRpc]
        private void Dead_ClientRpc()
        {            
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