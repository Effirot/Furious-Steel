using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

// WinBlocker HAHAHAHA

namespace CharacterSystem.Blocking
{
        
    public interface IDamageBlocker : 
        ISyncedActivitiesSource, 
        IDamagable,
        IDamageSource
    {
        DamageBlocker Blocker { set; }
    }

    public class DamageBlocker : SyncedActivities<IDamageBlocker>
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
        [SerializeField, Range(0f, 9f)]
        private float SpeedReducing = 4;

        [SerializeField]
        private bool InterruptOnHit = true;

        [Space]
        [Header("Damage")]
        [SerializeField]
        private Damage backDamage;

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


        public bool IsBlockInProcess { get; private set; }

        private Coroutine BlockProcessRoutine = null;


        public virtual bool Block(ref Damage damage)
        {
            if (damage.type == Damage.Type.Parrying || damage.type == Damage.Type.Unblockable) 
                return false;

            if (IsBlockInProcess)
            {
                OnSuccesfulBlockingEvent.Invoke();

                var reducingPercent = 1f - DamageReducing;

                if (damage.sender != null)
                {
                    var sendedBackDamage = backDamage; 
                    sendedBackDamage.sender = Invoker;
                    sendedBackDamage.pushDirection = -damage.pushDirection.normalized * DamageReducing * 2;
                    sendedBackDamage.type = Damage.Type.Parrying;

                    Damage.Deliver(damage.sender, sendedBackDamage);
                }
                damage *= reducingPercent;

                return damage.value == 0;
            }

            if (InterruptOnHit)
            {
                StopBlockProcess();
            }

            return false;
        } 

        private IEnumerator BlockProcess()
        {
            OnBeforeBlockingEvent.Invoke();

            yield return new WaitForSeconds(BeforeBlockTime);

            IsBlockInProcess = true;
            Invoker.permissions = BlockCharacterPermissions;
            OnBlockingEvent.Invoke();

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

            IsBlockInProcess = false;
            Invoker.permissions = AfterBlockCharacterPermissions;
            OnAfterBlockingEvent.Invoke();

            yield return new WaitForSeconds(AfterBlockTime);

            StopBlockProcess();
        }
        private void StartBlockProcess()
        {
            if (!Invoker.permissions.HasFlag(CharacterPermission.AllowBlocking))
                return;

            StopBlockProcess();

            Invoker.Blocker = this;

            BlockProcessRoutine = StartCoroutine(BlockProcess());

            Invoker.animator.SetBool("Blocking", true);
            Invoker.Speed -= SpeedReducing;
            Invoker.permissions = BeforeBlockCharacterPermissions;
        }
        private void StopBlockProcess()
        {
            if (BlockProcessRoutine != null)
            {
                StopCoroutine(BlockProcessRoutine);
                
                BlockProcessRoutine = null;

                Invoker.animator.SetBool("Blocking", false);
                Invoker.stunlock = 0;
                Invoker.Speed += SpeedReducing;
                Invoker.permissions = CharacterPermission.All;
            }

            IsBlockInProcess = false;
        }

        protected override void OnStateChanged(bool IsPressed)
        {
            if (IsPressed && BlockProcessRoutine == null)
            {   
                StartBlockProcess();
            }
        }

    }
}
