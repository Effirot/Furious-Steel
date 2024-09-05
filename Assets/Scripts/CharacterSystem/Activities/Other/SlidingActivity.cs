using System;
using System.Collections;
using System.Linq;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

using static CharacterSystem.Attacks.AttackActivity;

public class SlidingActivity : SyncedActivitySource<NetworkCharacter>
{
    [Space]
    [SerializeField]
    public CharacterPermission onSlidePremissions = CharacterPermission.AllowGravity | CharacterPermission.AllowJump | CharacterPermission.AllowDodge | CharacterPermission.AllowAttacking;

    [SerializeField]
    private WorkOnlyCondition workOnlyCondition;
    
    [SerializeField, Range(0, 1)]
    private float friction = 0.1f;
    
    [SerializeField, Range(0, 10f)]
    private float backwardFrictionMultiplyier = 3f;
    
    [SerializeField]
    private Vector3 pushWhenStart = Vector3.forward;
    
    [SerializeField]
    private Damage ramDamage = new Damage(10, 0, 0.5f, Vector3.forward + Vector3.up, Damage.Type.Physics);


    [SerializeField]
    private string animationPath = "";
    
    [SerializeField]
    public AudioSource audioSource;

    [Space]
    [SerializeField]
    public UnityEvent onSlideStart = new();
    [SerializeField]
    public UnityEvent onSlideStop = new();
    [SerializeField]
    public UnityEvent onGrounded = new();
    [SerializeField]
    public UnityEvent onAir = new();

    [SyncVar]
    private Vector3 localVelocity = Vector3.zero / 3;
    private float stopTimeout = 0;


    private bool IsActive =>
        workOnlyCondition switch
            {
                WorkOnlyCondition.Always => true,
                
                WorkOnlyCondition.OnGround => Source.IsGrounded,
                WorkOnlyCondition.OnAir => !Source.IsGrounded,
                
                WorkOnlyCondition.OnDodge => Source.activities.Any(activity => activity is DodgeActivity),
                WorkOnlyCondition.OnNotDodge => !Source.activities.Any(activity =>  activity is DodgeActivity),
                
                _ => throw new NotImplementedException(),
            };


    public override IEnumerator Process()
    {
        var waitForFixedUpdate = new WaitForFixedUpdate();
        Permissions = onSlidePremissions;

        audioSource?.Play();
        while (IsPressed)
        {
            if (!string.IsNullOrEmpty(animationPath))
            {
                Source.animator.Play(animationPath, -1, 0.1f);
            }

            Source.velocity = CalculateSlideVelocity();

            audioSource.volume = Mathf.Clamp01(localVelocity.magnitude);

            yield return waitForFixedUpdate;

            if (isServer)
            {
                if (localVelocity.magnitude < 0.05f)
                {
                    stopTimeout += Time.fixedDeltaTime * Source.LocalTimeScale * Source.PhysicTimeScale;
                
                    if (stopTimeout > 1f)
                    {
                        break;
                    }
                }
                else
                {
                    stopTimeout = 0;
                }
            }
        }
        
        Permissions = CharacterPermission.Default;
    }

    private Vector3 CalculateSlideVelocity()
    {   
        var timescale = Time.fixedDeltaTime * Source.LocalTimeScale * Source.PhysicTimeScale * Time.timeScale;

        localVelocity = Vector3.Lerp(localVelocity, Physics.gravity, 0.2f * timescale);

        if (!(audioSource.mute = Source.controllerCollision == null))
        {
            onGrounded.Invoke();

            var angle = Quaternion.FromToRotation(Source.controllerCollision.normal, Vector3.down); 
        
            var delatVelocity = angle * localVelocity;
            delatVelocity.y = Mathf.Min(0, delatVelocity.y);

            var forward = angle * transform.forward;
            var forwardFrictionMultiplyier = friction * (1f + (Vector3.Angle(forward, delatVelocity) / 180f * backwardFrictionMultiplyier));

            delatVelocity.x = Mathf.MoveTowards(delatVelocity.x, 0, forwardFrictionMultiplyier * 10 * timescale);
            delatVelocity.z = Mathf.MoveTowards(delatVelocity.z, 0, forwardFrictionMultiplyier * 10 * timescale);
            localVelocity = Quaternion.Inverse(angle) * delatVelocity;

            if (localVelocity.magnitude > 0.5f && Source is IAttackSource)
            {
                var attackSource = Source as IAttackSource;
                var damage = ramDamage;
                damage.sender = attackSource;
                damage.pushDirection = localVelocity * ramDamage.pushDirection.magnitude;
                
                var report = Damage.Deliver(Source.controllerCollision.transform, damage);

                if (report.isDelivered)
                {
                    attackSource.DamageDelivered(report);
                }
            }
        }
        else
        {
            onAir.Invoke();
        }

        return localVelocity;
    }

    public override void Play()
    {
        if (!IsInProcess && IsActive)
        {
            localVelocity = transform.rotation * pushWhenStart;

            stopTimeout = 0;
            Source.slopeAngle -= 1000; 

            var material = Source.characterController.sharedMaterial;
            if (material == null)
            {
                material = new PhysicsMaterial();
            }

            material.dynamicFriction = 0;

            Source.characterController.sharedMaterial = material;

            base.Play();

            onSlideStart.Invoke();
            Source.activities.onSyncedActivityListChanged += StopSlidingOnAttack_Event;
        }
    }
    public override void Stop(bool interuptProcess)
    {
        if (IsInProcess)
        {
            Source.slopeAngle += 1000; 

            base.Stop(interuptProcess);
            Source.activities.onSyncedActivityListChanged -= StopSlidingOnAttack_Event;

            onSlideStop.Invoke();
        }

        audioSource?.Stop();
    }

    private void StopSlidingOnAttack_Event(SyncedActivitiesList.EventType type, SyncedActivitySource syncedActivity)
    {
        if (type is SyncedActivitiesList.EventType.Add)
        {
            Stop(true);
        }
    }

    private void OnDrawGizmos()
    {
        if (IsInProcess && Source.controllerCollision != null)
        {
            Gizmos.DrawRay(transform.position, Source.controllerCollision.normal);
        }
    }
}
