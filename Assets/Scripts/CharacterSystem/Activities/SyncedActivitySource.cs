using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Cysharp.Threading.Tasks;
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

public abstract class SyncedActivitySource : NetworkBehaviour
{
    public enum SyncedActivityPriority : byte
    {
        LessNormal = 1,
        Normal = 2,
        High = 3,
        Highest = 4,
        Unoverridable = 5,
    }

    private static List<SyncedActivitySource> regsitredSyncedActivities = new ();

    [Space]
    [Header("Input")]
    [SerializeField]
    private InputActionReference inputAction;

    [SerializeField]
    private SyncedActivityPriority Priority = SyncedActivityPriority.Normal;

    [SerializeField, Range(-4, 10)]
    public float SpeedChange = 0;

    [SerializeField]
    private bool IsPerformingAsDefault = true;
    
    public CharacterPermission Permissions { 
        get => permissions;
        set {
            permissions = value;

            onPermissionsChanged?.Invoke(value);
        }
    }  

    public event Action<CharacterPermission> onPermissionsChanged;

    private CharacterPermission permissions = CharacterPermission.Default;  

    

    public ISyncedActivitiesSource Source { 
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
    public bool IsInProcess => process != null;

    private NetworkVariable<bool> network_isPerforming = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> network_isPressed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    private ISyncedActivitiesSource m_invoker;
    private SyncedActivitySource syncedActivityOverrider;

    private Coroutine process = null;


    public bool HasOverrides()
    {
        if (syncedActivityOverrider.IsUnityNull() || !syncedActivityOverrider.IsPerforming || (int)syncedActivityOverrider.Priority <= (int)Priority)
        {
            syncedActivityOverrider = regsitredSyncedActivities.Find(
                activity => 
                    activity.inputAction == inputAction && 
                    (int)activity.Priority > (int)Priority &&
                    System.Object.ReferenceEquals(activity.Source, Source) && 
                    !activity.Source.IsUnityNull() &&
                    activity.IsPerforming);
        }

        return !syncedActivityOverrider.IsUnityNull();
    }    

    public override void OnNetworkSpawn ()
    {
        base.OnNetworkSpawn();

        Subscribe();
        
        IsPerforming = IsPerformingAsDefault;
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
            Source.activities.Add(this);
            
            Play_ClientRpc();
        }
    }
    public virtual void Stop(bool interuptProcess = true)
    {
        if (IsServer)
        {
            if (IsInProcess)
            {
                permissions = CharacterPermission.Default;
                
                if (interuptProcess)
                {
                    StopCoroutine(process);
                    StopAllCoroutines();
                }

                process = null;

                Source.activities.Remove(this);
            }           

            Stop_ClientRpc(interuptProcess);
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
        yield return Process();

        Stop(false);
    }

    private void Register ()
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
            if (process != null)
            {
                StopCoroutine(process);
                process = null;
            }

            process = StartCoroutine(ProcessRoutine());

            Source?.activities.Add(this);

            Play();
        }
    }
    [ClientRpc]
    private void Stop_ClientRpc(bool interuptProcess)
    {
        if (!IsServer)
        {
            Stop(interuptProcess);
        
            if (IsInProcess && interuptProcess)
            {
                StopCoroutine(process);
                Source?.activities.Remove(this);
            }

            process = null;
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
    IEnumerable<SyncedActivitySource>
{
    public delegate void OnSyncedActivityListChangedDelegate(EventType type, SyncedActivitySource syncedActivity);

    public enum EventType {
        Add,
        Remove
    }
    

    public event OnSyncedActivityListChangedDelegate onSyncedActivityListChanged;

    public ReadOnlyCollection<SyncedActivitySource> SyncedActivities => syncedActivities.AsReadOnly();

    public SyncedActivitySource this [int index] => syncedActivities[index];

    private List<SyncedActivitySource> syncedActivities = new();

    public CharacterPermission CalculatePermissions()
    {
        var result = CharacterPermission.Default;

        foreach (var activity in this)
        {
            result &= activity.Permissions;

            if (activity.Permissions.HasFlag(CharacterPermission.Untouchable))
            {
                result |= CharacterPermission.Untouchable;
            }

            if (activity.Permissions.HasFlag(CharacterPermission.Unpushable))
            {
                result |= CharacterPermission.Unpushable;
            }
        }

        return result;
    }

    public void Add(SyncedActivitySource syncedActivity)
    {
        if (syncedActivity != null)
        {
            syncedActivities.Add(syncedActivity);

            onSyncedActivityListChanged?.Invoke(EventType.Add, syncedActivity);
        }
    }
    public void Remove(SyncedActivitySource syncedActivity)
    {
        if (syncedActivity != null)
        {
            syncedActivities.Remove(syncedActivity);

            onSyncedActivityListChanged?.Invoke(EventType.Remove, syncedActivity);
        }
    }

    public IEnumerator<SyncedActivitySource> GetEnumerator() => syncedActivities.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => syncedActivities.GetEnumerator();
}