using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Cysharp.Threading.Tasks;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.TextCore;
using UnityEngine.VFX;

using static UnityEngine.InputSystem.InputAction;

namespace CharacterSystem.Attacks
{
    public interface IAttackSource :
        ISyncedActivitiesSource,
        IDamagable,
        ITimeScalable,
        IPhysicObject
    {
        public int Combo { get; }

        public bool IsGrounded { get; }

        public float DamageMultipliyer { get; set; }

        public DamageDeliveryReport lastReport { get; set; }

        public event Action<DamageDeliveryReport> onDamageDelivered;
        public event Action<int> onComboChanged;

        public void SetPosition (Vector3 position);
        public void DamageDelivered(DamageDeliveryReport report);
    }

    [DisallowMultipleComponent]
    public class AttackActivity : SyncedActivitySource<IAttackSource>
    {
        public enum WorkOnlyCondition
        {
            Always,
            OnGround,
            OnAir,
            OnDodge,
            OnNotDodge,
            OnChampion,
        }


        [Space]
        [Header("Attack")]
        [SerializeField]
        private bool IsInterruptableWhenBlocked = false;

        [SerializeField]
        private bool IsInterruptOnHit = true;

        [SerializeField]
        private bool RestartWhenHolding = true;

        [SerializeField]
        private WorkOnlyCondition workOnlyCondition = WorkOnlyCondition.Always;


        [SerializeField, SerializeReference, SubclassSelector]
        public AttackQueueElement[] attackQueue;


        [Space]
        [Header("Events")]
        [SerializeField]
        public UnityEvent<DamageDeliveryReport> OnDamageReport = new ();
        
        [SerializeField]
        public UnityEvent OnAttackEnded = new ();
        
        [SerializeField]
        public UnityEvent OnAttackInterrupted = new ();
        
        [SerializeField]
        public UnityEvent OnUnsuccesfullyStart = new ();


        public DamageDeliveryReport currentAttackDamageReport { get; protected set; }

        public virtual bool IsActive => 
            !IsInProcess && 
            Source != null && 
            !Source.isStunned && 
            Source.activities.CalculatePermissions().HasFlag(CharacterPermission.AllowAttacking) && 
            isPerforming &&
            workOnlyCondition switch
            {
                WorkOnlyCondition.Always => true,
                
                WorkOnlyCondition.OnGround => Source.IsGrounded,
                WorkOnlyCondition.OnAir => !Source.IsGrounded,
                
                WorkOnlyCondition.OnDodge => Source.activities.Any(activity => activity is DodgeActivity),
                WorkOnlyCondition.OnNotDodge => Source.activities.All(activity => activity is not DodgeActivity),
                
                _ => throw new NotImplementedException(),
            };

        public override void OnStartServer()
        {
            base.OnStartServer();

            Source.onDamageRecieved += (damage) => { 
                if (damage.type is not Damage.Type.Effect && IsInterruptOnHit)
                {
                    Stop(true);

                    OnAttackInterrupted.Invoke();
                }
            };
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            Stop();
        }

        public void PlayForced()
        {
            base.Play();
        }
        public override void Play()
        {   
            if (!HasOverrides())
            {
                if (IsActive)
                {
                    PlayForced();
                }
                else
                {
                    OnUnsuccesfullyStart.Invoke();
                }
            }
        }
        public override void Stop(bool interuptProcess = true)
        {
            if (IsInProcess)
            {                
                currentAttackDamageReport = null;

                OnAttackEnded.Invoke();

                base.Stop(interuptProcess);
            }
        }
        
        public override IEnumerator Process()
        {            
            yield return new WaitUntil(() => IsPressed);

            foreach (var item in attackQueue)
            {
                if (item != null)
                {
                    yield return item.AttackPipeline(this);
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (IsPressed && RestartWhenHolding && isServer)
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

                    OnAttackInterrupted.Invoke();

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
                if (item != null)
                {
                    item.OnDrawGizmos(transform);
                } 
            }
        }
    }

#region Queue elements
    [Serializable]
    public abstract class AttackQueueElement
    {    
        public abstract IEnumerator AttackPipeline(AttackActivity source);

        public abstract void OnDrawGizmos(Transform transform);
    }


    [Serializable, AddTypeMenu("Play VFX", -1)]
    public sealed class PlayVFX : AttackQueueElement
    {
        [SerializeField]
        private VisualEffect visualEffect;

        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            if (visualEffect != null)
            {
                visualEffect.playRate = source.Source.LocalTimeScale;
                visualEffect.Play();
            }
            else
            {
                Debug.LogWarning("Visual effect is null");
            }

            yield break;
        }

        public override void OnDrawGizmos(Transform transform) { }
    }
    [Serializable, AddTypeMenu("Set permissions", -1)]
    public sealed class SetPermissions : AttackQueueElement
    {
        [SerializeField]
        private CharacterPermission permission = CharacterPermission.Default;

        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            source.Permissions = permission;

            yield break;
        }

        public override void OnDrawGizmos(Transform transform) { }
    }
    [Serializable, AddTypeMenu("Reset Velocity", -1)]
    public sealed class ResetVelocity : AttackQueueElement
    {
        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            source.Source.velocity = Vector3.zero;

            yield break;
        }

        public override void OnDrawGizmos(Transform transform) { }
    }
    [Serializable, AddTypeMenu("Event", -1)]
    public sealed class Event : AttackQueueElement
    {
        [Header("Events")]
        
        [Space]
        [SerializeField]
        private UnityEvent Function = new(); 
        
        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            Function.Invoke();

            yield break;
        }

        public override void OnDrawGizmos(Transform transform)
        {
            
        }
    }
    [Serializable, AddTypeMenu("Play Animation", -1)]
    public sealed class PlayAnimation : AttackQueueElement
    {
        [SerializeField]
        public string AnimationName = "Torso.Attack1";
        
        [SerializeField]
        public float NormalizeTime = 0.1f;

        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            if (source.Source.animator != null && source.Source.animator.gameObject.activeInHierarchy)
            {
                source.Source.animator.Play(AnimationName, -1, NormalizeTime);
            }

            yield break;
        }

        public override void OnDrawGizmos(Transform transform){ }
    }
    [Serializable, AddTypeMenu("Pattern", -1)]
    public sealed class Pattern : AttackQueueElement
    {
        [SerializeField]
        public AttackPattern attackPattern;

        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            if (attackPattern == null)
            {
                Debug.LogWarning($"Pattern is not selected in {source.gameObject.ToSafeString()}");

                yield break;
            }

            foreach (var item in attackPattern.attackQueue)
            {
                if (item != null)
                {
                    yield return item.AttackPipeline(source);
                }
            }
        }

        public override void OnDrawGizmos(Transform transform) 
        { 
            attackPattern.GetGizmos(transform);
        }
    }

    [Serializable, AddTypeMenu("Move/Push")]
    public sealed class Push : AttackQueueElement, Charger.IChargeListener
    {        
        [Space]
        [SerializeField]
        private Vector3 CastPushDirection = Vector3.forward / 2; 
        
        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            yield return ChargedAttackPipeline(source, 1, false, false);
        }
        public IEnumerator ChargedAttackPipeline(AttackActivity source, float chargeValue, bool flexibleCollider, bool flexibleDamage)
        {            
            source.Source.Push(source.Source.transform.rotation * CastPushDirection * chargeValue);

            yield break;
        }

        public override void OnDrawGizmos(Transform transform)
        {

        }
    }
    [Serializable, AddTypeMenu("Move/Move")]
    public sealed class Move : AttackQueueElement, Charger.IChargeListener
    {        
        [Space]
        [SerializeField]
        private Vector3 MoveDirection = Vector3.forward * 30;

        [SerializeField]
        private string animationName = "Legs.Dash";

        [SerializeField, Range(0, 10)]
        private float time = 0.2f;

        [SerializeField]
        private bool CheckColision = true;

        [SerializeField]
        private CharacterPermission permission = CharacterPermission.None;
        
        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            yield return ChargedAttackPipeline(source, 1, false, false);
        }
        public IEnumerator ChargedAttackPipeline(AttackActivity source, float chargeValue, bool flexibleCollider, bool flexibleDamage)
        {            
            if (source.Source.gameObject.TryGetComponent<CharacterController>(out var component))
            {
                Physics.SyncTransforms();

                source.Permissions = permission;

                var waitForFixedUpdate = new WaitForFixedUpdate();
                var wasteTime = 0f;

                while (wasteTime < time * chargeValue)
                {
                    if (source.Source.animator != null && source.Source.animator.gameObject.activeInHierarchy)
                    {
                        source.Source.animator.Play(animationName, -1, 0.1f);
                    }
                    
                    var direction = source.transform.rotation * MoveDirection * Time.fixedDeltaTime;
                    
                    component.Move(direction);

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

                    Physics.SyncTransforms();

                    yield return waitForFixedUpdate;

                    wasteTime += Time.fixedDeltaTime;
                }
            }
        }

        public override void OnDrawGizmos(Transform transform)
        {

        }
    }
    [Serializable, AddTypeMenu("Move/Teleport")]
    public sealed class Teleport : AttackQueueElement
    {
        [Header("Events")]
        
        [Space]
        [SerializeField]
        private Vector3 TeleportRelativeDirection = Vector3.forward * 3; 
        
        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            if (source.isServer)
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

                source.Source.transform.position = newPosition;
                source.Source.SetPosition (newPosition);

                Physics.SyncTransforms();
            }

            yield break;
        }

        public override void OnDrawGizmos(Transform transform)
        {

        }
    }
    [Serializable, AddTypeMenu("Move/Teleport To Last Target")]
    public sealed class TeleportToLastTarget : AttackQueueElement
    {
        [Header("Events")]
        
        [Space]
        [SerializeField]
        private Vector3 AdditiveTeleportRelativeDirectionDirection = Vector3.forward * 3; 
        
        public override IEnumerator AttackPipeline(AttackActivity source)
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
    
    [Serializable, AddTypeMenu("Attack/Cast")]
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

        private void Execute(IEnumerable<Caster> casters, AttackActivity damageSource)
        {
            foreach (var cast in casters)
            {
                foreach (var report in cast.CalculateReports(damageSource))
                {
                    damageSource.HandleDamageReport(report);
                }
            }
        }
        
        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            yield return ChargedAttackPipeline(source, 1, false, false);
        }
        public IEnumerator ChargedAttackPipeline(AttackActivity source, float chargeValue, bool flexibleCollider, bool flexibleDamage)
        {
            var invoker = source.Source;

            var newCasters = CopyArray(flexibleCollider ? chargeValue : 1, flexibleDamage ? chargeValue : 1 * source.Source.DamageMultipliyer);

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
    [Serializable, AddTypeMenu("Attack/Projectile Shoot")]
    public sealed class ProjectileShooter : AttackQueueElement, Charger.IChargeListener
    {
        [SerializeField]
        public GameObject projectilePrefab;

        [SerializeField]
        public Vector3 direction = Vector3.forward;        

        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            yield return ChargedAttackPipeline(source, 1, false, false);
        }

        public IEnumerator ChargedAttackPipeline(AttackActivity source, float chargeValue, bool flexibleCollider, bool flexibleDamage)
        {
            if (source.isServer)
            {
                if (projectilePrefab == null)
                {
                    Debug.LogWarning("Projectile Prefab is null");

                    yield break;
                }

                var projectileObject = GameObject.Instantiate(projectilePrefab, source.transform.position, Quaternion.identity);
                projectileObject.SetActive(true);
                NetworkServer.Spawn(projectileObject, source.Source.gameObject);

                foreach (var projectile in projectileObject.GetComponents<Projectile>())
                {
                    projectile.Initialize(source.transform.rotation * direction, source.Source, source.HandleDamageReport);

                    projectile.speed *= chargeValue;

                    if (flexibleDamage)
                    {
                        projectile.damage *= chargeValue;
                    }
                }
            }
        }

        public override void OnDrawGizmos(Transform transform)
        {
            
        }
    }
    [Serializable, AddTypeMenu("Attack/Self Damage")]
    public sealed class SelfDamage : AttackQueueElement
    {        
        [Space]
        [SerializeField]
        private Damage damage = new Damage();
        
        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            Damage.Deliver(source.Source, damage);

            yield break;
        }

        public override void OnDrawGizmos(Transform transform)
        {

        }
    }
    [Serializable, AddTypeMenu("Attack/Charger")]
    public sealed class Charger : AttackQueueElement
    {
        public interface IChargeListener 
        {
            IEnumerator ChargedAttackPipeline(AttackActivity source, float chargeValue, bool flexibleCollider, bool flexibleDamage);

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


        public override IEnumerator AttackPipeline (AttackActivity source)
        {
            yield return new WaitUntil(() => source.IsPressed);

            OnStart.Invoke();
            source.Permissions = Permissions;

            var waitForFixedUpdate = new WaitForFixedUpdate();
            var waitedTime = 0f;

            while (source.IsPressed && source.isPerforming)
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
                yield return chargeListener.ChargedAttackPipeline(source, Mathf.Lerp(MinChargeValue, MaxChargeValue, waitedTime / MaxChargingTime), flexibleCollider, flexibleDamage);           
            }

            OnEnd.Invoke();
        }
        
        public override void OnDrawGizmos(Transform transform)
        {
            chargeListener?.OnDrawGizmos(transform);
        }
    }

    [Serializable, AddTypeMenu("Repeat/Repeat")]
    public sealed class Repeat : AttackQueueElement
    {
        [SerializeField, SerializeReference, SubclassSelector]
        public AttackQueueElement[] queueElements;

        [SerializeField, Range(1, 50)]
        public int RepeatCount = 5;

        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            for (int i = 0; i < RepeatCount; i++)
            {
                foreach (var element in queueElements)
                {
                    if (element != null)
                    {
                        yield return element.AttackPipeline(source);
                    }
                }
            }
        }

        public override void OnDrawGizmos(Transform transform)
        {
            foreach (var element in queueElements)
            {
                if (element != null)
                {
                    element.OnDrawGizmos(transform);
                }
            }
        }
    }
    [Serializable, AddTypeMenu("Repeat/Hold Repeat")]
    public sealed class HoldRepeat : AttackQueueElement
    {
        [SerializeField, SerializeReference, SubclassSelector]
        public AttackQueueElement[] queueElements;

        [SerializeField, Range(0, 100)]
        public int RepeatLimit = 0;

        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            int repeats = 0;

            do 
            {
                foreach (var element in queueElements)
                {
                    if (element != null)
                    {
                        yield return element.AttackPipeline(source);
                    }
                }

                if (RepeatLimit > 0)
                {
                    repeats++;
                }
            }
            while (repeats <= RepeatLimit && source.IsPressed);
        }

        public override void OnDrawGizmos(Transform transform)
        {
            foreach (var element in queueElements)
            {
                if (element != null)
                {
                    element.OnDrawGizmos(transform);
                }
            }
        }
    }
    
    [Serializable, AddTypeMenu("Wait/Wait")]
    public sealed class Wait : AttackQueueElement
    {
        [SerializeField, Range(0, 10)]
        private float WaitTime = 1;
        
        [SerializeField]
        private CharacterPermission Permissions = CharacterPermission.Default;

        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            source.Permissions = Permissions;
            
            yield return new WaitForSeconds(WaitTime * source.Source.LocalTimeScale);
        }
        
        public override void OnDrawGizmos(Transform transform)
        {
            
        }
    }
    [Serializable, AddTypeMenu("Wait/Wait For Groudpound")]
    public sealed class WaitForGroudpound : AttackQueueElement
    {        
        [SerializeField]
        private CharacterPermission Permissions = CharacterPermission.Default;
        
        [SerializeField]
        private bool Reverse = false;

        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            source.Permissions = Permissions;
            
            yield return new WaitUntil(() => {
                return Reverse ? !source.Source.IsGrounded : source.Source.IsGrounded;
            });
        }
        
        public override void OnDrawGizmos(Transform transform)
        {
            
        }
    }
    [Serializable, AddTypeMenu("Wait/Reducable Wait")]
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

        public override IEnumerator AttackPipeline(AttackActivity source)
        {
            source.Permissions = Permissions;
            
            yield return new WaitForSeconds(Mathf.Clamp(WaitTime - source.Source.Combo * ReducingByHit, MinWaitTime, WaitTime) * source.Source.LocalTimeScale);
        }
        
        public override void OnDrawGizmos(Transform transform)
        {
            
        }
    }
    [Serializable, AddTypeMenu("Wait/Interruptable Wait")]
    public sealed class InterruptableWait : AttackQueueElement
    {
        [SerializeField, Range(0, 10)]
        private float WaitTime = 1;

        [SerializeField, Range(0, 5)]
        private float LastHitRequireSeconds = 1;
        
        [SerializeField]
        private CharacterPermission Permissions = CharacterPermission.Default;

        public override IEnumerator AttackPipeline(AttackActivity source)
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
    public abstract class Caster 
    {
        public Damage damage;

        public abstract Collider[] CastCollider(AttackActivity attack);
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

        public DamageDeliveryReport[] CalculateReports(AttackActivity attack)
        {
            var colliders = CastCollider(attack);
            var result = new DamageDeliveryReport[colliders.Length];
            var index = 0;

            foreach (var collider in colliders)
            {
                if (collider.isTrigger)
                    continue;

                var newDamage = damage;

                newDamage.pushDirection = attack.transform.rotation * damage.pushDirection;
                newDamage.sender = attack.Source;

                var report = result[index] = Damage.Deliver(collider.gameObject, newDamage);

                index++;
            }

            return result;
        }
    }

    [Serializable]
    public class BoxCaster : Caster
    {
        public Vector3 position = Vector3.zero;
        public Vector3 size = Vector3.one;
        public Vector3 angle = Vector3.zero;

        public override Collider[] CastCollider(AttackActivity attack)
        {
            var transform = attack.transform;

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

        public override Collider[] CastCollider(AttackActivity attack)
        {
            var transform = attack.transform;

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

        public override Collider[] CastCollider(AttackActivity attack)
        {
            var transform = attack.transform;

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

        public override Collider[] CastCollider(AttackActivity attack)
        {
            var transform = attack.transform;

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