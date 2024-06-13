using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.TextCore;
using static UnityEngine.InputSystem.InputAction;

namespace CharacterSystem.Attacks
{
    public interface IDamageSource :
        ISyncedActivitiesSource,
        IDamagable,
        ITimeScalable,
        IPhysicObject
    {
        DamageDeliveryReport lastReport { get; set; }

        int Combo { get; }

        event Action<DamageDeliveryReport> onDamageDelivered;
        event Action<int> onComboChanged;

        void SetPosition (Vector3 position);
        void DamageDelivered(DamageDeliveryReport report);
    }

    [DisallowMultipleComponent]
    public class DamageSource : SyncedActivitySource<IDamageSource>
    {
        [Flags]
        public enum AdditiveExecutingConditions
        {
            OnInitialize,
            OnHit,
            OnDespawn,
        }

        [SerializeField]
        private bool IsPerformingAsDefault = true;

        [SerializeField]
        private bool IsInterruptableWhenBlocked = false;

        [SerializeField]
        private bool IsInterruptOnHit = true;

        [SerializeField]
        private bool RestartWhenHolding = true;
        
        [SerializeField, SerializeReference, SubclassSelector]
        public AttackQueueElement[] attackQueue;

        [SerializeField]
        public UnityEvent<DamageDeliveryReport> OnDamageReport = new ();
        
        [SerializeField]
        public UnityEvent OnAttackEnded = new ();


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

        public virtual bool IsActive => !IsInProcess && !HasOverrides() && !Source.isStunned && Source.permissions.HasFlag(CharacterPermission.AllowAttacking) && IsPerforming; 

        private NetworkVariable<bool> network_isPerforming = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
                        
            if (IsServer)
            {
                network_isPerforming.Value = IsPerformingAsDefault;
            }
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            Stop();
        }

        public void PlayForced()
        {
            if (!HasOverrides())
            {
                base.Play();
            }
        }
        public override void Play()
        {            
            if (IsActive)
            {
                PlayForced();                
            }    
        }
        public override void Stop(bool interuptProcess = true)
        {
            if (IsInProcess)
            {
                Permissions = CharacterPermission.Default;
                
                currentAttackDamageReport = null;

                OnAttackEnded.Invoke();
                
                base.Stop(interuptProcess);
            }
        }
        
        public override IEnumerator Process()
        {            
            foreach (var item in attackQueue)
            {
                yield return item.AttackPipeline(this);
            }
        }

        protected virtual void Start()
        {
            if (IsServer && IsInterruptOnHit)
            {
                Source.onDamageRecieved += (damage) => { 
                    if (damage.type != Damage.Type.Effect)
                    {
                        Stop();
                    }
                };
            }
        }     
        protected virtual void FixedUpdate()
        {
            if (IsPressed && RestartWhenHolding)
            {
                Play();
            }
        }

        internal void HandleDamageReport(DamageDeliveryReport report)
        {
            if (report.isDelivered)
            {
                if (report.isBlocked && IsInterruptableWhenBlocked)
                {
                    Stop();

                    Permissions = CharacterPermission.Default;
                }

                OnDamageReport.Invoke(report);

                currentAttackDamageReport = report;

                Source.DamageDelivered(report);
                Source.lastReport = report;
            }
        }

        private void OnDrawGizmosSelected()
        {
            foreach (var item in attackQueue)
            {
                item?.OnDrawGizmos(transform);
            }
        }
    }

#region Queue elements
    [Serializable]
    public abstract class AttackQueueElement
    {
        protected void Execute(IEnumerable<Caster> casters, DamageSource damageSource)
        {
            var impactPushVector = Vector3.zero;
            var impactsCount = 0; 

            foreach (var cast in casters)
            {
                foreach (var collider in cast.CastCollider(damageSource.transform))
                {
                    if (collider.isTrigger)
                        continue;

                    var damage = cast.damage;
                    damage.pushDirection = damageSource.Source.transform.rotation * cast.damage.pushDirection;
                    damage.sender = damageSource.Source;

                    var report = Damage.Deliver(collider.gameObject, damage);

                    if (report.isDelivered)
                    {
                        impactsCount++;
                        impactPushVector += -report.damage.pushDirection / 1.2f;
                    }

                    damageSource.HandleDamageReport(report);
                }
            }

            damageSource.Source.Push(impactPushVector / impactsCount);
        }
    
        public abstract IEnumerator AttackPipeline(DamageSource source);

        public abstract void OnDrawGizmos(Transform transform);
    }
    
    [Serializable]
    public sealed class Push : AttackQueueElement, Charger.IChargeListener
    {        
        [Space]
        [SerializeField]
        private Vector3 CastPushDirection = Vector3.forward / 2; 
        
        public override IEnumerator AttackPipeline(DamageSource source)
        {
            yield return ChargedAttackPipeline(source, 1, false, false);
        }
        public IEnumerator ChargedAttackPipeline(DamageSource source, float chargeValue, bool flexibleCollider, bool flexibleDamage)
        {            
            source.Source.Push(source.Source.transform.rotation * CastPushDirection * chargeValue);

            yield break;
        }

        public override void OnDrawGizmos(Transform transform)
        {

        }
    }
    [Serializable]
    public sealed class Move : AttackQueueElement, Charger.IChargeListener
    {        
        [Space]
        [SerializeField]
        private Vector3 MoveDirection = Vector3.forward * 30;

        [SerializeField, Range(0, 10)]
        private float time = 0.2f;

        [SerializeField]
        private bool CheckColision = true;

        [SerializeField]
        private CharacterPermission permission = CharacterPermission.None;
        
        public override IEnumerator AttackPipeline(DamageSource source)
        {
            yield return ChargedAttackPipeline(source, 1, false, false);
        }
        public IEnumerator ChargedAttackPipeline(DamageSource source, float chargeValue, bool flexibleCollider, bool flexibleDamage)
        {            
            if (source.Source.gameObject.TryGetComponent<CharacterController>(out var component))
            {
                source.Permissions = permission;

                var waitForFixedUpdate = new WaitForFixedUpdate();
                var wasteTime = 0f;

                while (wasteTime < time * chargeValue)
                {
                    var direction = source.transform.rotation * MoveDirection * Time.fixedDeltaTime;
                    
                    if (CheckColision)
                    {
                        if (Physics.OverlapCapsule(
                            component.transform.position + component.center + direction * 3 - Vector3.up * component.height / 3f, 
                            component.transform.position + component.center + direction * 3 + Vector3.up * component.height / 3f, 
                            component.radius / 1.85f,
                            LayerMask.GetMask("Character", "Ground")).Where(collider => collider != component).Any())
                        {
                            yield break;
                        }
                    }

                    component.Move(direction);

                    yield return waitForFixedUpdate;

                    wasteTime += Time.fixedDeltaTime;
                }
            }

            yield break;
        }

        public override void OnDrawGizmos(Transform transform)
        {

        }
    }
    [Serializable]
    public sealed class SelfDamage : AttackQueueElement
    {        
        [Space]
        [SerializeField]
        private Damage damage = new Damage();
        
        public override IEnumerator AttackPipeline(DamageSource source)
        {
            Damage.Deliver(source.Source, damage);

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
        private Vector3 TeleportRelativeDirection = Vector3.forward * 3; 
        
        public override IEnumerator AttackPipeline(DamageSource source)
        {
            if (source.IsServer)
            {
                var newPosition = source.transform.position + source.transform.rotation * TeleportRelativeDirection;

                if (Physics.Linecast(
                    source.transform.position, 
                    source.transform.position + source.transform.rotation * TeleportRelativeDirection, 
                    out var hit,
                    LayerMask.GetMask("Ground")))
                {
                    newPosition = hit.point;
                }

                source.Source.SetPosition (newPosition);
            }

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
                source.Source.SetPosition (source.currentAttackDamageReport.target.transform.position + source.transform.rotation * AdditiveTeleportRelativeDirectionDirection);
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
            var invoker = source.Source;

            var newCasters = CopyArray(flexibleCollider ? chargeValue : 1, flexibleDamage ? chargeValue : 1);

            OnCast.Invoke();

            invoker.Push(invoker.transform.rotation * CastPushDirection);

            Execute(newCasters, source);

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
    public sealed class ProjectileShooter : AttackQueueElement, Charger.IChargeListener
    {
        [SerializeField]
        public GameObject projectilePrefab;

        [SerializeField]
        public Vector3 direction = Vector3.forward;        

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            yield return ChargedAttackPipeline(source, 1, false, false);
        }

        public IEnumerator ChargedAttackPipeline(DamageSource source, float chargeValue, bool flexibleCollider, bool flexibleDamage)
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
                projectileObject.GetComponent<NetworkObject>().Spawn();

                var projectile = projectileObject.GetComponent<Projectile>();
                
                if (projectile == null)
                {
                    Debug.LogWarning("Projectile Prefab does not contains a Projectile Component");

                    yield break;
                }
                projectile.Initialize(source.transform.rotation * direction, source.Source, source.HandleDamageReport);

                projectile.speed *= chargeValue;
                if (flexibleDamage)
                {
                    projectile.damage *= chargeValue;
                }
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
        public AttackQueueElement queueElement;

        [SerializeField, Range(1, 50)]
        public int RepeatCount = 5;

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
        public AttackQueueElement queueElement;

        [SerializeField, Range(0, 100)]
        public int RepeatLimit = 0;

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            int repeats = 0;
            while (repeats <= RepeatLimit && source.IsPressed)
            {
                yield return queueElement.AttackPipeline(source);
                
                if (RepeatLimit > 0)
                {
                    repeats++;
                }
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
        private CharacterPermission Permissions = CharacterPermission.Default;

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            source.Permissions = Permissions;
            
            yield return new WaitForSeconds(WaitTime * source.Source.LocalTimeScale);
        }
        
        public override void OnDrawGizmos(Transform transform)
        {
            
        }
    }
    [Serializable]
    public sealed class WaitForGroudpound : AttackQueueElement
    {        
        [SerializeField]
        private CharacterPermission Permissions = CharacterPermission.Default;
        
        [SerializeField]
        private bool Reverse = false;

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            source.Permissions = Permissions;
            
            yield return new WaitUntil(() => {
                if (source.Source.gameObject.TryGetComponent<CharacterController>(out var character))
                {
                    return Reverse ? !character.isGrounded : character.isGrounded;
                }
                
                return true;
            });
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
        private CharacterPermission Permissions = CharacterPermission.Default;

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            source.Permissions = Permissions;
            
            yield return new WaitForSeconds(Mathf.Clamp(WaitTime - source.Source.Combo * ReducingByHit, MinWaitTime, WaitTime) * source.Source.LocalTimeScale);
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
        private CharacterPermission Permissions = CharacterPermission.Default;

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            source.Permissions = Permissions;

            var deltaTime = DateTime.Now - (source.Source.lastReport?.time ?? DateTime.MinValue);
                        
            if (deltaTime > new TimeSpan((long)Mathf.Round(TimeSpan.TicksPerSecond * LastHitRequireSeconds / source.Source.LocalTimeScale)))
            {
                yield return new WaitForSeconds(WaitTime * source.Source.LocalTimeScale);
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

            if (source.Source.animator != null && source.Source.animator.gameObject.activeInHierarchy)
            {
                source.Source.animator.Play(AnimationName, -1, 0.1f);
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
        private CharacterPermission Permissions = CharacterPermission.Default;

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
            source.Permissions = Permissions;

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
#endregion
}