using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Cysharp.Threading.Tasks;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public interface ISyncedActivitiesSource : IGameObjectLink
{
    NetworkIdentity netIdentity { get; }

    Animator animator { get; }

    SyncedActivitiesList activities { get; }

    CharacterPermission permissions { get; set; }
    
    public bool isServer => netIdentity.isServer;
    public bool isLocalPlayer => netIdentity.isLocalPlayer;
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
    
    [SerializeField, SyncVar]
    public bool isPerforming = true;

    [SerializeField]
    public InputActionReference inputAction;

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

    [HideInInspector]
    public ISyncedActivitiesSource Source;

    public bool IsPressed {
        get => isPressed;
        set {
            if (isServer)
            {
                isPressed = value;
            }
            else
            {
                if (!this.IsUnityNull() && NetworkClient.active)
                {
                    SetPressState(value);
                }
            }
        }
    }

    [SyncVar(hook = nameof(OnStateChangedHook))]
    private bool isPressed;

    public bool IsInProcess => process != null;

    private SyncedActivitySource syncedActivityOverrider;

    private Coroutine process = null;

    public void SetPerformingState(bool state)
    {
        isPerforming = state;
    }
    public bool HasOverrides()
    {
        if (syncedActivityOverrider.IsUnityNull() || !syncedActivityOverrider.isPerforming || (int)syncedActivityOverrider.Priority <= (int)Priority)
        {
            syncedActivityOverrider = regsitredSyncedActivities.Find(
                activity => 
                    activity.inputAction == inputAction && 
                    (int)activity.Priority > (int)Priority &&
                    System.Object.ReferenceEquals(activity.Source, Source) &&
                    activity.isPerforming);
        }

        return !syncedActivityOverrider.IsUnityNull();
    }    

    public override void OnStartClient()
    {
        base.OnStartClient();

        Subscribe();
    }
    public override void OnStopClient()
    {
        base.OnStopClient();

        Unsubscribe();
    }

    protected virtual void Awake()
    {
        Register();
    }
    protected virtual void OnDestroy()
    {
        regsitredSyncedActivities.Remove(this);
    }


    public virtual void Play()
    {
        if (!IsInProcess)
        {
            if (isServer)
            {
                Play_ClientRpc();
            }

            process = StartCoroutine(SubProcessRoutine());

            Source?.activities.Add(this);            
        }
    }

    public void Stop()
    {
        Stop(true);
    }   
    public virtual void Stop(bool interuptProcess)
    {
        if (IsInProcess)
        {
            if (isServer)
            {
                Stop_ClientRpc();
            }
            
            permissions = CharacterPermission.Default;
            
            if (interuptProcess)
            {
                StopCoroutine(process);
                StopAllCoroutines();
            }

            process = null;

            Source.activities.Remove(this);
        }
    }


    public abstract IEnumerator Process();

    protected virtual void OnStateChanged(bool IsPressed)
    {
        if (isServer && !HasOverrides() && IsPressed && !this.IsUnityNull())
        {
            Play();
        }
    }

    private IEnumerator SubProcessRoutine()
    {
        yield return Process();

        Stop(false);

        process = null;
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
        if (isOwned && inputAction != null)
        {
            inputAction.action.Enable();
            inputAction.action.started += OnInputPressStateChanged_Event;
            inputAction.action.performed += OnInputPressStateChanged_Event;
            inputAction.action.canceled += OnInputPressStateChanged_Event;
        }
    }
    private void Unsubscribe ()
    {        
        if (isOwned && inputAction != null)
        {
            inputAction.action.Disable();
            inputAction.action.started -= OnInputPressStateChanged_Event;
            inputAction.action.performed -= OnInputPressStateChanged_Event;
            inputAction.action.canceled -= OnInputPressStateChanged_Event;
        }
    }

    private void OnInputPressStateChanged_Event(CallbackContext callback)
    {
        IsPressed = callback.ReadValueAsButton(); 
    }
    private void OnStateChangedHook(bool Old, bool New)
    {
        if (HasOverrides()) 
            return;

        OnStateChanged(New);
    }

    [ClientRpc]
    private void Play_ClientRpc()
    {
        if (!isServer)
        {
            Play();
        }
    }
    [ClientRpc]
    private void Stop_ClientRpc()
    {
        if (!isServer)
        {
            Stop();
        }
    }

    [Client, Command]
    private void SetPressState(bool value)
    {
        if (isPressed != value && !isClient)
        {
            OnStateChanged(value);
        }

        isPressed = value;
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