using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

// WinBlocker HAHAHAHA

namespace CharacterSystem.Blocking
{
    public class DamageBlocker : SyncedActivities
    {
        [Space]
        [Header("Stun")]
        [SerializeField]
        private bool StunBeforeBlockTime = true;
        [SerializeField, Range(0f, 5f)]
        private float BeforeBlockTime = 0.5f;
        
        [SerializeField]
        private bool StunAtBlockProcessTime = true;
        [SerializeField, Range(0f, 5f)]
        private float BlockProcessTime = 0.5f;

        [SerializeField]
        private bool StunAfterBlockTime = false;
        [SerializeField, Range(0f, 5f)]
        private float AfterBlockTime = 0.5f;

        [Space]
        [Header("Animation")]
        [SerializeField]
        private string animationName = "";

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
            if (IsBlockInProcess && Invoker.permissions.HasFlag(CharacterPermission.AllowBlocking))
            {
                OnSuccesfulBlockingEvent.Invoke();

                if (damage.sender != null)
                {
                    backDamage.sender = Invoker.gameObject;
                    backDamage.pushDirection = damage.sender.transform.position - transform.position;
                    
                    Damage.Deliver(damage.sender, backDamage);
                }
                damage *= 1f - DamageReducing;

                return damage.value == 0;
            }

            StopBlockProcess();

            return false;
        } 

        private IEnumerator BlockProcess()
        {
            Invoker.animator.Play(animationName);

            Invoker.Blocker = this;

            if (StunBeforeBlockTime)
            {
                Invoker.stunlock = BeforeBlockTime;
            }
            OnBeforeBlockingEvent.Invoke();
            yield return new WaitForSeconds(BeforeBlockTime);
            
            if (StunAtBlockProcessTime)
            {
                Invoker.stunlock = BlockProcessTime;
            }
            IsBlockInProcess = true;
            OnBlockingEvent.Invoke();
            yield return new WaitForSeconds(BlockProcessTime);
            IsBlockInProcess = false;

            if (StunAfterBlockTime)
            {
                Invoker.stunlock = AfterBlockTime;
            }
            OnAfterBlockingEvent.Invoke();
            yield return new WaitForSeconds(AfterBlockTime);

            BlockProcessRoutine = null;
        }
        private void StartBlockProcess()
        {
            if (BlockProcessRoutine != null)
            {
                StopCoroutine(BlockProcessRoutine);
             
                BlockProcessRoutine = null;
            }

            BlockProcessRoutine = StartCoroutine(BlockProcess());
        }
        private void StopBlockProcess()
        {
            if (BlockProcessRoutine != null)
            {
                StopCoroutine(BlockProcessRoutine);
                
                BlockProcessRoutine = null;

                Invoker.stunlock = 0;
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
