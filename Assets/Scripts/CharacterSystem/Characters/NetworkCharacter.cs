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



#if UNITY_EDITOR
using UnityEditor;
#endif

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
    public abstract class NetworkCharacter : NetworkBehaviour,
        IDamagable,
        ITeammate
    {
        public delegate void OnCharacterStateChangedDelegate (NetworkCharacter character);
        public delegate void OnCharacterSendDamageDelegate (NetworkCharacter character, Damage damage);
        
        public static event OnCharacterStateChangedDelegate OnCharacterDead = delegate { };
        public static event OnCharacterStateChangedDelegate OnCharacterSpawn = delegate { };


        public const float RotateInterpolationTime = 11f;
        public const float ServerPositionInterpolationTime = 0.07f;
        public const float VelocityReducingMultipliyer = 0.85f;

        [Header("Stats")]
        [SerializeField, Range (1, 1000)]
        public float maxHealth = 150;
        
        [SerializeField, Range (1, 100)]
        public float regenerationPerSecond = 9;
        

        [field : SerializeField, Range (0.1f, 25f)]
        public virtual float Speed { get; set; } = 11;

        [field : SerializeField, Range (0.1f, 2.5f)]
        public float Mass { get; set; } = 1f;

        [field : SerializeField]
        public GameObject CorpsePrefab = null;


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


        [field : Space]
        [field : Header("Effects")]
        [field : SerializeField]
        public virtual int TeamIndex { get; private set; }

        
        public event Action<Damage> onDamageRecieved = delegate { };
        public event Action<float> onHealthChanged = delegate { };
        public event Action<bool> isGrounded = delegate { };

        public Animator animator { get; private set; }
        public CharacterController characterController { get; private set; }

        public CharacterPermission permissions
        {
            get => network_permissions.Value;
            set
            {
                if (IsServer)
                {
                    if (!value.HasFlag(CharacterPermission.AllowRotate))
                    {   
                        SetAngle(transform.eulerAngles.y);
                    }
                    if (!value.HasFlag(CharacterPermission.AllowMove))
                    {   
                        SetPosition(transform.position);
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
                        speed_acceleration_multipliyer = 0;

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
        

        private NetworkVariable<Vector3> network_position = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);    
        private NetworkVariable<Vector2> network_movementVector = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector2> network_lookVector = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> network_velocity = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);   

        private NetworkVariable<CharacterPermission> network_permissions = new (CharacterPermission.All, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<float> network_stunlock = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> network_health = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        private Coroutine regenerationCoroutine;

        private float speed_acceleration_multipliyer = 0;

        public virtual void Kill ()
        {
            if (IsServer && IsSpawned)
            {
                Dead_ClientRpc();
                if (!IsClient)
                {
                    Dead();
                }
    
                foreach (var item in GetComponentsInChildren<NetworkObject>()) 
                {
                    if (this.NetworkObject != item && item.IsSpawned)
                    {
                        item.Despawn(true);
                    }
                }

                if (this.NetworkObject.IsSpawned)
                {
                    this.NetworkObject.Despawn(true);
                }
            }
        }
        public virtual bool Hit (Damage damage)
        {
            if (!IsSpawned)
                return false;
            

            health -= damage.value;

            onDamageRecieved?.Invoke(damage);

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

                animator.SetTrigger("OnHit");

                OnHitEffect.Play();
            }

            if (OnHitSound != null && !OnHitSound.isPlaying && !OnHitSound.gameObject.activeInHierarchy)
            {
                OnHitSound.Play();
            }
            
            stunlock = Mathf.Max(damage.stunlock, stunlock); 

            return false;
        }
        public virtual bool Heal (Damage damage)
        {
            health += damage.value;

            onDamageRecieved?.Invoke(damage);

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
        public void Push (Vector3 direction)
        {
            if (velocity.magnitude < direction.magnitude)
            {
                velocity = direction;
            }
        }

        public override void OnNetworkSpawn ()
        {
            base.OnNetworkSpawn();

            SetAngle(transform.rotation.eulerAngles.y);
            SetPosition(transform.position);

            if (IsServer)
            {
                health = maxHealth;

                Spawn_ClientRpc();
            }
        }
        public override void OnNetworkDespawn ()
        {
            base.OnNetworkDespawn();

            network_permissions.OnValueChanged -= OnPermissionsChanged;
            
            StopAllCoroutines();
        }

        public void SetAngle (float angle)
        {
            if (IsServer)
            {
                transform.eulerAngles = new Vector3(0, angle, 0);

                SetAngle_ClientRpc(angle);
            }

        }
        public void SetPosition (Vector3 position)
        {
            if (IsServer)
            {
                transform.position = position;
                network_position.Value = position;

                SetPosition_ClientRpc(position);
            }
        }

        protected virtual void Awake ()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();

            network_permissions.OnValueChanged += OnPermissionsChanged;

            network_health.OnValueChanged += (Old, New) => onHealthChanged(New);
        }

        protected virtual void FixedUpdate ()
        {
            SetAnimationStates();

            if (isStunned)
            {
                stunlock -= Time.fixedDeltaTime;

                if (stunlock < 0)
                {
                    stunlock = 0;
                }
            }

            CharacterMove(CalculateMovement() + CalculatePhysicsSimulation()); 
        }
        protected virtual void Update () { }
        protected virtual void LateUpdate ()
        {
            if (!isStunned)
            {
                RotateCharacter();
            }
        }

        protected virtual void OnValidate () { }
        protected virtual void OnTriggerEnter (Collider collider) { }
        protected virtual void OnTriggerExit (Collider collider) { }
        protected virtual void OnDrawGizmos () { }
        protected virtual void OnDrawGizmosSelected ()
        {
            Gizmos.DrawWireSphere(network_position.Value, 0.1f);
            Gizmos.DrawRay(network_position.Value, velocity);
            Gizmos.DrawRay(network_position.Value, movementVector);
        }

        protected virtual void OnPermissionsChanged (CharacterPermission Old, CharacterPermission New)
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
        
        protected virtual void Dead () 
        { 
            if (IsClient && CorpsePrefab != null)
            {
                var corpseObject = Instantiate(CorpsePrefab, transform.position, transform.rotation);
                corpseObject.transform.localScale = transform.localScale;

                Destroy(corpseObject, 10);
            }
        }
        protected virtual void Spawn () { }

        private Vector3 CalculateMovement ()
        {
            var characterMovement = Vector3.zero;

            if (isStunned)
            {
                speed_acceleration_multipliyer = 0;
            }
            else
            {
                if (permissions.HasFlag(CharacterPermission.AllowMove))
                {
                    speed_acceleration_multipliyer = Mathf.Lerp(speed_acceleration_multipliyer, Speed <= 0 ? 0 : movementVector.magnitude * Speed, 0.12f);

                    if (IsServer)
                    {
                        characterMovement = isStunned ? Vector3.zero : new Vector3(movementVector.x, 0, movementVector.y) * (speed_acceleration_multipliyer / 100);
                    }
                }
                else
                {
                    speed_acceleration_multipliyer = Mathf.Lerp(speed_acceleration_multipliyer, 0, 0.8f);
                }
            }

            return characterMovement;
        }
        private Vector3 CalculatePhysicsSimulation ()
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
        private void CharacterMove (Vector3 vector)
        {
            if (IsSpawned)
            {
                if(!IsServer)
                {
                    vector += Vector3.Lerp(Vector3.zero, network_position.Value - transform.position, 10f * Time.fixedDeltaTime);
                }

                if (Vector3.Distance(network_position.Value, transform.position) < 1f)
                {
                    characterController.Move(vector);
                }
                else
                {
                    transform.position = network_position.Value;
                }

                if (IsServer)
                {
                    network_position.Value = transform.position;
                }
            }
        }
        private void RotateCharacter ()
        {
            if (permissions.HasFlag(CharacterPermission.AllowRotate))
            {
                var LookVector = new Vector3 (lookVector.x, 0, lookVector.y);

                if (LookVector.magnitude > 0.1f)
                {
                    if (IsServer)
                    {
                        transform.rotation = Quaternion.LookRotation(LookVector);
                    }
                    else
                    {
                        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(LookVector), RotateInterpolationTime * Time.deltaTime);
                    }
                }
            }
        }

        private void SetAnimationStates ()
        {
            if (animator.gameObject.activeInHierarchy && animator != null)
            {
                animator.SetFloat("Walk_Speed", speed_acceleration_multipliyer);
                animator.SetBool("IsGrounded", characterController.isGrounded);
                animator.SetBool("IsStunned", isStunned);
            }
        }

        private IEnumerator Regeneration ()
        {
            yield return new WaitForSeconds(7f);

            var waitForFixedUpdateRoutine = new WaitForFixedUpdate();
            while (health < maxHealth && IsSpawned)
            {
                health = Mathf.Clamp(health + regenerationPerSecond * Time.fixedDeltaTime, 0, maxHealth);

                yield return waitForFixedUpdateRoutine;
            }

            regenerationCoroutine = null;
        }


        [ClientRpc]
        private void Spawn_ClientRpc ()
        {
            OnCharacterSpawn.Invoke(this);

            Spawn();
        }
        [ClientRpc]
        private void Dead_ClientRpc ()
        {            
            OnCharacterDead.Invoke(this);

            Dead();

            gameObject.layer = LayerMask.NameToLayer("Untouchable");
        }

        [ClientRpc]
        private void SetAngle_ClientRpc (float angle)
        {
            transform.eulerAngles = new Vector3(0, angle, 0);
        }
        [ClientRpc]
        private void SetPosition_ClientRpc (Vector3 position, ClientRpcParams rpcParams = default)
        {
            transform.position = position;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof (NetworkCharacter), true)]
    public class NetworkCharacter_Editor : Editor
    {
        private new NetworkCharacter target => base.target as NetworkCharacter;

        public override void OnInspectorGUI()
        {
            if (target.IsServer && target.IsSpawned)
            {
                if (GUILayout.Button("Heal"))
                {
                    target.Heal(new Damage(
                        9999.99f,
                        null,
                        0,
                        Vector3.zero,
                        Damage.Type.Unblockable));
                }

                if (GUILayout.Button("Kill"))
                {
                    target.Kill();
                }
            }

            base.OnInspectorGUI();
        }
    }
#endif
}