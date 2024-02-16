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
    
    public interface IDamageSource : 
        ISyncedActivitiesSource, 
        IDamagable
    {
        new GameObject gameObject { get; }

        float Speed { get; set; }

        void OnDamageDelivered(Damage damage);
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
        protected Multicaster multicaster;

        [SerializeField]
        private bool IsPerformingAsDefault = false;

        [SerializeField]
        private bool IsInterruptable = false;

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


        public void StartAttack()
        {
            if (Invoker.IsServer && 
                !Invoker.isStunned && 
                Invoker.permissions.HasFlag(CharacterPermission.AllowAttacking) &&
                IsPerforming)
            {
                StartAttack_ClientRpc();

                StartAttack_Internal();
            }            
        }
        public void EndAttack()
        {
            if (Invoker.IsServer)
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
            multicaster.OnDrawGizmos(transform);
        }

        private void StartAttack_Internal()
        {
            // Invoker.Speed -= SpeedReducing;

            attackProcess = StartCoroutine(multicaster.AttackPipeline(this));
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
                // Invoker.Speed += SpeedReducing;

                StopCoroutine(attackProcess);
            }

            Invoker.permissions = CharacterPermission.All;
            attackProcess = null;
        }
        [ClientRpc]
        private void EndAttack_ClientRpc()
        {
            EndAttack_Internal();
        }
    }


    [Serializable]
    public abstract class Multicaster
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
    }

    [Serializable]
    public sealed class SingleCastMulticaster : Multicaster
    {
        [Header("Casters")]
        
        [Space]
        [SerializeField, SerializeReference, SubclassSelector]
        private Caster[] casters;

        [Header("Timing")]

        [Space]
        [SerializeField, Range(0, 3)]
        public float BeforeAttackDelay = 1;        
        [SerializeField]
        public CharacterPermission BeforeAttackPermissions = CharacterPermission.All;

        [Space]
        [SerializeField, Range(0, 3)]
        public float AfterAttackDelay = 1;
        [SerializeField]
        public CharacterPermission AfterAttackPermissions = CharacterPermission.All;

        [Header("Events")]
        
        [Space]
        [SerializeField]
        private string StartCastAnimationName = "Torso.Attack1_Prepare";
        [SerializeField]
        private Vector3 StartCastPushDirection = Vector3.forward; 
        public UnityEvent OnStartCast = new();
        
        [SerializeField]
        private string CastAnimationName = "Torso.Attack1";
        [SerializeField]
        private Vector3 CastPushDirection = Vector3.forward / 2; 
        public UnityEvent OnCast = new();
        
        [SerializeField]
        private string EndCastAnimationName = "Torso.Attack1_Ending";
        [SerializeField]
        private Vector3 EndCastPushDirection = Vector3.zero; 
        public UnityEvent OnEndCast = new();

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            var invoker = source.Invoker;

            OnStartCast.Invoke();
            invoker.permissions = BeforeAttackPermissions;
            invoker.Push(invoker.transform.rotation * StartCastPushDirection);
            invoker.animator.Play(StartCastAnimationName);
            yield return new WaitForSeconds(BeforeAttackDelay);

            OnCast.Invoke();
            invoker.Push(invoker.transform.rotation * CastPushDirection);
            invoker.animator.Play(CastAnimationName);

            Execute(casters, invoker, 1);

            yield return new WaitForSeconds(AfterAttackDelay);
            OnEndCast.Invoke();
            invoker.permissions = AfterAttackPermissions;
            invoker.Push(invoker.transform.rotation * EndCastPushDirection);
            invoker.animator.Play(EndCastAnimationName);
        }

        public override void OnDrawGizmos(Transform transform)
        {
            foreach (var caster in casters)
            {
                caster.CastColliderGizmos(transform);
            }
        }
    }

    [Serializable]
    public sealed class ChargableSingleCastMulticaster : Multicaster
    {
        [Header("Casters")]
        
        [Space]
        [SerializeField, SerializeReference, SubclassSelector]
        private Caster[] casters;

        [Header("Timing")]

        [Space]
        [SerializeField, Range(0, 3)]
        public float BeforeAttackDelay = 1;        
        [SerializeField]
        public CharacterPermission BeforeAttackPermissions = CharacterPermission.All;

        [Space]
        [SerializeField, Range(0, 5)]
        public float AttackChargeMaxTime = 2;        
        [SerializeField, Range(0, 3)]
        public float AttackChargeMinMultipliyer = 0.25f;        
        [SerializeField, Range(0, 3)]
        public float AttackChargeMaxMultipliyer = 1.25f;        
        [SerializeField]
        public CharacterPermission AttackChargePermissions = CharacterPermission.All;

        [Space]
        [SerializeField, Range(0, 3)]
        public float AfterAttackDelay = 1;
        [SerializeField]
        public CharacterPermission AfterAttackPermissions = CharacterPermission.All;

        [Header("Events")]
                
        [Space]
        [SerializeField]
        private string StartCastAnimationName = "Torso.Attack1_Prepare";
        [SerializeField]
        private Vector3 StartCastPushDirection = Vector3.forward; 
        public UnityEvent OnStartCast = new();
        
        [SerializeField]
        private string ChargeAnimationName = "Torso.Attack1_Prepare";
        public UnityEvent<float> OnCharge = new();
        
        [SerializeField]
        private string FullyChargeAnimationName = "Torso.Attack1_Prepare";
        public UnityEvent OnFullyCharge = new();
        
        [SerializeField]
        private string CastAnimationName = "Torso.Attack1";
        [SerializeField]
        private Vector3 CastPushDirection = Vector3.forward / 2; 
        public UnityEvent OnCast = new();
        
        [SerializeField]
        private string EndCastAnimationName = "Torso.Attack1_Ending";
        [SerializeField]
        private Vector3 EndCastPushDirection = Vector3.zero; 
        public UnityEvent OnEndCast = new();

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            var invoker = source.Invoker;

            float multiplier = 0;
            float holdTime = 0;

            OnStartCast.Invoke();
            invoker.permissions = BeforeAttackPermissions;
            invoker.Push(invoker.transform.rotation * StartCastPushDirection);
            invoker.animator.Play(StartCastAnimationName);
            yield return new WaitForSeconds(BeforeAttackDelay);

            var waitForFixedUpdateRoutine = new WaitForFixedUpdate();
            while (source.IsPressed)
            {   
                yield return waitForFixedUpdateRoutine;

                if (holdTime >= AttackChargeMaxTime)
                {
                    continue;
                }

                OnCharge.Invoke(holdTime);

                holdTime += Time.fixedDeltaTime;
                multiplier = Mathf.Lerp(AttackChargeMinMultipliyer, AttackChargeMaxMultipliyer, holdTime);

                if (holdTime >= AttackChargeMaxTime)
                {
                    OnFullyCharge.Invoke();
                }
            }

            OnCast.Invoke();
            invoker.Push(invoker.transform.rotation * CastPushDirection);
            invoker.animator.Play(CastAnimationName);

            Execute(casters, invoker, multiplier);

            yield return new WaitForSeconds(AfterAttackDelay);
            OnEndCast.Invoke();
            invoker.permissions = AfterAttackPermissions;
            invoker.Push(invoker.transform.rotation * EndCastPushDirection);
            invoker.animator.Play(EndCastAnimationName);
        }
    
        public override void OnDrawGizmos(Transform transform)
        {
            foreach (var caster in casters)
            {
                caster.CastColliderGizmos(transform);
            }
        }
    }

    [Serializable]
    public sealed class QueuedCastMulticaster : Multicaster
    {
        [SerializeField, SerializeReference, SubclassSelector]
        private Multicaster[] queue;

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            foreach (var multicaster in queue)
            {
                yield return multicaster.AttackPipeline(source);
            }
        }

        public override void OnDrawGizmos(Transform transform)
        {
            foreach (var multicaster in queue)
            {
                multicaster.OnDrawGizmos(transform);
            }
        }
    }

    [Serializable]
    public sealed class RepetitiveCastMulticaster : Multicaster
    {
        [SerializeField, SerializeReference, SubclassSelector]
        private Multicaster multicaster;

        [SerializeField, Range(1, 50)]
        private int RepeatCount = 5;

        public override IEnumerator AttackPipeline(DamageSource source)
        {
            for (int i = 0; i < RepeatCount; i++)
            {
                yield return multicaster.AttackPipeline(source);
            }
        }

        public override void OnDrawGizmos(Transform transform)
        {
            multicaster.OnDrawGizmos(transform);
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
