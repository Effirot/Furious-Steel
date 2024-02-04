using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using static UnityEngine.InputSystem.InputAction;

namespace CharacterSystem.Attacks
{
    public abstract class AttackOrigin : SyncedActivities
    {
        [SerializeField]
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

        private Coroutine attackProcess = null;

        private NetworkVariable<bool> network_isPressed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
       
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
            if (value && IsPerforming)
            {
                StartAttack();
            }
        }


        protected abstract IEnumerator AttackProcessRoutine(); 
    }
}
