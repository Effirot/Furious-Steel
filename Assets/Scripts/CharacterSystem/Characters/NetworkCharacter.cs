using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using CharacterSystem.Blocking;
using CharacterSystem.DamageMath;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CharacterSystem.Objects
{
    [Flags]
    public enum CharacterPermission 
    {
        Default                 = 0b_1111_1111_0000_0000,
        None                = 0b_0000_0000_0000_0000,

        Untouchable         = 0b_0000_0000_1000_0000,

        AllowMove           = 0b_0000_0001_0000_0000,
        AllowRotate         = 0b_0000_0010_0000_0000,
        AllowGravity        = 0b_0000_0100_0000_0000,
        AllowJump           = 0b_0000_1000_0000_0000,
        
        AllowAttacking      = 0b_0001_0000_0000_0000,
        AllowBlocking       = 0b_0010_0000_0000_0000,
        AllowPowerUps       = 0b_0100_0000_0000_0000,
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public abstract class NetworkCharacter : NetworkBehaviour,
        IDamagable,
        ITeammate,
        ISyncedActivitiesSource,
        ITimeScalable,
        IPhysicObject
    {
        public delegate void OnCharacterStateChangedDelegate (NetworkCharacter character);
        public delegate void OnCharacterSendDamageDelegate (NetworkCharacter character, Damage damage);
        
        public static event OnCharacterStateChangedDelegate OnCharacterDead = delegate { };
        public static event OnCharacterStateChangedDelegate OnCharacterSpawn = delegate { };

        public static List<NetworkCharacter> NetworkCharacters { get; } = new();


        public const float RotateInterpolationTime = 37f;
        public const float ServerPositionInterpolationTime = 0.07f;
        public const float VelocityReducingMultipliyer = 0.85f;

#region Stats
        [field : Header("Stats")]
        [field : SerializeField, Range (1, 1000)]
        public float maxHealth { get; set; } = 150;
        
        [SerializeField, Range (1, 100)]
        public float regenerationPerSecond = 9;
        

        [field : SerializeField, Range (0.1f, 25f)]
        public float Speed { get; set; } = 11;


        [field : SerializeField, Range (0.1f, 2.5f)]
        public float mass { get; set; } = 1f;

        [SerializeField]
        public GameObject CorpsePrefab = null;

        [SerializeField]
        public AudioSource OnHitSound;

        [SerializeField]
        public VisualEffect OnHitEffect;

        [Header("Jump")]
        [SerializeField]
        private float JumpForce = 1;

        [SerializeField]
        private int JumpCount = 1;

        [SerializeField]
        private VisualEffect JumpEffect;

        public Team team { get; set; }

#endregion
        [SerializeField]
        private DecalProjector shadow;

        public event Action<Damage> onDamageRecieved = delegate { };
        public event Action<float> onHealthChanged = delegate { };
        public event Action<bool> isGroundedEvent = delegate { };
        public event Action onJumpEvent = delegate { };
        public event Action<bool> onStunStateChanged = delegate { };

        public bool IsGrounded => characterController.isGrounded;

        public virtual float LocalTimeScale { get; set; } = 1;
        
        public Damage lastRecievedDamage { get; set; }
        public Animator animator { get; private set; }
        public CharacterController characterController { get; private set; }
        
        public virtual float CurrentSpeed { 
            get => network_speed.Value; 
            set {
                if (IsServer)
                {
                    network_speed.Value = value;
                }
            }
        }

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
                if (IsServer)
                {
                    network_movementVector.Value = value;
                }
                else
                {
                    if (IsOwner)
                    {
                        SetMovementDirection_ServerRpc(value);
                    }
                }
            }
        }      
        public Vector2 lookVector
        { 
            get => network_lookVector.Value.normalized;
            set 
            {
                if (IsServer)
                {
                    network_lookVector.Value = value;
                }
                else
                {
                    if (IsOwner && permissions.HasFlag(CharacterPermission.AllowRotate))
                    {
                        SetLookDirection_ServerRpc(value);
                    }
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

                    if (value <= 0)
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

        public SyncedActivitiesList activities { get; } = new();
        public float PhysicTimeScale { get; set; } = 1;
        public float GravityScale { get; set; } = 1;

        private NetworkVariable<float> network_speed = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);    
        private NetworkVariable<Vector3> network_position = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);    
        private NetworkVariable<Vector2> network_movementVector = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector2> network_lookVector = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector3> network_velocity = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);   

        private NetworkVariable<CharacterPermission> network_permissions = new (CharacterPermission.Default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<float> network_stunlock = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> network_health = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        private Coroutine regenerationCoroutine;

        private int completeJumpCount = 0;

        private bool isGroundedDeltaState = false;
        private Vector2 speed_acceleration_multipliyer = Vector2.zero;

        private Vector3 resultMovePosition;

        public virtual void Kill ()
        {
            if (IsServer)
            {
                Dead_ClientRpc();

                if (!IsClient)
                {
                    Dead();
                }
    
                foreach (var item in GetComponentsInChildren<NetworkObject>().Reverse()) 
                {
                    Destroy(item.gameObject);
                }

                Destroy(gameObject);
            }
        }
        public virtual bool Hit (Damage damage)
        {
            if (!IsSpawned)
                return false;

            Push(damage.pushDirection);

            health -= damage.value;  

            stunlock = Mathf.Max(damage.stunlock, stunlock); 

            if (IsServer)
            {
                OnHit_ClientRpc(damage);

                if (!IsClient)
                {
                    OnHitReaction(damage);
                }
            }

            return false;
        }
        public virtual bool Heal (Damage damage)
        {
            health += Mathf.Abs(damage.value);

            onDamageRecieved?.Invoke(damage);

            OnHeal_ClientRpc(damage);
            if (!IsClient)
            {
                OnHealReaction(damage);
            }

            return true;
        }
        public void Push (Vector3 direction)
        {
            if (direction.magnitude > 0)
            {
                speed_acceleration_multipliyer = Vector2.zero;
                velocity = direction / mass;
            }
        }

        public override void OnNetworkSpawn ()
        {
            base.OnNetworkSpawn();

            activities.onSyncedActivityListChanged += HandleActivitiesChanges_Event;

            SetPosition(transform.position);

            if (IsServer)
            {
                CurrentSpeed = Speed;
                health = maxHealth;

                Spawn_ClientRpc();
            }

            if (IsClient)
            {
                isGroundedEvent += (isGrounded) => 
                {
                    if (isGrounded)
                    {
                        JumpEffect.Play();
                    }
                };
            }

            NetworkCharacters.Add(this);
        }
        public override void OnNetworkDespawn ()
        {
            base.OnNetworkDespawn();

            activities.onSyncedActivityListChanged -= HandleActivitiesChanges_Event;
            network_permissions.OnValueChanged -= OnPermissionsChanged;
            
            StopAllCoroutines();
            
            NetworkCharacters.Remove(this);

            SpawnCorpse();
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
                network_position.Value = position;
                transform.position = position;

                SetPosition_ClientRpc(position);
            }
        }
        public void Jump(Vector2 direction)
        {
            if (IsOwner)
            {
                Jump_ServerRpc(direction);
            }
        }

        protected virtual void OnHitReaction(Damage damage) 
        { 
            onDamageRecieved?.Invoke(damage); 
           
            if (damage.type is Damage.Type.Physics or Damage.Type.Balistic)
            {
                if (OnHitSound != null && OnHitSound.enabled && OnHitSound.gameObject.activeInHierarchy)
                {
                    OnHitSound.Play();
                }
                if (OnHitEffect != null && damage.value > 0)
                {
                    OnHitEffect.SetVector3("Direction", damage.pushDirection);
                    OnHitEffect.Play();
                }
            }
        }
        protected virtual void OnHealReaction(Damage damage) 
        { 
            onDamageRecieved?.Invoke(damage); 
        }


        protected virtual void Awake ()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();

            network_permissions.OnValueChanged += OnPermissionsChanged;
            network_health.OnValueChanged += (Old, New) => onHealthChanged(New);
        }
        protected virtual void Start () { }

        protected virtual void FixedUpdate ()
        {
            SetAnimationStates();

            if (isStunned)
            {
                var stunnedState = isStunned;

                if (isStunned)
                {
                    if (IsGrounded)
                    {
                        stunlock -= Time.fixedDeltaTime * LocalTimeScale;
                    }
                }
                else
                {
                    stunlock = 0;
                }

                if (isStunned != stunnedState)
                {
                    if (isStunned)
                    {
                        movementVector = Vector3.zero;
                    }

                    onStunStateChanged.Invoke(isStunned);
                }
            }
            
            CalculatePhysicsSimulation();
            CharacterMove(CalculateMovement()); 

            if (isGroundedDeltaState != IsGrounded)
            {
                isGroundedEvent.Invoke(IsGrounded);
            }
            isGroundedDeltaState = IsGrounded;

            if (shadow != null)
            {
                if (Physics.Raycast(transform.position, Vector3.down, out var hit, LayerMask.GetMask("Ground")))
                {
                    shadow.transform.position = hit.point + Vector3.up * 0.1f;
                }
                else
                {
                    shadow.transform.position = transform.position;
                }      

                shadow.fadeFactor = 1 / Vector3.Distance(shadow.transform.position, transform.position);
            }

            if (!isStunned)
            {
                RotateCharacter();
            }
        }       
        protected virtual void Update () { }
        protected virtual void LateUpdate () { }

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
            if (Old.HasFlag(CharacterPermission.AllowMove) && !New.HasFlag(CharacterPermission.AllowMove))
            {
                movementVector = Vector2.zero;
            }

            if (New.HasFlag(CharacterPermission.Untouchable))
            {
                gameObject.layer = LayerMask.NameToLayer("Untouchable");
            }
            else
            {
                gameObject.layer = LayerMask.NameToLayer("Character");
            }
        }
        
        protected virtual void Dead () { }
        protected virtual void Spawn () { }

        protected virtual GameObject SpawnCorpse()
        {
            if (!NetworkManager.ShutdownInProgress && NetworkManager.IsListening && IsClient && CorpsePrefab != null)
            {
                var corpseObject = Instantiate(CorpsePrefab, transform.position, transform.rotation);

                corpseObject.SetActive(true);
                corpseObject.transform.localScale = transform.localScale;
                
                if (corpseObject.TryGetComponent<AudioSource>(out var audio))
                {
                    audio.enabled = true;
                    audio.Play();
                }

                foreach (var rigidbody in corpseObject.GetComponentsInChildren<Rigidbody>())
                {
                    rigidbody.AddForce(velocity * 300 * UnityEngine.Random.Range(0.5f, 1.5f) + Vector3.up * 300 * UnityEngine.Random.Range(0.5f, 1.5f));
                    rigidbody.AddTorque(
                        Vector3.right * 400 * UnityEngine.Random.Range(0.5f, 2f) + 
                        Vector3.up * 400 * UnityEngine.Random.Range(0.5f, 2f) + 
                        Vector3.left * 400 * UnityEngine.Random.Range(0.5f, 2f));
                }

                Destroy(corpseObject, 10);

                return corpseObject;
            }

            return null;
        }

        private void HandleActivitiesChanges_Event(SyncedActivitiesList.EventType type, SyncedActivitySource syncedActivity)
        {
            switch (type)
            {
                case SyncedActivitiesList.EventType.Add:
                    permissions = activities.CalculatePermissions();
                    syncedActivity.onPermissionsChanged += HandlePermissionsChanged_Event;
                    CurrentSpeed -= syncedActivity.SpeedChange;
                    break;
                
                case SyncedActivitiesList.EventType.Remove:
                    permissions = activities.CalculatePermissions();
                    syncedActivity.onPermissionsChanged -= HandlePermissionsChanged_Event;
                    CurrentSpeed += syncedActivity.SpeedChange;
                    break;
            }
        }
        private void HandlePermissionsChanged_Event(CharacterPermission characterPermission)
        {
            permissions = activities.CalculatePermissions();
        }

        private Vector3 CalculateMovement ()
        {
            var characterMovement = Vector3.zero;

            speed_acceleration_multipliyer = Vector2.Lerp(
                speed_acceleration_multipliyer, 
                CurrentSpeed <= 0 ? Vector2.zero : movementVector * Mathf.Max(0, CurrentSpeed) * (characterController.isGrounded ? 1f : 0.3f), 
                20  * Time.fixedDeltaTime * LocalTimeScale);

            return new Vector3(speed_acceleration_multipliyer.x / 2, 0, speed_acceleration_multipliyer.y / 2) * Time.fixedDeltaTime * LocalTimeScale;
        }
        private void CalculatePhysicsSimulation ()
        {
            var velocity = this.velocity;

            velocity.y =  permissions.HasFlag(CharacterPermission.AllowGravity) ? 
                Mathf.Lerp(velocity.y, IsGrounded ? -0.1f : Physics.gravity.y, 0.2f * Time.fixedDeltaTime * LocalTimeScale) : 
                Mathf.Lerp(velocity.y, 0, 0.22f);

            if (IsGrounded || !permissions.HasFlag(CharacterPermission.AllowGravity))
            {
                var interpolateValue = (IsGrounded ? 8 : 2.5f) * mass * Time.fixedDeltaTime * LocalTimeScale * PhysicTimeScale;
                
                velocity.x =  Mathf.Lerp(velocity.x, 0, interpolateValue);
                velocity.z =  Mathf.Lerp(velocity.z, 0, interpolateValue);
            }

            this.velocity = velocity;
        }
        private void CharacterMove (Vector3 vector)
        {
            if (IsSpawned)
            {
                if(!IsServer)
                {
                    vector += Vector3.Lerp(Vector3.zero, network_position.Value - transform.position, 18f * Time.fixedDeltaTime * LocalTimeScale);
                }

                if (Vector3.Distance(network_position.Value, transform.position) < 1.8f)
                {
                    characterController.Move(vector + (velocity * LocalTimeScale * PhysicTimeScale));
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
            var LookVector = new Vector3 (lookVector.x, 0, lookVector.y);

            if (LookVector.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(LookVector), RotateInterpolationTime * Time.fixedDeltaTime * LocalTimeScale);
            }
        }
        private void SetAnimationStates ()
        {
            if (animator.gameObject.activeInHierarchy && animator != null)
            {
                animator.speed = LocalTimeScale;

                var vector = new Vector3(speed_acceleration_multipliyer.x , 0, -speed_acceleration_multipliyer.y); 
                
                vector = transform.rotation * -vector;
                vector += velocity;

                animator.SetFloat("Walk_Speed_X", vector.x);
                animator.SetFloat("Walk_Speed_Y", vector.z);
                animator.SetFloat("Falling_Speed", velocity.y);

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
                health = Mathf.Clamp(health + regenerationPerSecond * Time.fixedDeltaTime * LocalTimeScale, 0, maxHealth);

                yield return waitForFixedUpdateRoutine;
            }

            regenerationCoroutine = null;
        }

        [ServerRpc]
        private void Jump_ServerRpc(Vector2 direction)
        {
            direction.Normalize();
            direction *= Mathf.Max(0, CurrentSpeed) * Time.fixedDeltaTime * LocalTimeScale * 1.5f;

            if (!isStunned && (IsGrounded || completeJumpCount < JumpCount) && permissions.HasFlag(CharacterPermission.AllowJump))
            {
                if (IsGrounded)
                {
                    completeJumpCount = 1;
                }
                else
                {
                    completeJumpCount++;
                }

                Jump_ClientRpc(direction);
                Jump_Internal(direction);
            }
        }
        [ClientRpc]
        private void Jump_ClientRpc(Vector2 direction)
        {      
            if (!IsServer)
            {
                Jump_Internal(direction);
            }    
        }
        private void Jump_Internal(Vector2 direction)
        {     
            var V3direction = new Vector3(direction.x, 0, direction.y);
            
            if (IsClient && JumpEffect != null) {
                JumpEffect.Play();
            }

            V3direction.y = JumpForce;

            Push(V3direction);

            onJumpEvent.Invoke();
        }

        [ClientRpc] 
        private void OnHit_ClientRpc(Damage damage)
        {
            OnHitReaction(damage);
        }
        [ClientRpc]
        private void OnHeal_ClientRpc(Damage damage)
        {
            OnHealReaction(damage);
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
        
        [ServerRpc]
        private void SetMovementDirection_ServerRpc(Vector2 walkDirection)
        {
            if (permissions.HasFlag(CharacterPermission.AllowMove))
            {
                movementVector = walkDirection;
            }
        }
        [ServerRpc]
        private void SetLookDirection_ServerRpc(Vector2 walkDirection)
        {
            if (permissions.HasFlag(CharacterPermission.AllowRotate))
            {
                lookVector = walkDirection;
            }
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