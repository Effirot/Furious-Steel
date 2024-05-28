using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using CharacterSystem.Blocking;
using CharacterSystem.DamageMath;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.TextCore.Text;
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
        ISyncedActivitiesSource
    {
        public delegate void OnCharacterStateChangedDelegate (NetworkCharacter character);
        public delegate void OnCharacterSendDamageDelegate (NetworkCharacter character, Damage damage);
        
        public static event OnCharacterStateChangedDelegate OnCharacterDead = delegate { };
        public static event OnCharacterStateChangedDelegate OnCharacterSpawn = delegate { };

        public const float RotateInterpolationTime = 15f;
        public const float ServerPositionInterpolationTime = 0.07f;
        public const float VelocityReducingMultipliyer = 0.85f;

#region Stats
        [field : Header("Stats")]
        [field : SerializeField, Range (1, 1000)]
        public float maxHealth { get; set; } = 150;
        
        [SerializeField, Range (1, 100)]
        public float regenerationPerSecond = 9;
        

        [field : SerializeField, Range (0.1f, 25f)]
        public virtual float Speed { get; set; } = 11;

        [SerializeField, Range (0.1f, 2.5f)]
        public float Mass = 1f;

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

        [field : Space]
        [field : Header("Team")]
        [field : SerializeField]
        public virtual int TeamIndex { get; private set; }
#endregion
        
        [SerializeField]
        private DecalProjector shadow;

        public event Action<Damage> onDamageRecieved = delegate { };
        public event Action<float> onHealthChanged = delegate { };
        public event Action<bool> isGroundedEvent = delegate { };
        public event Action onJumpEvent = delegate { };
        public event Action<bool> onStunStateChanged = delegate { };

        public bool IsGrounded => characterController.isGrounded;
        public bool isDashing { get; private set; } = false;

        public Damage lastRecievedDamage { get; set; }
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

        public SyncedActivitiesList activities { get; private set; } = new();


        private NetworkVariable<Vector3> network_position = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);    
        private NetworkVariable<Vector2> network_movementVector = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector2> network_lookVector = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
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
            health += damage.value;

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
                direction /= 2;

                speed_acceleration_multipliyer = Vector2.zero;
                velocity = direction;
            }
        }

        public override void OnNetworkSpawn ()
        {
            base.OnNetworkSpawn();

            activities.onSyncedActivityListChanged += HandleActivitiesChanges_Event;

            SetPosition(transform.position);

            if (IsServer)
            {
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
        }
        public override void OnNetworkDespawn ()
        {
            base.OnNetworkDespawn();

            activities.onSyncedActivityListChanged -= HandleActivitiesChanges_Event;
            network_permissions.OnValueChanged -= OnPermissionsChanged;
            
            StopAllCoroutines();
            
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

            gameObject.layer = LayerMask.NameToLayer("Character");

            network_permissions.OnValueChanged += OnPermissionsChanged;
            network_health.OnValueChanged += (Old, New) => onHealthChanged(New);
            network_health.OnValueChanged += (Old, New) => { if (New <= 0) Kill(); };
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
                    stunlock -= Time.fixedDeltaTime;
                }
                else
                {
                    stunlock = 0;
                }

                if (isStunned != stunnedState)
                {
                    onStunStateChanged.Invoke(isStunned);
                }
            }
            
            CalculatePhysicsSimulation();
            CharacterMove(CalculateMovement(Time.fixedDeltaTime / 2)); 

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
                RotateCharacter(Time.fixedDeltaTime);
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
            if (IsClient && CorpsePrefab != null)
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
                    rigidbody.AddForce(velocity * 3000 * UnityEngine.Random.Range(0.5f, 1.5f) + Vector3.up * 500 * UnityEngine.Random.Range(0.5f, 1.5f));
                    rigidbody.AddTorque(
                        Vector3.right * 4000 * UnityEngine.Random.Range(0.5f, 2f) + 
                        Vector3.up * 4000 * UnityEngine.Random.Range(0.5f, 2f) + 
                        Vector3.left * 4000 * UnityEngine.Random.Range(0.5f, 2f));
                }

                Destroy(corpseObject, 10);

                return corpseObject;
            }

            return null;
        }

        private void HandleActivitiesChanges_Event(SyncedActivitiesList.EventType type, SyncedActivity syncedActivity)
        {
            switch (type)
            {
                case SyncedActivitiesList.EventType.Add:
                    permissions = activities.CalculatePermissions();
                    syncedActivity.onPermissionsChanged += HandlePermissionsChanged_Event;
                    Speed -= syncedActivity.SpeedChange;
                    break;
                
                case SyncedActivitiesList.EventType.Remove:
                    permissions = activities.CalculatePermissions();
                    syncedActivity.onPermissionsChanged -= HandlePermissionsChanged_Event;
                    Speed += syncedActivity.SpeedChange;
                    break;
            }
        }
        private void HandlePermissionsChanged_Event(CharacterPermission characterPermission)
        {
            permissions = activities.CalculatePermissions();
        }

        private Vector3 CalculateMovement (float TimeScale)
        {
            var characterMovement = Vector3.zero;

            if (!isStunned && permissions.HasFlag(CharacterPermission.AllowMove))
            {
                speed_acceleration_multipliyer = Vector2.Lerp(
                    speed_acceleration_multipliyer, 
                    Speed <= 0 ? Vector2.zero : movementVector * Mathf.Max(0, Speed) * (characterController.isGrounded ? 1f : 0.3f), 
                    38  * TimeScale);

                characterMovement = isStunned ? Vector3.zero : new Vector3(speed_acceleration_multipliyer.x, 0, speed_acceleration_multipliyer.y) * TimeScale;
            }
            else
            {
                speed_acceleration_multipliyer = Vector2.zero;
            }

            return characterMovement;
        }
        private void CalculatePhysicsSimulation ()
        {
            var velocity = this.velocity;

            velocity.x =  Mathf.Lerp(velocity.x, 0, InterpolateSpeed());
            velocity.y =  permissions.HasFlag(CharacterPermission.AllowGravity) ? 
                            Mathf.Lerp(velocity.y, IsGrounded ? -0.1f : Physics.gravity.y, 0.00085f) : 
                            Mathf.Lerp(velocity.y, 0, 0.12f);
            velocity.z =  Mathf.Lerp(velocity.z, 0, InterpolateSpeed());

            this.velocity = velocity;

            float InterpolateSpeed() => (IsGrounded ? 0.12f : 0.025f) * Mass;
        }
        private void CharacterMove (Vector3 vector)
        {
            if (IsSpawned)
            {
                if(!IsServer)
                {
                    vector += Vector3.Lerp(Vector3.zero, network_position.Value - transform.position, 34f * Time.fixedDeltaTime);
                }

                if (Vector3.Distance(network_position.Value, transform.position) < 1.4f)
                {
                    characterController.Move(vector + velocity);
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
        private void RotateCharacter (float TimeScale)
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
                        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(LookVector), RotateInterpolationTime * TimeScale);
                    }
                }
            }
        }
        private void SetAnimationStates ()
        {
            if (animator.gameObject.activeInHierarchy && animator != null)
            {
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
                health = Mathf.Clamp(health + regenerationPerSecond * Time.fixedDeltaTime, 0, maxHealth);

                yield return waitForFixedUpdateRoutine;
            }

            regenerationCoroutine = null;
        }

        [ServerRpc]
        private void Jump_ServerRpc(Vector2 direction)
        {
            direction.Normalize();
            direction *= Mathf.Max(0, Speed) * Time.fixedDeltaTime * 3f;

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