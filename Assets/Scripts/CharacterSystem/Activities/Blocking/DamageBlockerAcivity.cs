using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Cysharp.Threading.Tasks;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// WinBlocker HAHAHAHA

namespace CharacterSystem.Blocking
{
    public interface IDamageBlockerAcivity : 
        ISyncedActivitiesSource, 
        IDamagable,
        IAttackSource
    {
        DamageBlockerAcivity Blocker { set; }

        public event Action<bool> onStunStateChanged;
    }

    public class DamageBlockerAcivity : SyncedActivitySource<IDamageBlockerAcivity>
    {
        [Space]
        [Header("Stun")]
        [SerializeField]
        private CharacterPermission BeforeBlockCharacterPermissions = CharacterPermission.None;
        [SerializeField, Range(0f, 5f)]
        private float BeforeBlockTime = 0.5f;
        
        [SerializeField]
        private CharacterPermission BlockCharacterPermissions = CharacterPermission.None;
        [SerializeField, Range(0f, 5f)]
        private float BlockProcessTime = 0.5f;
        [SerializeField]
        private bool HoldMode = false;

        [SerializeField]
        private CharacterPermission AfterBlockCharacterPermissions = CharacterPermission.None;
        [SerializeField, Range(0f, 5f)]
        private float AfterBlockTime = 0.5f;

        [Space]
        [SerializeField, Range(0f, 180f)]
        private float blockingDegree = 75f;

        [SerializeField]
        private bool InterruptOnHit = true;

        [Space]
        [Header("Damage")]
        [SerializeField]
        private Damage backDamage;

        [SerializeField]
        private Damage selfHeal;

        [SerializeField, Range(0, 1)]
        public float DamageReducing = 1;

        [Space]
        [Header("Events")]
        [SerializeField]
        private UnityEvent OnBeforeBlockingEvent = new();
        [SerializeField]
        private UnityEvent OnAfterBlockingEvent = new();
        [SerializeField]
        private UnityEvent OnBlockingEvent = new();
        [SerializeField]
        private UnityEvent OnSuccesfulBlockingEvent = new();
        [SerializeField]
        private UnityEvent OnInterrupted = new();

        public bool IsBlockActive { get; private set; }

        public virtual bool CheckBlocking(ref Damage damage)
        {            
            if (damage.type == Damage.Type.Effect || 
                damage.type == Damage.Type.Parrying || 
                damage.type == Damage.Type.Unblockable) 
            {
                return false;
            }

            if (!damage.sender.IsUnityNull() &&
                IsBlockActive && 
                Vector3.Angle(transform.forward, damage.sender.transform.position - transform.position) < blockingDegree)
            {
                SkipBlock();

                if (isServer)
                {
                    OnSuccesfulBlockingEvent.Invoke();

                    ExecuteSuccesfullyBlockEvent_Command();
                }

                if (damage.sender != null && damage.type != Damage.Type.Magical && damage.type != Damage.Type.Balistic)
                {
                    LateDamageDelivery(damage);
                }
                damage *= 1f - DamageReducing;
                
                return true;
            }
            
            SkipBlock();

            return false;

            void SkipBlock()
            {
                if (InterruptOnHit)
                {
                    Source.animator.SetBool("Blocking", false);

                    IsBlockActive = false;
                    Permissions = AfterBlockCharacterPermissions;

                    OnAfterBlockingEvent.Invoke();
                    OnInterrupted.Invoke();
                }
            }
            async void LateDamageDelivery(Damage damage)
            {
                await UniTask.WaitForFixedUpdate();
                
                var blockDamage = backDamage;
                blockDamage.sender = Source;
                blockDamage.pushDirection = -damage.pushDirection;
                blockDamage.pushDirection = transform.rotation * backDamage.pushDirection;
                blockDamage.type = Damage.Type.Parrying;
                
                var selfDamage = selfHeal;
                selfDamage.sender = Source;
                selfDamage.pushDirection = transform.rotation * selfHeal.pushDirection;
                selfDamage.type = Damage.Type.Parrying;

                Damage.Deliver(damage.sender, blockDamage);
                Damage.Deliver(Source, selfDamage);
            }
        }

        public override void Play()
        {
            if (!Source.isStunned && isPerforming && Source.permissions.HasFlag(CharacterPermission.AllowBlocking))
            {
                base.Play();
            }
        }
        public override void Stop(bool interuptProcess = true)
        {
            Permissions = CharacterPermission.Default;

            if (IsInProcess)
            {                                
                Source.animator.SetBool("Blocking", false);
                IsBlockActive = false;    
            }

            base.Stop(interuptProcess);
        }
        
        public override IEnumerator Process()
        {
            Permissions = BeforeBlockCharacterPermissions;
            Source.Blocker = this;
            Source.Push(Vector3.up * 0.01f);
            
            var array = Source.activities;
            for (int i = 0; i < array.Count(); i++)
            {
                if (array[i] is AttackActivity)
                {
                    array[i].Stop();
                }
            }

            OnBeforeBlockingEvent.Invoke();

            yield return new WaitForSeconds(BeforeBlockTime);

            IsBlockActive = true;
            Permissions = BlockCharacterPermissions;
            OnBlockingEvent.Invoke();

            Source.animator.SetBool("Blocking", true);

            if (HoldMode)
            {
                var yield = new WaitForFixedUpdate();    

                while (IsPressed)
                {
                    yield return yield;    
                }
            }
            else
            {
                yield return new WaitForSeconds(BlockProcessTime);
            }

            Source.animator.SetBool("Blocking", false);

            IsBlockActive = false;
            Permissions = AfterBlockCharacterPermissions;
            OnAfterBlockingEvent.Invoke();

            yield return new WaitForSeconds(AfterBlockTime);

            Permissions = CharacterPermission.Default;
        }

        [ClientRpc]
        private void ExecuteSuccesfullyBlockEvent_Command()
        {
            OnSuccesfulBlockingEvent.Invoke();
        }
    }
}
