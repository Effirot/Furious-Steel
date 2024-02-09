using System;
using System.Collections;
using System.Collections.Generic;
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


        [field : SerializeField]
        public virtual float Speed { get; set; } = 7;

        [field : SerializeField]
        public virtual bool IsMoving { get; set; } = true;

        [field : SerializeField]
        public virtual bool StunlockProtection { get; set; } = false;

        [field : SerializeField]
        public VisualEffect OnHitEffect { get; private set; } = null;

        [field : SerializeField]
        public VisualEffect OnHealEffect { get; private set; } = null;
        
        [field : SerializeField]
        public VisualEffect StulockEffect { get; private set; } = null;

        [SerializeField]
        public float MaxHealth = 300;

        [SerializeField]
        public UnityEvent<Damage> OnHitEvent = new();

        public DamageBlocker Blocker { get; set; }
        public Animator animator { get; private set; }
        public CharacterController characterController { get; private set; }

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

                    network_health.Value = Mathf.Clamp(value, 0, MaxHealth);
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
                if (IsServer && !StunlockProtection)
                {
                    network_stunlock.Value = value;

                    animator.SetBool("Stunned", value > 0);

                    if (value > 0)
                    {
                        speed_Multipliyer = 0;

                        animator.SetFloat("Walk_Speed", 0);
                        SetAngle_ClientRpc(transform.rotation.eulerAngles.y);
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

        private NetworkVariable<float> network_stunlock = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> network_health = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        private NetworkVariable<Vector3> network_position = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);    
        private NetworkVariable<Vector2> network_movementVector = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> network_velocity = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);   

        private Coroutine regenerationCoroutine;

        private float speed_Multipliyer = 0;


        public void Kill()
        {
            if (IsServer)
            {
                Dead_ClientRpc();

    
                foreach (var item in GetComponentsInChildren<NetworkObject>()) 
                {
                    if (item.IsSpawned)
                    {
                        item.Despawn(false);
                    }
                } 
    
                StartCoroutine(DisolveCorpse());
            }
        }
        public virtual void Hit(Damage damage)
        {
            if (Blocker != null && Blocker.Block(ref damage))
            {
                return;
            }

            health -= damage.Value;
            OnHitEvent.Invoke(damage);

            if (health <= 0)
            {
                Kill();
            }

            var VecrtorToTarget = transform.position - damage.Sender.transform.position;
            VecrtorToTarget.Normalize();
            
            if (OnHitEffect != null)
            {
                if (OnHitEffect.HasVector3("Direction"))
                {
                    OnHitEffect.SetVector3("Direction", VecrtorToTarget * damage.PushForce);
                }

                OnHitEffect.Play();
            }
            
            stunlock = Mathf.Max(damage.Stunlock, stunlock); 
        }
        public virtual void Heal(float value)
        {
            health += value;

            if (OnHealEffect != null)
            {
                OnHealEffect.Play();
            }
        }
        
        public void Push(Vector3 direction)
        {
            velocity = direction / 150;
        }

        protected void SetMovementVector(Vector2 vector)
        {
            if (IsOwner)
            {
                network_movementVector.Value = vector.normalized;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                health = MaxHealth;

                StartCoroutine(StunlockReduceProcess());

                network_position.Value = transform.position;

                SetAngle_ClientRpc(transform.rotation.eulerAngles.y);
                SetPosition_ClientRpc(transform.position);
                Spawn_ClientRpc();
            }
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

            network_position.Value = transform.position;
        }
        protected virtual void Start()
        {
            
        }
        protected virtual void Update()
        {
            
        }
        protected virtual void FixedUpdate()
        {
            CharacterMove(CalculateMovement() + CalculatePhysicsSimulation()); 

            if (!isStunned)
            {
                RotateCharacter();
            }
        }
        protected virtual void LateUpdate()
        {
            InterpolateToServerPosition();
        }

        protected virtual void Dead()
        {
            OnCharacterDead.Invoke(this);
        }
        protected virtual void Spawn()
        {
            OnCharacterSpawn.Invoke(this);
        }

        private Vector3 CalculateMovement()
        {
            var characterMovement = Vector3.zero;

            if (IsMoving)
            {
                speed_Multipliyer = Mathf.Lerp(speed_Multipliyer, movementVector.magnitude, 0.12f);

                if (IsServer)
                {
                    characterMovement = isStunned ? Vector3.zero : new Vector3(movementVector.x, 0, movementVector.y) * (Speed / 100) * speed_Multipliyer;
                }
            }
            else
            {
                speed_Multipliyer = Mathf.Lerp(speed_Multipliyer, 0, 0.8f);
            }

            return characterMovement;
        }
        private Vector3 CalculatePhysicsSimulation()
        {
            velocity *= VelocityReducingMultipliyer;

            var gravity = Physics.gravity * Time.fixedDeltaTime;

            return velocity / 2 + gravity;
        }
        private void CharacterMove(Vector3 vector)
        {
            if (IsSpawned)
            {
                characterController.Move(vector);

                if (!isStunned)
                {
                    animator.SetFloat("Walk_Speed", speed_Multipliyer * Speed);
                }
            }
        }
        private void RotateCharacter()
        {
            if (IsMoving)
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
                    characterController.Move(Vector3.Lerp(Vector3.zero, network_position.Value - transform.position, 7f * Time.deltaTime));
                }
            }
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

            while (health < MaxHealth)
            {
                health = Mathf.Clamp(health + 10, 0, MaxHealth);

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
            StopAllCoroutines();
            
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