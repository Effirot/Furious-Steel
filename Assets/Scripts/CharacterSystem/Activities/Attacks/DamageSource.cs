using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

namespace CharacterSystem.Attacks
{
    public abstract class DamageSource : SyncedActivities
    {
        public enum AttackTimingStatement
        {
            Waiting,
            BeforeAttack,
            Attack,
            AfterAttack,
        }

        [SerializeField, SerializeReference, SubclassSelector]
        protected Caster[] casters;

        [SerializeField]
        private NetworkVariable<bool> network_isPerforming = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [SerializeField]
        private bool IsInterruptable = false;

        [SerializeField]
        public UnityEvent<Damage> OnHitEvent = new();


        public bool IsAttacking => attackProcess != null;
        public bool IsPerforming 
        { 
            get => network_isPerforming.Value; 
            set 
            {
                if (IsServer)
                {
                    network_isPerforming.Value = value;
                }
            } 
        }

        public float SpeedReducing = 3;

        public AttackTimingStatement currentAttackStatement { get; protected set; }

        private Coroutine attackProcess = null;


        public void StartAttack()
        {
            if (Invoker.permissions.HasFlag(CharacterPermission.AllowAttacking) && !IsAttacking && IsPerforming)
            {
                Invoker.Speed -= SpeedReducing;

                attackProcess = StartCoroutine(AttackProcessRoutine());
            }
        }
        public void EndAttack()
        {
            if (IsAttacking)
            {
                Invoker.Speed += SpeedReducing;

                StopCoroutine(attackProcess);
            }

            attackProcess = null;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            network_isPerforming.OnValueChanged += OnPerformStateChanged;
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            EndAttack();
        }

        protected virtual void OnPerformStateChanged(bool OldValue, bool NewValue)
        {
            if (!NewValue)
            {
                EndAttack();
            }
            else
            {
                if (IsPressed)
                {
                    StartAttack();
                }   
            }
        }
        protected override void OnStateChanged(bool value)
        {
            if (IsPerforming)
            {
                if (value)
                {
                    StartAttack();
                }
                else
                {
                    if (IsInterruptable && currentAttackStatement == AttackTimingStatement.BeforeAttack)
                    {
                        EndAttack();
                    }
                }
            }
        }

        protected abstract IEnumerator AttackProcessRoutine(); 
    

        protected void Execute(float Multiplayer = 1)
        {
            ExecuteInternal_ClientRpc(Multiplayer);

            Execute_Internal(Multiplayer);
        }
        private void Execute_Internal(float Multiplayer)
        {
            if (Invoker.isStunned) return;   

            foreach (var cast in casters)
            {
                foreach (var collider in cast.CastCollider(transform))
                {
                    if (collider.isTrigger)
                        continue;

                    var damage = cast.damage;
                    damage.pushDirection = transform.rotation * cast.damage.pushDirection;
                    damage.sender = Invoker.gameObject;
                    damage *= Multiplayer;

                    Damage.Deliver(collider.gameObject, damage);


                    OnHitEvent.Invoke(damage);
                }
            }
        }

        [ClientRpc]
        private void ExecuteInternal_ClientRpc(float Multiplayer)
        {
            Execute_Internal(Multiplayer);
        }


    }

    
    [Serializable]
    public abstract class Caster 
    {
        public Damage damage;

        public abstract Collider[] CastCollider(Transform transform);
        public abstract void CastColliderGizmos(Transform transform);
    }

    [Serializable]
    public class BoxCaster : Caster
    {
        public Vector3 position = Vector3.zero;
        public Vector3 size = Vector3.one;
        public Vector3 angle = Vector3.zero;

        public override Collider[] CastCollider(Transform transform)
        {
            return Physics.OverlapBox(transform.position + (transform.rotation * position), size, Quaternion.Euler(angle + transform.eulerAngles));
        }

        public override void CastColliderGizmos(Transform transform)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position + (transform.rotation * position), Quaternion.Euler(transform.eulerAngles + angle), Vector3.one);

            Gizmos.DrawWireCube(Vector3.zero, size * 2);
        }
    }
    [Serializable]
    public class SphereCaster : Caster
    {
        public Vector3 position;
        public float radius = 1;

        public override Collider[] CastCollider(Transform transform)
        {
            return Physics.OverlapSphere(transform.position + (transform.rotation * position), radius);
        }

        public override void CastColliderGizmos(Transform transform)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position + (transform.rotation * position), transform.rotation, Vector3.one);

            Gizmos.DrawWireSphere(Vector3.zero, radius);
        }
    }
    [Serializable]
    public class RaycastCaster : Caster
    {
        public Vector3 origin;
        public Vector3 direction;
        public float maxDistance;

        public override Collider[] CastCollider(Transform transform)
        {
            if (Physics.Raycast((transform.rotation * origin) + transform.position, transform.rotation * direction, out var hit, maxDistance))
            {
                return new Collider[] { hit.collider };
            }
            else
            {
                return new Collider[0];
            }
        }

        public override void CastColliderGizmos(Transform transform)
        {
            Gizmos.DrawRay((transform.rotation * origin) + transform.position, (transform.rotation * direction.normalized) * maxDistance);
        }
    }

}
