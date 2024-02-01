using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using static UnityEngine.InputSystem.InputAction;

public abstract class AttackOrigin : NetworkBehaviour
{
    [SerializeField]
    private InputActionReference inputAction;

    [SerializeField]
    private bool _IsPerforming = true;

    [SerializeField]
    public NetworkCharacter Reciever = null;


    public bool IsPerforming { 
        get => _IsPerforming; 
        set 
        {
            _IsPerforming = value;

            if (IsOwner)
            {
                if (IsPerforming)
                {
                    IsPressed = inputAction.action.IsPressed();
                }
                else
                {
                    IsPressed = false;

                    if (IsAttacking)
                    {
                        EndAttack();
                    }
                }
            }
        } 
    }

    public bool IsAttacking => attackProcess != null;

    public bool IsPressed {
        get => network_isPressed.Value;
        set => network_isPressed.Value = value;
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

    public override void OnNetworkSpawn()
    {
        network_isPressed.OnValueChanged += OnPressStateChanged;
        
        if (inputAction != null && IsOwner)
        {
            inputAction.action.Enable();

            inputAction.action.performed += SetPressState_event;
            inputAction.action.canceled += SetPressState_event;
        }
    }

    protected virtual void OnPressStateChanged(bool OldValue, bool NewValue)
    {
        if (NewValue && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject  == null)
        {
            StartAttack();
        }
    }

    protected abstract IEnumerator AttackProcessRoutine(); 

    protected void EndAttack()
    {
        if (attackProcess != null)
        {
            StopCoroutine(attackProcess);
        }

        attackProcess = null;
    }

    private void SetPressState_event(CallbackContext callback)
    {
        if (IsPerforming)
        {
            IsPressed = !callback.canceled;
        }
    }
}

