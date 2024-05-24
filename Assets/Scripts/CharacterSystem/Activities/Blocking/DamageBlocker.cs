using System;
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

        public event Action<bool> onStunStateChanged;
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
        private float PushForce = 0.5f;


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
            if (damage.type == Damage.Type.Parrying || damage.type == Damage.Type.Unblockable || damage.type == Damage.Type.Effect) 
                return false;

            if (IsBlockInProcess)
            {
                if (InterruptOnHit)
                {
                    StopBlock_ClientRpc();

                    if (!IsClient)
                    {
                        StopBlockProcess();
                    }
                }
                
                if (IsServer)
                {
                    ExecuteSuccesfullyBlockEvent_ClientRpc();

                    if (!IsClient)
                    {
                        OnSuccesfulBlockingEvent.Invoke();
                    }
                }

                if (damage.sender != null && damage.type != Damage.Type.Magical && damage.type != Damage.Type.Balistic)
                {                    
                    backDamage.sender = Invoker;
                    backDamage.pushDirection = Vector3.up * 0.3f + -damage.pushDirection.normalized * PushForce;
                    backDamage.type = Damage.Type.Parrying;

                    Damage.Deliver(damage.sender, backDamage);
                }
                damage *= 1f - DamageReducing;

                return true;
            }

            return false;
        } 

        public void StartBlockProcess()
        {
            if (!Invoker.permissions.HasFlag(CharacterPermission.AllowBlocking))
                return;

            if (HasOverrides())
                return;

            if (Invoker.isStunned)
                return;


            Invoker.Push(Vector3.up * 0.01f);

            foreach (var attacks in Invoker.gameObject.GetComponentsInChildren<DamageSource>())
            {
                attacks.EndAttackLocaly();
            }

            StopBlockProcess();

            Invoker.Blocker = this;

            BlockProcessRoutine = StartCoroutine(BlockProcess());
        }
        public void StopBlockProcess()
        {
            if (BlockProcessRoutine != null)
            {
                StopCoroutine(BlockProcessRoutine);
                
                Invoker.animator.SetBool("Blocking", false);
                Invoker.Speed += SpeedReducing;
                Invoker.permissions = CharacterPermission.Default;
            
                IsBlockInProcess = false;
            }
            
            BlockProcessRoutine = null;
        }

        private IEnumerator BlockProcess()
        {
            Invoker.Speed -= SpeedReducing;
            Invoker.permissions = BeforeBlockCharacterPermissions;

            OnBeforeBlockingEvent.Invoke();

            yield return new WaitForSeconds(BeforeBlockTime);

            IsBlockInProcess = true;
            Invoker.permissions = BlockCharacterPermissions;
            OnBlockingEvent.Invoke();

            Invoker.animator.SetBool("Blocking", true);

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

            Invoker.animator.SetBool("Blocking", false);


            IsBlockInProcess = false;
            Invoker.permissions = AfterBlockCharacterPermissions;
            OnAfterBlockingEvent.Invoke();

            yield return new WaitForSeconds(AfterBlockTime);

            StopBlockProcess();
        }

        protected override void OnStateChanged(bool IsPressed)
        {
            if (IsPressed && BlockProcessRoutine == null)
            {   
                StartBlockProcess();
            }
        }

        [ClientRpc]
        private void StopBlock_ClientRpc()
        {
            StopBlockProcess();
        }

        [ClientRpc]
        private void ExecuteSuccesfullyBlockEvent_ClientRpc()
        {
            OnSuccesfulBlockingEvent.Invoke();
        }
    }
}
