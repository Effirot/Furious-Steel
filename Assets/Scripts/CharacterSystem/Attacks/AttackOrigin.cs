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
    public abstract class AttackOrigin : NetworkBehaviour
    {
        [SerializeField]
        private InputActionReference inputAction;

        [SerializeField]
        private NetworkVariable<bool> network_isPerforming = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [SerializeField]
        public NetworkCharacter Reciever = null;


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
        public bool IsPressed 
        {
            get => network_isPressed.Value;
            set => network_isPressed.Value = value;
        }

        private Coroutine attackProcess = null;

        private NetworkVariable<bool> network_isPressed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        
        public void StartAttack()
        {
            ResearchReciever();

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
            
            network_isPressed.OnValueChanged += OnPressStateChanged;
            network_isPerforming.OnValueChanged += OnPerformStateChanged;
            
            if (inputAction != null && IsOwner)
            {
                inputAction.action.Enable();

                inputAction.action.performed += SetPressState_event;
                inputAction.action.canceled += SetPressState_event;
            }
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            EndAttack();
        }

        protected virtual void Awake()
        {
            ResearchReciever();
        }
        protected virtual void OnPressStateChanged(bool OldValue, bool NewValue)
        {
            if (NewValue && IsPerforming)
            {
                StartAttack();
            }
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

        private void ResearchReciever()
        {
            if (Reciever == null)
            {
                Reciever = GetComponentInParent<NetworkCharacter>();
            }
        }

        protected abstract IEnumerator AttackProcessRoutine(); 

        private void SetPressState_event(CallbackContext callback)
        {
            IsPressed = !callback.canceled;
        }
    }
}
