using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public interface ISyncedActivitiesSource : IMonoBehaviourLink
{
    NetworkObject NetworkObject { get; }

    Animator animator { get; }

    CharacterPermission permissions { get; set; }
    
    public bool IsServer => NetworkObject.NetworkManager.IsServer;
    public bool IsOwner => NetworkObject.IsOwner;

}

public abstract class SyncedActivities<T> : NetworkBehaviour where T : ISyncedActivitiesSource
{
    [Space]
    [Header("Input")]
    [SerializeField]
    private InputActionReference inputAction;

    [HideInInspector]
    public T Invoker = default;
    
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

        ResearchSource();

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
        ResearchSource();

        OnStateChanged(New);
    }
    private void ResearchSource()
    {
        if (Invoker == null)
        {
            Invoker = GetComponentInParent<T>();

            if (Invoker == null)
            {
                Debug.Log($"Unable to find activityes source {typeof(T).Name}");
            }
        }

        
    }
}
