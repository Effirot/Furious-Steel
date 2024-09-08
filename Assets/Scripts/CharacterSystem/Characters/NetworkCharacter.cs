using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using CharacterSystem.Blocking;
using CharacterSystem.DamageMath;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering.Universal;
using Unity.VisualScripting;
using Unity.Cinemachine;
using Cysharp.Threading.Tasks;
using CharacterSystem.Effects;
using CharacterSystem.Attacks;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CharacterSystem.Objects
{
    [Flags]
    public enum CharacterPermission 
    {
        Default             = 0b_1111_1111_0000_0000,
        None                = 0b_0000_0000_0000_0000,

        Unpushable          = 0b_0000_0000_0100_0000,
        Untouchable         = 0b_0000_0000_1000_0000,

        AllowMove           = 0b_0000_0001_0000_0000,
        AllowRotate         = 0b_0000_0010_0000_0000,
        AllowGravity        = 0b_0000_0100_0000_0000,
        AllowJump           = 0b_0000_1000_0000_0000,
        
        AllowAttacking      = 0b_0001_0000_0000_0000,
        AllowBlocking       = 0b_0010_0000_0000_0000,
        AllowPickUps        = 0b_0100_0000_0000_0000,
        AllowDodge          = 0b_1000_0000_0000_0000,
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public abstract partial class NetworkCharacter : NetworkBehaviour,
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

        public const float CollisionTimeout = 0.1f;

#region Stats

        [Header("Stats")]
        [SerializeField, Range (1, 1000), SyncVar]
        public float maxHealth = 150;
        
        [SerializeField, Range (1, 100)]
        public float regenerationPerSecond = 9;
                
        [Range (0.1f, 25f), SyncVar]
        public float Speed;

        [SerializeField, Range (0.1f, 2.5f), SyncVar]
        public float mass = 1f;

        [SerializeField]
        public GameObject CorpsePrefab = null;

        [Header("Jump")]
        [SerializeField]
        private float JumpForce = 1;

        [SerializeField]
        private int JumpCount = 1;

        public Team team { get; set; }

#endregion

        public event Action<Damage> onDamageRecieved = delegate { };
        public event Action<float> onHealthChanged = delegate { };
        public event Action<bool> isGroundedEvent = delegate { };
        public event Action onJumpEvent = delegate { };
        public event Action<bool> onStunStateChanged = delegate { };
        public event Action<DamageDeliveryReport, DamageDeliveryReport> onWallHit = delegate { };
        
        [SyncVar(hook = nameof(OnPermissionsChanged))]
        public CharacterPermission permissions = CharacterPermission.Default;

        [SyncVar(hook = nameof(OnHealthChanged))]
        public float health;


        [SyncVar]
        public float stunlock;
        [SyncVar]
        public Vector3 velocity = Vector3.zero;   

        public float PhysicTimeScale { get; set; } = 1;
        public float GravityScale { get; set; } = 1;
        
        public bool IsGrounded { get; private set; }

        public virtual float LocalTimeScale { get; set; } = 1;
        
        public Damage lastRecievedDamage { get; set; }
        public Animator animator { get; private set; }
        public CharacterController characterController { get; private set; }

        public bool isStunned => stunlock > 0;

        public SyncedActivitiesList activities { get; } = new();

        public float slopeAngle { get; set; } = 65;
        public ControllerColliderHit controllerCollision { get; private set; } = null;

        public Vector2 movementVector
        { 
            get => network_movementVector.normalized;
            set
            {
                if (isServer)
                {
                    network_movementVector = value;
                    local_move_direction = value;
                }
                else
                {
                    if (isOwned)
                    {
                        local_move_direction = value;

                        SetMovementDirection_Command(value);
                    }
                }
            }
        }
        public Vector2 lookVector
        { 
            get => network_lookVector.normalized;
            set 
            {
                if (isServer)
                {
                    network_lookVector = value;
                }
                else
                {
                    if (isOwned)
                    {
                        SetLookDirection_Command(value);
                    }
                }
            }
        }

        float IDamagable.maxHealth => maxHealth;
        float IDamagable.health { get => health; set => health = value; }
        float IDamagable.stunlock { get => stunlock; set => stunlock = value; }

        CharacterPermission ISyncedActivitiesSource.permissions { get => permissions; set => permissions = value; }

        Vector3 IPhysicObject.velocity { get => velocity; set => velocity = value; }
        float IPhysicObject.mass { get => mass; set => mass = value; }


        [SyncVar]
        private Vector3 network_position = Vector3.zero;    
        [SyncVar]
        private Vector2 network_movementVector = Vector2.zero;
        [SyncVar]
        private Vector2 network_lookVector = Vector2.zero;
        
        private Coroutine regenerationCoroutine;

        private int completeJumpCount = 0;

        private bool isGroundedDeltaState = false;
        private float groundCollisionTimeout = CollisionTimeout;


        private Vector2 speed_acceleration_multipliyer = Vector2.zero;
        
        private Vector2 local_move_direction = Vector2.zero;

        private Vector3 resultMovementValue = Vector3.zero;

        public virtual void Kill (Damage damage)
        {
            if (isServer)
            {
                OnCharacterDead.Invoke(this);

                Dead(damage);

                Dead_ClientRpc(damage);


                foreach (var item in GetComponentsInChildren<NetworkIdentity>().Reverse()) 
                {
                    NetworkServer.Destroy(item.gameObject);
                }

                NetworkServer.Destroy(gameObject);
            }
        }
        public virtual bool Hit (ref Damage damage)
        {
            if (damage.args.Contains(Damage.DamageArgument.REMOVE_STUN))
            {
                stunlock = 0;
            }

            if (!permissions.HasFlag(CharacterPermission.Unpushable))
            {
                Push(damage.pushDirection);
            }

            if (isServerOnly)
            {
                OnHealthChanged(health, health - damage.value);
            }

            health -= damage.value;  
            stunlock = Mathf.Max(damage.stunlock, stunlock); 

            onDamageRecieved?.Invoke(damage);

            if (isServer)
            {
                OnHitReaction_ClientRpc(damage);
                
                OnHitReaction(damage);
            }

            return false;
        }
        public virtual bool Heal (ref Damage damage)
        {
            var newHealth = Mathf.Clamp(health + Mathf.Abs(damage.value), 0, maxHealth);
            
            if (isServerOnly)
            {
                OnHealthChanged(health, newHealth);
            }
            
            health = newHealth;

            onDamageRecieved?.Invoke(damage);

            Push(damage.pushDirection);

            if (isServer)
            {
                OnHealReaction_ClientRpc(damage);
            }

            if (!isClient)
            {
                OnHealReaction(damage);
            }

            return true;
        }
        public void Push (Vector3 direction)
        {
            if (direction.magnitude > 0 && !permissions.HasFlag(CharacterPermission.Unpushable))
            {
                controllerCollision = null;
                groundCollisionTimeout = -1;

                speed_acceleration_multipliyer = Vector2.zero;
                velocity = direction / mass;
            }
        }
        public void StartRegeneration ()
        {
            if (regenerationCoroutine != null)
            {
                StopCoroutine(regenerationCoroutine);
                regenerationCoroutine = null;
            }

            if (regenerationPerSecond >= 0)
            {
                regenerationCoroutine = StartCoroutine(Regeneration());
            }
        }

        [ClientRpc]
        public void SetAngle (float angle)
        {
            transform.eulerAngles = new Vector3(0, angle, 0);
        }
        [ClientRpc]
        public void SetPosition (Vector3 position)
        {
            network_position = position;
            transform.position = position;

            Physics.SyncTransforms();
        }
        
        [Client, Command]
        public void Jump (Vector2 direction)
        {
            controllerCollision = null;
            groundCollisionTimeout = -1;

            direction.Normalize();
            direction *= Mathf.Max(0, Speed / 2) * Time.fixedDeltaTime * LocalTimeScale * 0.9f;

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

                var V3direction = new Vector3(direction.x, 0, direction.y);
            
                V3direction.y = JumpForce;

                Push(V3direction);

                onJumpEvent.Invoke();
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
            }
        }
        protected virtual void OnHealReaction(Damage damage) 
        { 
            onDamageRecieved?.Invoke(damage); 
        }

        public override void OnStartServer()
        {
            transform.position = network_position;
            health = maxHealth;
        }
        public override void OnStartClient()
        {
            base.OnStartClient();

            onDamageRecieved += OnDamageRecieved_Event;
            onStunStateChanged += OnStunned.Invoke;
            isGroundedEvent += OnGrounded.Invoke;
            onJumpEvent += OnJump.Invoke;
            onWallHit += OnWallHit.Invoke;

            NetworkCharacters.Add(this);
        }

        protected virtual void Awake ()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();

            network_position = transform.position;
         
            activities.onSyncedActivityListChanged += HandleActivitiesChanges_Event;
        }
        protected virtual void Start () 
        { 
            if (isServer)
            {
                OnCharacterSpawn.Invoke(this);

                Spawn();

                if (isServer)
                {
                    Spawn_ClientRpc();
                }
            }
        }
        protected virtual void OnDestroy()
        {
            activities.onSyncedActivityListChanged -= HandleActivitiesChanges_Event;
            
            StopAllCoroutines();
            
            NetworkCharacters.Remove(this);

            SpawnCorpse();
        }


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
                        movementVector = Vector2.zero;
                    }

                    onStunStateChanged.Invoke(isStunned);
                }
            }
            
            CalculatePhysicsSimulation();
            resultMovementValue = CalculateMovement(); 

            if (isGroundedDeltaState != IsGrounded)
            {
                isGroundedEvent.Invoke(IsGrounded);

                if (IsGrounded && isStunned)
                {
                    stunlock = 0;
                }
            }
            isGroundedDeltaState = IsGrounded;

            groundCollisionTimeout -= Time.fixedDeltaTime;
            IsGrounded = IsGrounded && groundCollisionTimeout > 0;
            
            if (groundCollisionTimeout <= 0)
            {
                controllerCollision = null;
            }
        }
        protected virtual void Update () 
        {

        }
        protected virtual void LateUpdate () 
        { 
            CharacterMove(resultMovementValue);

            RotateCharacter();
        }

        protected virtual void OnDrawGizmos () { }
        protected virtual void OnDrawGizmosSelected ()
        {
            Gizmos.DrawWireSphere(network_position, 0.1f);
            Gizmos.DrawRay(network_position, velocity);
            Gizmos.DrawRay(network_position, movementVector);
        }

        protected virtual void OnControllerColliderHit(ControllerColliderHit hit)
        {
            controllerCollision = hit;
            groundCollisionTimeout = CollisionTimeout;

            IsGrounded = Vector3.Angle(Vector3.up, hit.normal) <= slopeAngle;

            var WallHitVelocity = velocity;

            if (!hit.gameObject.isStatic && hit.gameObject.TryGetComponent<IPhysicObject>(out var component))
            {
                WallHitVelocity -= this.velocity;
            }
            
            WallHitVelocity.y = 0;

            if (isStunned && !IsGrounded && WallHitVelocity.magnitude > 0.4f)
            {   
                OnWallHitEffect(hit);
            }
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

            if (New.HasFlag(CharacterPermission.Unpushable))
            {
                velocity = Vector3.zero;
            }
        }
        protected virtual void OnHealthChanged(float Old, float New)
        {
            if (Old > New)
            {
                StartRegeneration ();
            }

            onHealthChanged.Invoke(New);
        }


        protected virtual void Dead (Damage damage) { }
        protected virtual void Spawn () { }

        protected virtual void OnWallHitEffect(ControllerColliderHit hit)
        {
            stunlock = 0;

            // Calculate Angle between hit normal and current velocity
            var angleNormal = hit.normal;
            angleNormal.y = 0;
            var angleVelocity = velocity;
            angleVelocity.y = 0;

            float angle = Vector3.Angle(angleNormal, angleVelocity); 

            // Recalculate velocity
            var newVelocity = Quaternion.Euler(0, 180 + angle * 2, 0) * velocity;
            newVelocity.y = Mathf.Abs(newVelocity.y);
            newVelocity.y = Mathf.Max(newVelocity.y, 0.3f);
            
            // Deliver damage
            if (isServer) 
            {
                SetPosition(transform.position + newVelocity / 8f);
            }


            onWallHit(
                // Self Damage 
                Damage.Deliver(this, new Damage(
                    Mathf.Round(velocity.magnitude * 10f), 
                    lastRecievedDamage.senderID, 
                    0.2f, 
                    newVelocity / 1.5f, 
                    Damage.Type.Physics) { args = new Damage.DamageArgument[] { Damage.DamageArgument.WALL_HIT } }),
                
                // Other Damage 
                Damage.Deliver(hit.gameObject, new Damage(
                    Mathf.Round(velocity.magnitude * 10), 
                    lastRecievedDamage.senderID, 
                    0.2f, 
                    Quaternion.Euler(0, 180, 0) * newVelocity, 
                    Damage.Type.Physics) { args = new Damage.DamageArgument[] { Damage.DamageArgument.WALL_HIT } })
            );
        }

        protected virtual GameObject SpawnCorpse()
        {
            if (CorpsePrefab != null && NetworkClient.active)
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
            permissions = activities.CalculatePermissions();

            switch (type)
            {
                case SyncedActivitiesList.EventType.Add:
                    syncedActivity.onPermissionsChanged += HandlePermissionsChanged_Event;
                    Speed -= syncedActivity.SpeedChange;
                    break;
                
                case SyncedActivitiesList.EventType.Remove:
                    if (!activities.Any())
                    {
                        permissions = CharacterPermission.Default;
                    }

                    syncedActivity.onPermissionsChanged -= HandlePermissionsChanged_Event;
                    Speed += syncedActivity.SpeedChange;
                    break;
            }
        }
        private void HandlePermissionsChanged_Event(CharacterPermission characterPermission)
        {
            permissions = activities.CalculatePermissions();
        }

        private Vector3 CalculateMovement ()
        {
            var PhysicTimeScale = Mathf.Max(this.PhysicTimeScale, 0);
            var characterMovement = Vector3.zero;
            var friction = 0F;

            if (controllerCollision != null && controllerCollision.collider != null)
            {
                friction = controllerCollision.collider.material.dynamicFriction;
            }

            var newValue = !permissions.HasFlag(CharacterPermission.AllowMove) ? 
                Vector2.zero : 
                (isLocalPlayer && !isServer ? local_move_direction : movementVector) * Mathf.Max(0, Speed) * (characterController.isGrounded ? 1f : 0.6f) * PhysicTimeScale;

            speed_acceleration_multipliyer = Vector2.Lerp(
                speed_acceleration_multipliyer, 
                newValue, 
                friction * 3 + 2 * Time.fixedDeltaTime * LocalTimeScale);

            return new Vector3(speed_acceleration_multipliyer.x / 2, 0, speed_acceleration_multipliyer.y / 2) * LocalTimeScale;
        }
        private void CalculatePhysicsSimulation ()
        {
            var velocity = this.velocity;
            var PhysicTimeScale = Mathf.Max(this.PhysicTimeScale, 0);
            var timescale = Time.fixedDeltaTime * Time.timeScale * LocalTimeScale * PhysicTimeScale;

            velocity.y =  permissions.HasFlag(CharacterPermission.AllowGravity) ? 
                Mathf.Lerp(velocity.y, IsGrounded ? -0.1f : Physics.gravity.y, 0.23f * timescale) : 
                Mathf.Lerp(velocity.y, 0, 16f * timescale);

            if (controllerCollision != null && controllerCollision.collider != null)
            {
                var interpolateValue = 
                    (controllerCollision.collider.material.dynamicFriction) * mass * timescale;
                
                velocity.x =  Mathf.Lerp(velocity.x, IsGrounded ? 0 : controllerCollision.normal.x * 0.2f, interpolateValue);
                velocity.z =  Mathf.Lerp(velocity.z, IsGrounded ? 0 : controllerCollision.normal.z * 0.2f, interpolateValue);
            }

            this.velocity = velocity;
        }
        private void CharacterMove (Vector3 vector)
        {
            if(!isServer)
            {
                vector += Vector3.Lerp(Vector3.zero, network_position - transform.position, 15f * Time.deltaTime * LocalTimeScale);
            }

            if (Vector3.Distance(network_position, transform.position) < 1.8f)
            {
                var PhysicTimeScale = Mathf.Max(this.PhysicTimeScale, 0);

                characterController.Move(vector * Time.deltaTime + (50f * velocity * LocalTimeScale * PhysicTimeScale * Time.deltaTime));
            }
            else
            {
                transform.position = network_position;
            }

            network_position = transform.position;
        }
        private void RotateCharacter ()
        {
            if (isStunned)
            {
                // var newVelocity = velocity;
                // newVelocity.y = 0;

                // if (newVelocity.magnitude > 0)
                // {
                //     transform.rotation = Quaternion.LookRotation(-newVelocity);
                // }
            }
            else
            {
                if (permissions.HasFlag(CharacterPermission.AllowRotate))
                {
                    var LookVector = new Vector3 (lookVector.x, 0, lookVector.y);

                    if (LookVector.magnitude > 0)
                    {
                        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(LookVector), RotateInterpolationTime * Time.fixedDeltaTime * LocalTimeScale);
                    }
                }
            }
        }
        private void SetAnimationStates ()
        {
            if (animator != null && animator.gameObject.activeInHierarchy)
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
            while (health < maxHealth)
            {
                health = Mathf.Clamp(health + regenerationPerSecond * Time.fixedDeltaTime * LocalTimeScale, 0, maxHealth);

                yield return waitForFixedUpdateRoutine;
            }

            regenerationCoroutine = null;
        }

        [ClientRpc]
        private void OnHitReaction_ClientRpc(Damage damage)
        {
            if (!isServer)
            {
                OnHitReaction(damage);
            }
        }
        [ClientRpc]
        private void OnHealReaction_ClientRpc(Damage damage)
        {
            OnHealReaction(damage);
        }

        [ClientRpc]
        private void Dead_ClientRpc (Damage damage) 
        { 
            if (!isServer)
            {
                OnCharacterDead.Invoke(this);

                Dead(damage);
            }
        }
        [ClientRpc]
        private void Spawn_ClientRpc () 
        { 
            if (!isServer)
            {
                OnCharacterSpawn.Invoke(this);

                Spawn();
            }
        }

        [Client, Command]
        private void SetMovementDirection_Command(Vector2 walkDirection)
        {
            if (permissions.HasFlag(CharacterPermission.AllowMove) && isServer)
            {
                movementVector = walkDirection;
            }
        }
        [Client, Command]
        private void SetLookDirection_Command(Vector2 walkDirection)
        {
            if (isServer)
            {
                lookVector = walkDirection;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof (NetworkCharacter))]
        protected partial class NetworkCharacter_Editor : Editor
        {
            private new NetworkCharacter target => base.target as NetworkCharacter;
            
            private bool effectsFoldState = false;

            private SerializedProperty maxHealth;
            private SerializedProperty regenerationPerSecond;
            private SerializedProperty Speed;
            private SerializedProperty mass;
            private SerializedProperty CorpsePrefab;
            private SerializedProperty JumpForce;
            private SerializedProperty JumpCount;

            public virtual void OnEnable()
            {
                maxHealth             ??= serializedObject.FindProperty("maxHealth");
                regenerationPerSecond ??= serializedObject.FindProperty("regenerationPerSecond");
                Speed                 ??= serializedObject.FindProperty("Speed");
                mass                  ??= serializedObject.FindProperty("mass");
                CorpsePrefab          ??= serializedObject.FindProperty("CorpsePrefab");
                JumpForce             ??= serializedObject.FindProperty("JumpForce");
                JumpCount             ??= serializedObject.FindProperty("JumpCount");
            }
            public override void OnInspectorGUI()
            { 
                if (target.netIdentity?.isServer ?? false)
                {
                    if (GUILayout.Button("Heal"))
                    {
                        var damage = new Damage(
                            9999.99f,
                            null,
                            0,
                            Vector3.zero,
                            Damage.Type.Unblockable);
                        target.Heal(ref damage);
                    }

                    if (GUILayout.Button("Kill"))
                    {
                        target.Kill(new Damage(
                            9999.99f,
                            null,
                            0,
                            Vector3.zero,
                            Damage.Type.Unblockable));
                    }
                }

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(maxHealth);
                EditorGUILayout.PropertyField(regenerationPerSecond);
                EditorGUILayout.PropertyField(Speed);
                EditorGUILayout.PropertyField(mass);
                EditorGUILayout.PropertyField(CorpsePrefab);
                EditorGUILayout.PropertyField(JumpForce);
                EditorGUILayout.PropertyField(JumpCount);

                EditorGUILayout.Space();

                serializedObject.ApplyModifiedProperties();
                
                if (effectsFoldState = EditorGUILayout.Foldout(effectsFoldState, "Effects"))
                {
                    DrawNetworkCharacterEffects();
                }

                EditorGUI.EndChangeCheck();
            }
        }
#endif
    }

}