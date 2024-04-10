using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public interface ISyncedActivitiesSource : IGameObjectLink
{
    NetworkObject NetworkObject { get; }

    Animator animator { get; }

    CharacterPermission permissions { get; set; }
    
    public bool IsServer => NetworkManager.Singleton.IsServer;
    public bool IsOwner => NetworkObject.IsOwner;
}

public abstract class SyncedActivities<T> : NetworkBehaviour where T : ISyncedActivitiesSource
{
    [Space]
    [Header("Input")]
    [SerializeField]
    private InputActionReference inputAction;

    public T Invoker { 
        get 
        {
            if (m_invoker == null)
            {
                return m_invoker = ResearchInvoker();
            }

            return m_invoker;
        } 
        private set => m_invoker = value;
    }
    
    public bool IsPressed 
    {
        get => network_isPressed.Value;
        set
        {
            if (IsOwner && IsSpawned)
            {
                network_isPressed.Value = value;
            }
        }
    }
    
    private NetworkVariable<bool> network_isPressed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    private T m_invoker;

    protected abstract void OnStateChanged(bool IsPressed);

    public override void OnNetworkSpawn ()
    {
        base.OnNetworkSpawn();

        Subscribe();
    }
    public override void OnNetworkDespawn ()
    {
        base.OnNetworkDespawn();

        Unsubscribe();
    }

    private void Subscribe ()
    {
        if (IsOwner && inputAction != null)
        {
            inputAction.action.Enable();
            inputAction.action.started += OnInputPressStateChanged_Event;
            inputAction.action.performed += OnInputPressStateChanged_Event;
            inputAction.action.canceled += OnInputPressStateChanged_Event;
        }

        network_isPressed.OnValueChanged += InvokeStateChangedFunction_Event;
    }
    private void Unsubscribe ()
    {
        if (IsOwner && inputAction != null)
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
        OnStateChanged(New);
    }

    private T ResearchInvoker ()
    {
        var result = GetComponentInParent<T>();

        if (result == null)
        {
            Debug.LogWarning($"Unable to find activityes source {typeof(T).Name}");
        }

        return result;
    }
}
