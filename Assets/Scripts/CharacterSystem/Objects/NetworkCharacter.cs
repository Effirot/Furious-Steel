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
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkCharacter : 
        NetworkBehaviour,
        IDamagable
    {
        public delegate void OnCharacterStateChangedDelegate (NetworkCharacter character);
        public delegate void OnCharacterSendDamageDelegate (NetworkCharacter character, Damage damage);
        
        public static event OnCharacterStateChangedDelegate OnCharacterDead = delegate { };
        public static event OnCharacterStateChangedDelegate OnCharacterSpawn = delegate { };

        new public Rigidbody rigidbody { get; private set; }

        [field : SerializeField]
        public Animator animator { get; private set; }

        [field : SerializeField]
        public virtual float Speed { get; set; } = 7;

        [field : SerializeField]
        public virtual bool IsMoving { get; set; } = true;

        [field : SerializeField]
        public virtual bool StunlockProtection { get; set; } = false;

        [field : SerializeField]
        public VisualEffect OnHitEffect { get; private set; } = null;
        
        [field : SerializeField]
        public VisualEffect StulockEffect { get; private set; } = null;

        [field : SerializeField]
        public float MaxHealth = 300;


        public DamageBlocker Blocker { get; set; } = null;

        [SerializeField]
        public UnityEvent<Damage> OnHitEvent = new();

        public float Health 
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

        public Vector2 MovementVector => network_movementVector.Value.normalized;
        
        public bool IsStunned => Stunlock > 0;
        
        public float Stunlock 
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
                        SetAngle_ClientRpc(transform.rotation.eulerAngles.y);
                    }
                }
            }
        }

        private NetworkVariable<float> network_stunlock = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> network_health = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector3> network_position = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);    
        private NetworkVariable<Vector2> network_movementVector = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private Coroutine regenerationCoroutine;

        private float speed_Multipliyer = 0;

        public void Kill()
        {
            if (IsServer)
            {
                Dead_ClientRpc();

                foreach (var item in GetComponentsInChildren<NetworkObject>())
                {
                    item.Despawn(true);
                }
            }
        }

        protected void SetMovementVector(Vector2 vector)
        {
            if (IsOwner)
            {
                network_movementVector.Value = vector.normalized;
            }
        }

        public virtual void SendDamage(Damage damage)
        {
            if (Blocker != null && Blocker.Block(ref damage))
            {
                return;
            }

            Health -= damage.Value;
            OnHitEvent.Invoke(damage);

            if (Health <= 0 && IsServer && IsSpawned)
            {
                Dead_ClientRpc();

                NetworkObject.Despawn(false);
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
            
            Stunlock = Mathf.Max(damage.Stunlock, Stunlock); 

            rigidbody.AddForce(VecrtorToTarget * damage.PushForce);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                Health = MaxHealth;

                StartCoroutine(StunlockReduceProcess());

                network_position.Value = rigidbody.position;

                SetAngle_ClientRpc(transform.rotation.eulerAngles.y);
            }
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            StopAllCoroutines();

            SwitchOffRigidbodyConstraints();
        }

        protected virtual void Awake()
        {
            rigidbody = GetComponent<Rigidbody>();
        }
        protected virtual void Start()
        {
            
        }
        protected virtual void Update()
        {
            
        }
        protected virtual void FixedUpdate()
        {
            if (Stunlock <= 0 && NetworkManager.Singleton.IsListening)
            {
                CalculateMovement();
                
                RotateCharacter();
            }
        }
        protected virtual void LateUpdate()
        {
            InterpolateToServerPosition();

            animator.SetFloat("Walk_Speed", speed_Multipliyer * Speed);
        }

        protected virtual void Dead()
        {
            OnCharacterDead.Invoke(this);
        }
        protected virtual void Spawn()
        {
            OnCharacterSpawn.Invoke(this);
        }

        private void CalculateMovement()
        {
            if (IsMoving)
            {
                speed_Multipliyer = Mathf.Lerp(speed_Multipliyer, MovementVector.magnitude, 0.12f);

                rigidbody.Move(
                    transform.position + new Vector3(MovementVector.x, 0, MovementVector.y) * (Speed / 100) * speed_Multipliyer,
                    transform.rotation
                );
            }
            else
            {
                speed_Multipliyer = Mathf.Lerp(speed_Multipliyer, 0, 0.1f);
            }
        }
        private void RotateCharacter()
        {
            if (IsMoving)
            {
                var lookVector = new Vector3 (MovementVector.x, 0, MovementVector.y);

                if (lookVector.magnitude > 0.1f)
                {
                    rigidbody.rotation = Quaternion.Lerp(rigidbody.rotation, Quaternion.LookRotation(lookVector), 0.2f);
                }
            }
        }
        private void InterpolateToServerPosition()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                if(IsServer)
                {
                    network_position.Value = rigidbody.position;
                }
                else
                {
                    rigidbody.position = Vector3.Lerp(rigidbody.position, network_position.Value, 0.06f);
                }
            }
        }

        private IEnumerator StunlockReduceProcess()
        {
            while (true)
            {
                Stunlock = Mathf.Clamp(Stunlock - 0.1f, 0, float.MaxValue);

                if (StulockEffect != null)
                {
                    if (IsStunned)
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

            while (Health < MaxHealth)
            {
                Health = Mathf.Clamp(Health + 10, 0, MaxHealth);

                yield return new WaitForSeconds(1f);
            }

            regenerationCoroutine = null;
        }

        protected void SwitchOffRigidbodyConstraints()
        {
            rigidbody.constraints = RigidbodyConstraints.None;
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

            StartCoroutine(DisolveCorpse());

            gameObject.layer = LayerMask.NameToLayer("Untouchable");
        }

        [ClientRpc]
        private void SetAngle_ClientRpc(float angle)
        {
            transform.eulerAngles = new Vector3(0, angle, 0);
        }

    }
}