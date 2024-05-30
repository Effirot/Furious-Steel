using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    SyncedActivitiesList activities { get; }

    CharacterPermission permissions { get; set; }
    
    public bool IsServer => NetworkManager.Singleton.IsServer;
    public bool IsOwner => NetworkObject.IsOwner;
}

public abstract class SyncedActivity : NetworkBehaviour
{
    public enum SyncedActivityPriority : byte
    {
        LessNormal = 1,
        Normal = 2,
        High = 3,
        Highest = 4,
        Unoverridable = 5,
    }

    private static List<SyncedActivity> regsitredSyncedActivities = new ();

    [Space]
    [Header("Input")]
    [SerializeField]
    private InputActionReference inputAction;

    [SerializeField]
    private SyncedActivityPriority Priority = SyncedActivityPriority.Normal;

    [SerializeField, Range(-4, 10)]
    public float SpeedChange = 0;   
    
    public CharacterPermission Permissions { 
        get => permissions;
        set {
            permissions = value;

            onPermissionsChanged?.Invoke(value);
        }
    }  

    public event Action<CharacterPermission> onPermissionsChanged;

    private CharacterPermission permissions = CharacterPermission.Default;  

    

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

    public bool IsInProcess => process != null;

    private NetworkVariable<bool> network_isPressed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    private ISyncedActivitiesSource m_invoker;
    private SyncedActivity syncedActivityOverrider;

    private Coroutine process = null;


    public bool HasOverrides()
    {
        if (syncedActivityOverrider.IsUnityNull())
        {
            syncedActivityOverrider = regsitredSyncedActivities.Find(
                activity => 
                    activity.inputAction == inputAction && 
                    (int)activity.Priority > (int)Priority &&
                    activity.Invoker == Invoker);
        }

        return !syncedActivityOverrider.IsUnityNull();
    }
    


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

    public override void OnDestroy()
    {
        base.OnDestroy();

        regsitredSyncedActivities.Remove(this);

        Stop();
    }
    
    protected virtual void Awake()
    {
        Register();
    }

    public virtual void Play()
    {
        if (!HasOverrides() && IsServer && !IsInProcess)
        {
            process = StartCoroutine(ProcessRoutine());
            Invoker.activities.Add(this);
            
            Play_ClientRpc();
        }
    }
    public virtual void Stop() 
    {
        if (IsInProcess)
        {
            StopCoroutine(process);
            Invoker.activities.Remove(this);
        }

        process = null;

        if (IsServer)
        {
            Stop_ClientRpc();
        }
    }

    public abstract IEnumerator Process();

    protected virtual void OnStateChanged(bool IsPressed)
    {
        if (IsPressed)
        {
            Play();
        }
    }

    private IEnumerator ProcessRoutine()
    {
        yield return StartCoroutine(Process());

        Stop();
    }

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

    [ClientRpc]
    private void Play_ClientRpc()
    {
        if (!IsServer)
        {
            process = StartCoroutine(ProcessRoutine());
            Invoker.activities.Add(this);

            Play();
        }
    }
    [ClientRpc]
    private void Stop_ClientRpc()
    {
        if (!IsServer)
        {
            Stop();
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

public sealed class SyncedActivitiesList : 
    IEnumerable<SyncedActivity>
{
    public delegate void OnSyncedActivityListChangedDelegate(EventType type, SyncedActivity syncedActivity);

    public enum EventType {
        Add,
        Remove
    }
    

    public event OnSyncedActivityListChangedDelegate onSyncedActivityListChanged;

    public ReadOnlyCollection<SyncedActivity> SyncedActivities => syncedActivities.AsReadOnly();

    public SyncedActivity this [int index] => syncedActivities[index];

    private List<SyncedActivity> syncedActivities = new();

    public CharacterPermission CalculatePermissions()
    {
        var result = CharacterPermission.Default;

        foreach (var activity in this)
        {
            result &= activity.Permissions;
        }

        return result;
    }

    public void Add(SyncedActivity syncedActivity)
    {
        if (syncedActivity != null)
        {
            syncedActivities.Add(syncedActivity);

            onSyncedActivityListChanged?.Invoke(EventType.Add, syncedActivity);
        }
    }
    public void Remove(SyncedActivity syncedActivity)
    {
        if (syncedActivity != null)
        {
            syncedActivities.Remove(syncedActivity);

            onSyncedActivityListChanged?.Invoke(EventType.Remove, syncedActivity);
        }
    }

    public IEnumerator<SyncedActivity> GetEnumerator() => syncedActivities.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => syncedActivities.GetEnumerator();
}