using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public abstract class SyncedActivities : NetworkBehaviour
{
    [Space]
    [Header("Input")]
    [SerializeField]
    private InputActionReference inputAction;

    [HideInInspector]
    public NetworkCharacter Player = null;
    
    public bool IsPressed 
    {
        get => network_isPressed.Value;
        set
        {
            if (IsOwner)
            {
                network_isPressed.Value = value;
            }
        }
    }
    

    private NetworkVariable<bool> network_isPressed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    protected abstract void OnStateChanged(bool IsPressed);

    public override void OnNetworkSpawn()  
    {
        base.OnNetworkSpawn();

        ResearchPlayer();

        Subscribe();
    }
    public override void OnNetworkDespawn()  
    {
        base.OnNetworkDespawn();

        Unsubscribe();
    }

    private void Subscribe()
    {
        if (IsOwner)
        {
            inputAction.action.Enable();
            inputAction.action.started += OnInputPressStateChanged_Event;
            inputAction.action.performed += OnInputPressStateChanged_Event;
            inputAction.action.canceled += OnInputPressStateChanged_Event;
        }

        network_isPressed.OnValueChanged += InvokeStateChangedFunction_Event;
    }
    private void Unsubscribe()
    {
        if (IsOwner)
        {
            inputAction.action.Enable();
            inputAction.action.started -= OnInputPressStateChanged_Event;
            inputAction.action.performed -= OnInputPressStateChanged_Event;
            inputAction.action.canceled -= OnInputPressStateChanged_Event;
        }

        network_isPressed.OnValueChanged -= InvokeStateChangedFunction_Event;
    }

    private void OnInputPressStateChanged_Event(CallbackContext callback)
    {
        IsPressed = callback.ReadValueAsButton();
    }
    private void InvokeStateChangedFunction_Event(bool Old, bool New)
    {
        ResearchPlayer();

        OnStateChanged(New);
    }
    private void ResearchPlayer()
    {
        if (Player == null)
        {
            Player = GetComponentInParent<NetworkCharacter>();
        }
    }
}
