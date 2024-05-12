using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using Unity.VisualScripting;
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

public abstract class SyncedActivities : NetworkBehaviour
{
    public enum SyncedActivityPriority : byte
    {
        LessNormal = 1,
        Normal = 2,
        High = 3,
        Highest = 4,
        Unoverridable = 5,
    }

    private static List<SyncedActivities> regsitredSyncedActivities = new ();

    [Space]
    [Header("Input")]
    [SerializeField]
    private InputActionReference inputAction;

    [SerializeField]
    private SyncedActivityPriority Priority = SyncedActivityPriority.Normal;

    public ISyncedActivitiesSource Invoker { 
        get 
        {
            return m_invoker;
        } 
        protected set => m_invoker = value;
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
    
    private ISyncedActivitiesSource m_invoker;
    private SyncedActivities syncedActivityOverrider;


    public bool HasOverrides()
    {
        if (!syncedActivityOverrider.IsUnityNull())
        {
            syncedActivityOverrider = regsitredSyncedActivities.Find(
                activity => 
                    activity.inputAction == this.inputAction && 
                    activity.Priority > this.Priority &&
                    activity.Invoker == this.Invoker);
        }

        return !syncedActivityOverrider.IsUnityNull();
    }
    
    public override void OnNetworkSpawn ()
    {
        base.OnNetworkSpawn();

        Register();

        Subscribe();
    }
    public override void OnNetworkDespawn ()
    {
        base.OnNetworkDespawn();

        Unsubscribe();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        regsitredSyncedActivities.Remove(this);
    }

    protected abstract void OnStateChanged(bool IsPressed);

    private void Register()
    {
        var index = 0;

        foreach(var activity in regsitredSyncedActivities)
        {
            if ((int)activity.Priority <= (int)this.Priority)
            {
                break;
            }

            index++;
        }

        regsitredSyncedActivities.Insert(index, this);
    }
    private void Subscribe ()
    {
        network_isPressed.OnValueChanged += InvokeStateChangedFunction_Event;
        
        if (IsOwner && inputAction != null)
        {
            inputAction.action.Enable();
            inputAction.action.started += OnInputPressStateChanged_Event;
            inputAction.action.performed += OnInputPressStateChanged_Event;
            inputAction.action.canceled += OnInputPressStateChanged_Event;
        }
    }
    private void Unsubscribe ()
    {
        network_isPressed.OnValueChanged -= InvokeStateChangedFunction_Event;
        
        if (IsOwner && inputAction != null)
        {
            inputAction.action.Enable();
            inputAction.action.started -= OnInputPressStateChanged_Event;
            inputAction.action.performed -= OnInputPressStateChanged_Event;
            inputAction.action.canceled -= OnInputPressStateChanged_Event;
        }
    }

    private void OnInputPressStateChanged_Event(CallbackContext callback)
    {
        IsPressed = callback.ReadValueAsButton();
    }
    private void InvokeStateChangedFunction_Event(bool Old, bool New)
    {
        if (HasOverrides()) return;

        OnStateChanged(New);
    }
}
