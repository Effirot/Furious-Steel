using System.Collections;
using CharacterSystem.Attacks;
using CharacterSystem.Objects;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class DodgeActivity : SyncedActivitySource<NetworkCharacter>
{
    [Space]
    [Space]
    [SerializeField, Range(0, 3f)]
    public float Duration = 0.13f;

    [SerializeField, Range(0, 10f)]
    public float Timeout = 3f;

    [SerializeField, Range(0, 10f)]
    public float Speed = 3f;

    [SerializeField, Range(0, 10f)]
    public int MaxDodgesCount = 3;

    [SerializeField]
    public string PlayAnimation = "Legs.Dash";

    [SerializeField]
    public CharacterPermission dodgePremission = CharacterPermission.AllowDodge | CharacterPermission.Untouchable | CharacterPermission.AllowAttacking | CharacterPermission.AllowBlocking | CharacterPermission.AllowPickUps;

    [Space]
    [SerializeField]
    public UnityEvent OnDodgeStart = new();

    [SerializeField]
    public UnityEvent OnDodgeStop = new();

    [SerializeField]
    public UnityEvent<int> OnDodgeReloaded = new();

    [SyncVar(hook = nameof(OnDodgeCountChanged))]
    private int internalDodgesCount = 0;

    private float internalTimeoutCounter = 0;

    public override IEnumerator Process()
    {
        internalDodgesCount--;

        OnDodgeStart.Invoke();

        Source.velocity = Vector3.zero;

        var fixedUpdate = new WaitForFixedUpdate();
        var time = 0f;
        var direction = transform.forward;

        var movementVector = direction * 10 * Speed * Time.fixedDeltaTime;

        Permissions = dodgePremission;

        while (time < Duration)
        {
            Source.characterController.Move(movementVector);
            
            time += Time.fixedDeltaTime;

            Source.animator.Play(PlayAnimation);
         
            yield return fixedUpdate;
        }

        Permissions = CharacterPermission.Default;

        Source.Push(movementVector);

        OnDodgeStop.Invoke();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        Source.activities.onSyncedActivityListChanged += OnSourceActivitiesChanged_Event;

        internalDodgesCount = MaxDodgesCount;
    }
    public override void OnStopServer()
    {
        base.OnStartServer();

        Source.activities.onSyncedActivityListChanged -= OnSourceActivitiesChanged_Event;
    }

    public override void Play()
    {
        if (Source.permissions.HasFlag(CharacterPermission.AllowDodge) && internalDodgesCount > 0)
        {
            base.Play();
        }
    }
    public override void Stop(bool forced)
    {
        base.Stop(forced);
        
        StartCoroutine(TimeoutCounter());
    }

    private IEnumerator TimeoutCounter()
    {
        var fixedUpdate = new WaitForFixedUpdate();

        while (internalDodgesCount < MaxDodgesCount)
        {
            internalTimeoutCounter = Timeout;

            while (0 < internalTimeoutCounter)
            {            
                internalTimeoutCounter -= Time.fixedDeltaTime;

                yield return fixedUpdate;
            }

            internalDodgesCount++;
            
            OnDodgeReloaded.Invoke(internalDodgesCount);
        }

        internalDodgesCount = MaxDodgesCount;
    }

    private void OnDodgeCountChanged(int OldValue, int NewValue)
    {
        OnDodgeReloaded.Invoke(NewValue);
    }
    private void OnSourceActivitiesChanged_Event(SyncedActivitiesList.EventType type, SyncedActivitySource syncedActivity)
    {
        if (type == SyncedActivitiesList.EventType.Add && syncedActivity is not DodgeActivity)
        {
            Stop();
        }
    }
}
