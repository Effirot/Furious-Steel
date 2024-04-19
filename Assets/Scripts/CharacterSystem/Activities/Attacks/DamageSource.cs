using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
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
        float Speed { get; set; }

        DamageDeliveryReport lastReport { get; set; }

        int Combo { get; }

        event Action<DamageDeliveryReport> OnDamageDelivered;

        void SetPosition (Vector3 position);
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

        [SerializeField]
        private bool IsPerformingAsDefault = false;

        [SerializeField]
        private bool IsInterruptable = false;

        [SerializeField]
        private bool IsInterruptableWhenBlocked = false;

        [SerializeField, Range(0, 10)]
        public float SpeedReducing = 3;
        
        [SerializeField, SerializeReference, SubclassSelector]
        public AttackQueueElement[] attackQueue;

        [SerializeField]
        public UnityEvent<DamageDeliveryReport> OnDamageReport = new ();
        
        [SerializeField]
        public UnityEvent OnAttackEnded = new ();


        public AttackTimingStatement currentAttackStatement { get; protected set; }
        public DamageDeliveryReport currentAttackDamageReport { get; protected set; }

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
        public bool IsAttacking => attackProcess != null;

        public virtual bool IsActive => Invoker.permissions.HasFlag(CharacterPermission.AllowAttacking) && IsPerforming; 

        private NetworkVariable<bool> network_isPerforming = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private Coroutine attackProcess = null;

        public void StartAttackForced()
        {
            if (Invoker.IsServer)
            {
                StartAttack_ClientRpc();

                if (!IsClient)
                {
                    StartAttack_Internal();
                }
            }
        }
        public virtual void StartAttack()
        {
            if (!Invoker.isStunned && 
                Invoker.permissions.HasFlag(CharacterPermission.AllowAttacking) &&
                IsPerforming &&
                !IsAttacking)
            {
                StartAttackForced();
            }            
        }
        public virtual void EndAttack()
        {
            if (IsServer && !IsAttacking)
            {
                EndAttack_ClientRpc();
                
                if (!IsClient)
                {
                    EndAttack_Internal();
                }
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
            if (NewValue)
            {
                if (IsPressed)
                {                    
                    StartAttack();
                }   
            }
            else
            {
                EndAttack();
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

        internal void HandleDamageReport(DamageDeliveryReport report)
        {
            if (report.isDelivered)
            {
                OnDamageReport.Invoke(report);

                if (report.isBlocked && IsInterruptableWhenBlocked)
                {
                    EndAttack();
                }

                currentAttackDamageReport = report;

                Invoker.DamageDelivered(report);
                Invoker.lastReport = report;
            }
        }

        private void OnDrawGizmosSelected()
        {
            foreach (var item in attackQueue)
            {
                item?.OnDrawGizmos(transform);
            }
        }

        private IEnumerator AttackSubprocess()
        {
            foreach (var item in attackQueue)
            {
                yield return item.AttackPipeline(this);
            }

            EndAttack_Internal();
        }

        [ClientRpc]
        private void StartAttack_ClientRpc()
        {
            StartAttack_Internal();
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
        private void EndAttack_ClientRpc()
        {
            EndAttack_Internal();
        }
        private void EndAttack_Internal()
        {
            if (IsAttacking)
            {
                Invoker.Speed += SpeedReducing;
                Invoker.permissions = CharacterPermission.All;
            
                StopCoroutine(attackProcess);
                
                OnAttackEnded.Invoke();
                
                currentAttackDamageReport = null;

                attackProcess = null;

                if (IsServer && IsPressed)
                {
                    StartAttack();
                }
            }
        }
    }

    [Serializable]
    public abstract class AttackQueueElement
    {
        protected void Execute(IEnumerable<Caster> casters, IDamageSource source, DamageSource damageSource)
        {
            foreach (var cast in casters)
            {
                foreach (var collider in cast.CastCollider(damageSource.transform))
                {
                    if (collider.isTrigger)
                        continue;

                    var damage = cast.damage;
                    damage.pushDirection = source.transform.rotation * cast.damage.pushDirection;
                    damage.sender = source;

                    damageSource.HandleDamageReport(Damage.Deliver(collider.gameObject, damage));
                }
            }
        }
    
        public abstract IEnumerator AttackPipeline(DamageSource source);

        public abstract void OnDrawGizmos(Transform transform);
    }
    
    [Serializable]
    public sealed class Push : AttackQueueElement
    {
        [Header("Events")]
        
        [Space]
        [SerializeField]
        private Vector3 CastPushDirection = Vector3.forward / 2; 
        
        public override IEnumerator AttackPipeline(DamageSource source)
        {
            yield return ChargedAttackPipeline(source, 1, false, false);
        }
        public IEnumerator ChargedAttackPipeline(DamageSource source, float chargeValue, bool flexibleCollider, bool flexibleDamage)
        {            
            source.Invoker.Push(source.Invoker.transform.rotation * CastPushDirection);

            yield break;
        }

        public override void OnDrawGizmos(Transform transform)
        {

        }
    }
    [Serializable]
    public sealed class Event : AttackQueueElement
    {
        [Header("Events")]
        
        [Space]
        [SerializeField]
        private UnityEvent Function = new(); 
        
        public override IEnumerator AttackPipeline(DamageSource source)
        {
            Function.Invoke();

            yield break;
        }

        public override void OnDrawGizmos(Transform transform)
        {

        }
    }
    [Serializable]
    public sealed class Teleport : AttackQueueElement
    {
        [Header("Events")]
        
        [Space]
        [SerializeField]
        private Vector3 TeleportRelativeDirectionDirection = Vector3.forward * 3; 
        
        public override IEnumerator AttackPipeline(DamageSource source)
        {
            source.Invoker.SetPosition (source.transform.position + source.transform.rotation * TeleportRelativeDirectionDirection);

            yield break;
        }

        public override void OnDrawGizmos(Transform transform)
        {

        }
    }
    [Serializable]
    public sealed class TeleportToLastTarget : AttackQueueElement
    {
        [Header("Events")]
        
        [Space]
        [SerializeField]
        private Vector3 AdditiveTeleportRelativeDirectionDirection = Vector3.forward * 3; 
        
        public override IEnumerator AttackPipeline(DamageSource source)
        {
            if (source.currentAttackDamageReport != null && !source.currentAttackDamageReport.target.IsUnityNull())
            {
                source.Invoker.SetPosition (source.currentAttackDamageReport.target.transform.position + source.transform.rotation * AdditiveTeleportRelativeDirectionDirection);
            }

            yield break;
        }

        public override void OnDrawGizmos(Transform transform)
        {

        }
    }
    [Serializable]
    public sealed class Cast : AttackQueueElement, Charger.IChargeListener
    {
        [Header("Casters")]
        
        [Space]
        [SerializeField, SerializeReference, SubclassSelector]
        public Caster[] casters;

        [Header("Events")]
        
        [Space]
        [SerializeField]
        private Vector3 CastPushDirection = Vector3.forward / 2; 
        public UnityEvent OnCast = new();
        
        public override IEnumerator AttackPipeline(DamageSource source)
        {
            yield return ChargedAttackPipeline(source, 1, false, false);
        }
        public IEnumerator ChargedAttackPipeline(DamageSource source, float chargeValue, bool flexibleCollider, bool flexibleDamage)
        {
            var invoker = source.Invoker;

            var newCasters = CopyArray(flexibleCollider ? chargeValue : 1, flexibleDamage ? chargeValue : 1);


            OnCast.Invoke();
            invoker.Push(invoker.transform.rotation * CastPushDirection);

            Execute(newCasters, invoker, source);

            yield break;
        }

        public override void OnDrawGizmos(Transform transform)
        {
            foreach (var caster in casters)
            {
                caster?.CastColliderGizmos(transform);
            }
        }

        private Caster[] CopyArray(float MultiplySize, float MultiplyDamage)
        {
            var newArray = new Caster[casters.Count()];
            
            for (int i = 0; i < casters.Count(); i++)
            {
                var caster = newArray[i] = casters[i].Clone();

                caster.damage *= MultiplyDamage;
                caster *= MultiplySize;
            }
            
            return newArray;
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
                
                if (projectile == null)
                {
                    Debug.LogWarning("Projectile Prefab does not contains a Projectile Component");

                    yield break;
                }

                projectile.NetworkObject.Spawn();
                projectile.Initialize(source.transform.forward, source.Invoker);
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
        public AttackQueueElement[] queue;

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
            queueElement?.OnDrawGizmos(transform);
        }
    }
    [Serializable]
    public sealed class HoldRepeat : AttackQueueElement
    {
        [SerializeField, SerializeReference, SubclassSelector]
        private AttackQueueElement queueElement;

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            while (source.IsPressed)
            {
                yield return queueElement.AttackPipeline(source);
            }
        }

        public override void OnDrawGizmos(Transform transform)
        {
            queueElement?.OnDrawGizmos(transform);
        }
    }
    [Serializable]
    public sealed class Wait : AttackQueueElement
    {
        [SerializeField, Range(0, 10)]
        private float WaitTime = 1;
        
        [SerializeField]
        private CharacterPermission Permissions = CharacterPermission.All;

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            source.Invoker.permissions = Permissions;
            
            yield return new WaitForSeconds(WaitTime);
        }
        
        public override void OnDrawGizmos(Transform transform)
        {
            
        }
    }
    [Serializable]
    public sealed class ReducableWait : AttackQueueElement
    {
        [SerializeField, Range(0, 10)]
        private float WaitTime = 1;

        [SerializeField, Range(0, 10)]
        private float MinWaitTime = 0.2f;

        [SerializeField, Range(0, 10)]
        private float ReducingByHit = 0.03f;
        
        [SerializeField]
        private CharacterPermission Permissions = CharacterPermission.All;

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            source.Invoker.permissions = Permissions;
            
            yield return new WaitForSeconds(Mathf.Clamp(WaitTime - source.Invoker.Combo * ReducingByHit, MinWaitTime, WaitTime));
        }
        
        public override void OnDrawGizmos(Transform transform)
        {
            
        }
    }
    [Serializable]
    public sealed class InterruptableWait : AttackQueueElement
    {
        [SerializeField, Range(0, 10)]
        private float WaitTime = 1;

        [SerializeField, Range(0, 5)]
        private float LastHitRequireSeconds = 1;
        
        [SerializeField]
        private CharacterPermission Permissions = CharacterPermission.All;

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            source.Invoker.permissions = Permissions;

            var deltaTime = DateTime.Now - (source.Invoker.lastReport?.time ?? DateTime.MinValue);
                        
            if (deltaTime > new TimeSpan((long)Mathf.Round(TimeSpan.TicksPerSecond * LastHitRequireSeconds)))
            {
                yield return new WaitForSeconds(WaitTime);
            }           
        }
        
        public override void OnDrawGizmos(Transform transform)
        {
            
        }
    }


    [Serializable]
    public sealed class PlayAnimation : AttackQueueElement
    {
        [SerializeField]
        public string AnimationName = "Torso.Attack1";

        public override IEnumerator AttackPipeline(DamageSource source)
        {

            if (source.Invoker.animator != null && source.Invoker.animator.gameObject.activeInHierarchy)
            {
                source.Invoker.animator.Play(AnimationName, -1, 0.1f);
            }

            yield break;
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
            IEnumerator ChargedAttackPipeline(DamageSource source, float chargeValue, bool flexibleCollider, bool flexibleDamage);

            void OnDrawGizmos(Transform transform);
        }
        
        [SerializeField]
        private CharacterPermission Permissions = CharacterPermission.All;

        [SerializeField, Range(0, 10)]
        private float MaxChargingTime = 1;
        [SerializeField]
        private float MinChargeValue = 1.25f; 
        [SerializeField]
        private float MaxChargeValue = 1.25f; 

        [SerializeField]
        private bool flexibleDamage = true;
        [SerializeField]
        private bool flexibleCollider = false;

        [SerializeField]
        private UnityEvent OnStart = new ();
        [SerializeField]
        private UnityEvent OnEnd = new ();
        [SerializeField]
        private UnityEvent<float> OnCharge = new ();
        [SerializeField]
        private UnityEvent OnFullCharged = new ();

        [SerializeField, SerializeReference, SubclassSelector]
        public IChargeListener chargeListener; 

        [SerializeField]
        public Queue FullyChargeQueue;


        public override IEnumerator AttackPipeline (DamageSource source)
        {
            OnStart.Invoke();
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
                if (waitedTime >= MaxChargingTime)
                {
                    yield return FullyChargeQueue.AttackPipeline(source);
                }

                yield return chargeListener.ChargedAttackPipeline(source, Mathf.Lerp(MinChargeValue, MaxChargeValue, waitedTime / MaxChargingTime), flexibleCollider, flexibleDamage);           
            }

            OnEnd.Invoke();
        }
        
        public override void OnDrawGizmos(Transform transform)
        {
            FullyChargeQueue?.OnDrawGizmos(transform);
            chargeListener?.OnDrawGizmos(transform);
        }
    }

    [Serializable]
    public abstract class Caster 
    {
        public Damage damage;

        public abstract Collider[] CastCollider(Transform transform);
        public abstract void CastColliderGizmos(Transform transform);

        public abstract void MultiplySize(float multiplyer); 

        public Caster Clone()
        {
            return this.MemberwiseClone() as Caster;
        }

        public static Caster operator * (Caster caster, float multiplyer)
        {
            caster.MultiplySize(multiplyer);

            return caster;
        } 
    }

    [Serializable]
    public class BoxCaster : Caster
    {
        public Vector3 position = Vector3.zero;
        public Vector3 size = Vector3.one;
        public Vector3 angle = Vector3.zero;

        public override Collider[] CastCollider(Transform transform)
        {
            return Physics.OverlapBox(transform.position + (transform.rotation * position), size, Quaternion.Euler(angle + transform.eulerAngles), ~(1 << 7));
        }

        public override void CastColliderGizmos(Transform transform)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position + (transform.rotation * position), Quaternion.Euler(transform.eulerAngles + angle), Vector3.one);

            Gizmos.DrawWireCube(Vector3.zero, size * 2);

            Gizmos.DrawRay(Vector3.zero, damage.pushDirection);
        }

        public override void MultiplySize(float multiplyer)
        {
            size *= multiplyer;
        }
    }
    [Serializable]
    public class SphereCaster : Caster
    {
        public Vector3 position;
        public float radius = 1;

        public override Collider[] CastCollider(Transform transform)
        {
            return Physics.OverlapSphere(transform.position + (transform.rotation * position), radius, ~(1 << 7));
        }

        public override void CastColliderGizmos(Transform transform)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position + (transform.rotation * position), transform.rotation, Vector3.one);

            Gizmos.DrawWireSphere(Vector3.zero, radius);

            Gizmos.DrawRay(Vector3.zero, damage.pushDirection);
        }

        public override void MultiplySize(float multiplyer)
        {
            radius *= multiplyer;
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
            if (Physics.Raycast((transform.rotation * origin) + transform.position, transform.rotation * direction, out var hit, maxDistance, ~(1 << 7)))
            {
                return new Collider[] { hit.collider };
            }

            return new Collider[0];
        }

        public override void CastColliderGizmos(Transform transform)
        {
            Gizmos.DrawRay((transform.rotation * origin) + transform.position, (transform.rotation * direction.normalized) * maxDistance);
            
            Gizmos.DrawRay(Vector3.zero, damage.pushDirection);
        }

        public override void MultiplySize(float multiplyer)
        {
            maxDistance *= multiplyer;
        }
    }
    [Serializable]
    public class TargetRaycastCaster : Caster
    {
        public Vector3 origin;
        public Transform target;
        public float maxDistance;

        public override Collider[] CastCollider(Transform transform)
        {
            if (target != null)
            {
                var RayOrigin = (transform.rotation * origin) + transform.position;

                if (Physics.Raycast(RayOrigin, target.position - RayOrigin, out var hit, maxDistance, ~(1 << 7)))
                {
                    return new Collider[] { hit.collider };
                }
            }
            
            return new Collider[0];
        }

        public override void CastColliderGizmos(Transform transform)
        {
            if (target != null)
            {
                var RayOrigin = (transform.rotation * origin) + transform.position;

                Gizmos.DrawRay(RayOrigin, target.position - RayOrigin);
                
                Gizmos.DrawRay(Vector3.zero, damage.pushDirection);
            }
        }

        public override void MultiplySize(float multiplyer)
        {
            maxDistance *= multiplyer;
        }
    }
}