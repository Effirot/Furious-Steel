using System.Collections;
using CharacterSystem.Attacks;
using CharacterSystem.Objects;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class DodgeActivity : SyncedActivitySource<NetworkCharacter>
{
    [Space]
    [Header("Dodge duration")]
    [SerializeField, Range(0, 3f)]
    public float duration = 0.35f;

    [SerializeField]
    public CharacterPermission dodgePremission = CharacterPermission.AllowDodge | CharacterPermission.Untouchable | CharacterPermission.AllowAttacking | CharacterPermission.AllowBlocking | CharacterPermission.AllowPickUps;


    [Space]
    [Header("After dodge duration")]
    [SerializeField, Range(0, 3f)]
    public float afterDodgeDuration = 0.25f;

    [SerializeField]
    public CharacterPermission afterDodgePremission = CharacterPermission.AllowDodge | CharacterPermission.Untouchable | CharacterPermission.AllowAttacking | CharacterPermission.AllowBlocking | CharacterPermission.AllowPickUps;


    [Space]
    [Header("Timeout")]
    [SerializeField, Range(0, 10f)]
    public float Timeout = 3f;

    [SerializeField, Range(0, 10f)]
    public float Speed = 3f;

    [SerializeField, Range(0, 10f)]
    public int MaxDodgesCount = 3;

    [SerializeField]
    public string PlayAnimation = "Legs.Dash";


    [Space]
    [SerializeField]
    public UnityEvent onDodgeStart = new();

    [SerializeField]
    public UnityEvent onDodgeStop = new();

    [SerializeField]
    public UnityEvent<int> onDodgeCountChanged = new();

    public int DodgeCount {
        get => internalDodgesCount;
        set
        {
            if (isServerOnly)
            {
                OnDodgeCountChanged_Hook(internalDodgesCount, value);
            }

            internalDodgesCount = value;
        }
    } 

    [SyncVar(hook = nameof(OnDodgeCountChanged_Hook))]
    private int internalDodgesCount = 0;

    private Coroutine reloadProcess = null; 

    private float internalTimeoutCounter = 0;

    private float timescale => Time.fixedDeltaTime * Source.LocalTimeScale * Time.timeScale; 

    public override IEnumerator Process()
    {        
        var lookAngle = Quaternion.LookRotation(new Vector3(Source.lookVector.x, 0, Source.lookVector.y));

        var fixedUpdate = new WaitForFixedUpdate();
        var time = 0f;
        var direction = lookAngle * Vector3.forward;

        var movementVector = direction * 10 * Speed * timescale;

        DodgeCount--;

        Source.transform.rotation = lookAngle;
        onDodgeStart.Invoke();
        Permissions = dodgePremission;

        Source.SetPosition(Source.transform.position);

        while (time < duration)
        {
            Source.velocity = Vector3.zero;
            Source.characterController.Move(movementVector);
            
            time += timescale;

            Source.animator.Play(PlayAnimation);
         
            yield return fixedUpdate;
        }

        Source.SetPosition(Source.transform.position);

        Permissions = afterDodgePremission;
        yield return new WaitForSeconds(afterDodgeDuration * Source.LocalTimeScale * Time.timeScale);    
        
        Permissions = CharacterPermission.Default;
        Source.velocity = Vector3.zero;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        Source.activities.onSyncedActivityListChanged += OnSourceActivitiesChanged_Event;

        DodgeCount = MaxDodgesCount;
    }
    public override void OnStopServer()
    {
        base.OnStartServer();

        Source.activities.onSyncedActivityListChanged -= OnSourceActivitiesChanged_Event;
    }

    public override void Play()
    {
        if (Source.permissions.HasFlag(CharacterPermission.AllowDodge) && DodgeCount > 0 && !Source.isStunned)
        {
            Stop(true);

            base.Play();

            if (IsInProcess)
            {
                if (reloadProcess != null)
                {
                    StopCoroutine(reloadProcess);
                    reloadProcess = null;
                }
                reloadProcess = StartCoroutine(TimeoutCounter());
            }
        }
    }
    public override void Stop(bool interuptProcess)
    {
        onDodgeStop.Invoke();

        base.Stop(interuptProcess);
    }
    
    private IEnumerator TimeoutCounter()
    {
        var fixedUpdate = new WaitForFixedUpdate();

        while (DodgeCount < MaxDodgesCount)
        {
            internalTimeoutCounter = Timeout;

            while (0 < internalTimeoutCounter)
            {            
                internalTimeoutCounter -= Time.fixedDeltaTime;

                yield return fixedUpdate;
            }

            DodgeCount++;            
        }

        DodgeCount = MaxDodgesCount;
        reloadProcess = null;
    }

    private void OnDodgeCountChanged_Hook(int OldValue, int NewValue)
    {
        onDodgeCountChanged.Invoke(NewValue);
    }
    private void OnSourceActivitiesChanged_Event(SyncedActivitiesList.EventType type, SyncedActivitySource syncedActivity)
    {
        if (type == SyncedActivitiesList.EventType.Add && syncedActivity is not DodgeActivity)
        {
            Stop();
        }
    }
}
