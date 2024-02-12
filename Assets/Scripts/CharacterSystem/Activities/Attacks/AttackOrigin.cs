using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

namespace CharacterSystem.Attacks
{
    public abstract class AttackOrigin : SyncedActivities
    {
        public enum AttackTimingStatement
        {
            Waiting,
            BeforeAttack,
            Attack,
            AfterAttack,
        }

        [SerializeField]
        private NetworkVariable<bool> network_isPerforming = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [SerializeField]
        private bool IsInterruptable = false;

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

        public AttackTimingStatement currentAttackStatement { get; protected set; }

        private Coroutine attackProcess = null;


        public void StartAttack()
        {
            if (attackProcess == null && IsPerforming)
            {
                attackProcess = StartCoroutine(AttackProcessRoutine());
            }
        }
        public void EndAttack()
        {
            if (attackProcess != null)
            {
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
    }
}
