using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using JetBrains.Annotations;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.TextCore;
using static UnityEngine.InputSystem.InputAction;

namespace CharacterSystem.Attacks
{
    
    public interface IDamageSource : 
        ISyncedActivitiesSource, 
        IDamagable
    {
        new GameObject gameObject { get; }

        float Speed { get; set; }

        event Action<DamageDeliveryReport> OnDamageDelivered;

        void DamageDelivered(DamageDeliveryReport report);
    }

    public class DamageSource : SyncedActivities<IDamageSource>
    {
        public enum AttackTimingStatement
        {
            Waiting,
            BeforeAttack,
            Attack,
            AfterAttack,
        }

        [SerializeField, SerializeReference, SubclassSelector]
        protected AttackQueueElement queueElement;

        [SerializeField]
        private bool IsPerformingAsDefault = false;

        [SerializeField]
        private bool IsInterruptable = false;

        [SerializeField]
        private bool IsInterruptableWhenBlocked = false;

        [SerializeField]
        private UnityEvent<DamageDeliveryReport> OnDamageReport = new ();
        
        private NetworkVariable<bool> network_isPerforming = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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


        public virtual void StartAttack()
        {
            if (Invoker.IsServer && 
                !Invoker.isStunned && 
                Invoker.permissions.HasFlag(CharacterPermission.AllowAttacking) &&
                IsPerforming &&
                !IsAttacking)
            {
                StartAttack_ClientRpc();

                if (!IsHost)
                {
                    StartAttack_Internal();
                }
            }            
        }
        public virtual void EndAttack()
        {
            if (IsServer && !IsAttacking)
            {
                EndAttack_ClientRpc();
                
                EndAttack_Internal();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            network_isPerforming.OnValueChanged += OnPerformStateChanged;
            
            if (IsServer)
            {
                network_isPerforming.Value = IsPerformingAsDefault;
            }
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

        private void OnDrawGizmosSelected()
        {
            queueElement?.OnDrawGizmos(transform);
        }

        private void StartAttack_Internal()
        {
            if (!IsAttacking)
            {
                Invoker.Speed -= SpeedReducing;

                attackProcess = StartCoroutine(AttackSubprocess());
            }
        }
        [ClientRpc]
        private void StartAttack_ClientRpc()
        {
            StartAttack_Internal();
        }

        private void EndAttack_Internal()
        {
            if (IsAttacking)
            {
                Invoker.Speed += SpeedReducing;
                Invoker.permissions = CharacterPermission.All;
            
                StopCoroutine(attackProcess);
                attackProcess = null;
            }
        }
        [ClientRpc]
        private void EndAttack_ClientRpc()
        {
            EndAttack_Internal();
        }
    
        private IEnumerator AttackSubprocess()
        {
            yield return queueElement.AttackPipeline(this);

            EndAttack_Internal();
        }

        internal void HandleDamageReport(DamageDeliveryReport report)
        {
            if (report.isDelivered)
            {
                OnDamageReport.Invoke(report);

                if (report.isBlocked && IsInterruptableWhenBlocked)
                {
                    EndAttack();
                }

                Invoker.DamageDelivered(report);
            }
        }

    }

    [Serializable]
    public abstract class AttackQueueElement
    {
        [Space]
        [SerializeField, TextArea(1, 1)]
        private string Name;
        
        protected void Execute(IEnumerable<Caster> casters, IDamageSource source, float multipliyer)
        {
            var alreadyHitColliders = new List<Collider>();

            foreach (var cast in casters)
            {
                foreach (var collider in cast.CastCollider(source.transform))
                {
                    
                    if (collider.isTrigger)
                        continue;

                    if (alreadyHitColliders.Contains(collider))
                        continue;

                    alreadyHitColliders.Add(collider);

                    var damage = cast.damage;
                    damage.pushDirection = source.transform.rotation * cast.damage.pushDirection;
                    damage.sender = source;
                    damage *= multipliyer;

                    Damage.Deliver(collider.gameObject, damage);
                }
            }

            alreadyHitColliders.Clear();
        }
    
        public abstract IEnumerator AttackPipeline(DamageSource source);

        public abstract void OnDrawGizmos(Transform transform);
    
        public void PlayAnimation(IDamageSource source, string Path)
        {
            source.animator.Play(Path, -1, 1);
        }
    }
    
    [Serializable]
    public sealed class Cast : AttackQueueElement, Charger.IChargeListener
    {
        [Header("Casters")]
        
        [Space]
        [SerializeField, SerializeReference, SubclassSelector]
        private Caster[] casters;

        [Header("Events")]
        
        [Space]
        [SerializeField]
        private string CastAnimationName = "Torso.Attack1";
        [SerializeField]
        private Vector3 CastPushDirection = Vector3.forward / 2; 
        public UnityEvent OnCast = new();
        
        public override IEnumerator AttackPipeline(DamageSource source)
        {
            yield return ChargedAttackPipeline(source, 1);
        }
        public IEnumerator ChargedAttackPipeline(DamageSource source, float chargeValue)
        {
            var invoker = source.Invoker;

            OnCast.Invoke();
            invoker.Push(invoker.transform.rotation * CastPushDirection);
            PlayAnimation(source.Invoker, CastAnimationName);

            Execute(casters, invoker, chargeValue);

            yield break;
        }

        public override void OnDrawGizmos(Transform transform)
        {
            foreach (var caster in casters)
            {
                caster?.CastColliderGizmos(transform);
            }
        }
    }
    [Serializable]
    public sealed class ProjectileShooter : AttackQueueElement
    {
        public GameObject projectilePrefab;

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            if (source.IsServer)
            {
                if (projectilePrefab == null)
                {
                    Debug.LogWarning("Projectile Prefab is null");

                    yield break;
                }

                var projectileObject = GameObject.Instantiate(projectilePrefab, source.transform.position, Quaternion.identity);
                projectileObject.SetActive(true);

                var projectile = projectileObject.GetComponent<Projectile>();
                var projectileNetworkObject = projectileObject.GetComponent<NetworkObject>();
                
                if (projectile == null)
                {
                    Debug.LogWarning("Projectile Prefab does not contains a Projectile Component");

                    yield break;
                }

                projectileNetworkObject.Spawn();
                projectile.MoveDirection = source.transform.forward;
            }
        }

        public override void OnDrawGizmos(Transform transform)
        {
            
        }
    }
    
    [Serializable]
    public sealed class Queue : AttackQueueElement
    {
        [SerializeField, SerializeReference, SubclassSelector]
        private AttackQueueElement[] queue;

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            foreach (var queueElement in queue)
            {
                yield return queueElement.AttackPipeline(source);
            }
        }

        public override void OnDrawGizmos(Transform transform)
        {
            foreach (var queueElement in queue)
            {
                queueElement?.OnDrawGizmos(transform);
            }
        }
    }
    [Serializable]
    public sealed class Repeat : AttackQueueElement
    {
        [SerializeField, SerializeReference, SubclassSelector]
        private AttackQueueElement queueElement;

        [SerializeField, Range(1, 50)]
        private int RepeatCount = 5;

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            for (int i = 0; i < RepeatCount; i++)
            {
                yield return queueElement.AttackPipeline(source);
            }
        }

        public override void OnDrawGizmos(Transform transform)
        {
            queueElement.OnDrawGizmos(transform);
        }
    }
    [Serializable]
    public sealed class Wait : AttackQueueElement
    {
        [SerializeField, Range(0, 10)]
        private float WaitTime = 1;
        
        [SerializeField]
        private CharacterPermission Permissions = CharacterPermission.All;

        [SerializeField]
        private string AnimationName = "";

        [SerializeField]
        private UnityEvent OnStart = new();
        [SerializeField]
        private UnityEvent OnEnd = new();

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            OnStart.Invoke();
            PlayAnimation(source.Invoker, AnimationName);
            source.Invoker.permissions = Permissions;

            yield return new WaitForSeconds(WaitTime);

            OnEnd.Invoke();
        }
        
        public override void OnDrawGizmos(Transform transform)
        {
            
        }
    }

    [Serializable]
    public sealed class Charger : AttackQueueElement
    {
        public interface IChargeListener 
        {
            IEnumerator ChargedAttackPipeline(DamageSource source, float chargeValue);

            void OnDrawGizmos(Transform transform);
        }
        
        [SerializeField]
        private CharacterPermission Permissions = CharacterPermission.All;

        [SerializeField]
        private string AnimationName = "";

        [SerializeField, Range(0, 10)]
        private float MaxChargingTime = 1;
        [SerializeField]
        private float MinChargeValue = 1.25f; 
        [SerializeField]
        private float MaxChargeValue = 1.25f; 

        [SerializeField]
        private UnityEvent OnStart = new ();
        [SerializeField]
        private UnityEvent OnEnd = new ();
        [SerializeField]
        private UnityEvent<float> OnCharge = new ();
        [SerializeField]
        private UnityEvent OnFullCharged = new ();

        [SerializeField, SerializeReference, SubclassSelector]
        private IChargeListener chargeListener; 

        public override IEnumerator AttackPipeline (DamageSource source)
        {
            OnStart.Invoke();
            PlayAnimation(source.Invoker, AnimationName);
            source.Invoker.permissions = Permissions;

            var waitForFixedUpdate = new WaitForFixedUpdate();
            var waitedTime = 0f;

            while (source.IsPressed)
            {
                if (waitedTime < MaxChargingTime)
                {
                    waitedTime += Time.fixedDeltaTime;

                    OnCharge.Invoke(waitedTime);

                    if (waitedTime >= MaxChargingTime)
                    {
                        OnFullCharged.Invoke();
                    }
                }

                yield return waitForFixedUpdate;
            }

            if (chargeListener != null)
            {
                yield return chargeListener.ChargedAttackPipeline(source, Mathf.Lerp(MinChargeValue, MaxChargeValue, waitedTime / MaxChargingTime));
            }

            OnEnd.Invoke();
        }
        
        public override void OnDrawGizmos(Transform transform)
        {
            chargeListener?.OnDrawGizmos(transform);
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

            Gizmos.DrawRay(Vector3.zero, damage.pushDirection);
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

            Gizmos.DrawRay(Vector3.zero, damage.pushDirection);
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
            
            Gizmos.DrawRay(Vector3.zero, damage.pushDirection);
        }
    }
}