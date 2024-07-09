

using System.Collections;
using System.Security.Cryptography;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class ChargeRunningActivity : SyncedActivitySource<PlayerNetworkCharacter>
{
    [Space]
    [Header("Charging")]
    [SerializeField, Range(0, 35)]
    private float maxSpeed = 35;

    [SerializeField, Range(0.5f, 3)]
    private float speedAcelerationMultiplyer = 1.5f;
    
    [SerializeField, Range(0.01f, 50f)]
    private float lookVectorAceleration = 0.03f;

    [SerializeField, Range(1, 8)]
    private float stuckThresold = 4;

    [SerializeField, Range(0, 1)]
    private float collideAcelerationPercentRequirement = 0.4f;

    [SerializeField, Range(0, 50)]
    private float maxPushTime = 0f;

    [Space]
    [Header("Collider")]
    [SerializeField]
    private Vector3 colliderOffset = Vector3.forward; 
    [SerializeField]
    private Vector3 colliderScale = Vector3.one; 

    [SerializeField]
    private float explodeRange = 5; 

    [SerializeField]
    private Damage colliderDamage = new();

    [SerializeField]
    private Damage explodeDamage = new();


    [SerializeField]
    private Vector3 collideBackPush = new Vector3(0, 0.6f, -0.9f);


    [SerializeField]
    public UnityEvent onStartRunning = new();
    
    [SerializeField]
    public UnityEvent onCharging = new();

    [SerializeField]
    public UnityEvent onFullyCharging = new();
    
    [SerializeField]
    public UnityEvent onExplode = new();

    [SerializeField]
    public UnityEvent onStop = new();



    private float SpeedAceleration { 
        get => speedAceleration; 
        set {
            Source.Speed += value - speedAceleration;

            speedAceleration = value;
        }
    }
    private float speedAceleration = 0;
    
    [SyncVar]
    private Vector2 network_internal_lookVector = Vector2.zero;

    public override void Play()
    {
        if (Source.JumpButtonState && !IsInProcess && !Source.isStunned && Source.permissions.HasFlag(CharacterPermission.AllowAttacking))
        {
            SpeedAceleration = 0;

            base.Play();
        }
    }
    public override void Stop(bool interuptProcess = true)
    {
        onStop.Invoke();

        if (IsInProcess)
        {            
            SpeedAceleration = 0;
            
            Permissions = CharacterPermission.Default;
        }

        base.Stop(interuptProcess);
    }

    public override IEnumerator Process()
    {
        Permissions &= ~CharacterPermission.AllowJump;

        yield return new WaitUntil(() => Source.IsGrounded);

        Permissions = CharacterPermission.AllowGravity;
        
        onStartRunning.Invoke();
        
        SpeedAceleration = 0f;

        var waitForEndOfFrame = new WaitForEndOfFrame();
        
        var collision = false;
        var runAccelerationPercent = 0f;

        var pushTime = 0f;

        while (IsPressed && (pushTime < maxPushTime || maxPushTime == 0))
        {
            if (Source.isStunned)
                yield break;
                
            var newAcelerationPercent = SpeedAceleration / maxSpeed;
            
            if (newAcelerationPercent > collideAcelerationPercentRequirement && runAccelerationPercent < collideAcelerationPercentRequirement)
            {
                onCharging.Invoke();
            }
            if (newAcelerationPercent >= 1 && runAccelerationPercent < 1)
            {
                onFullyCharging.Invoke();
            }

            runAccelerationPercent = newAcelerationPercent;
            pushTime += Time.fixedDeltaTime;

            var timescale = Time.fixedDeltaTime * speedAcelerationMultiplyer * Source.LocalTimeScale;
            if (SpeedAceleration + timescale < maxSpeed)
            {
                SpeedAceleration += timescale;
            }
            else
            {
                SpeedAceleration = maxSpeed;
            }


            if (isServer)
            {
                var angle = Vector2.SignedAngle(Source.lookVector, Vector2.up);
                var newAngle = Vector2.SignedAngle(network_internal_lookVector, Vector2.up);

                var interpolatedAngle = Quaternion.RotateTowards(Quaternion.Euler(0, 0, angle), Quaternion.Euler(0, 0, newAngle), lookVectorAceleration * Time.fixedDeltaTime * Source.LocalTimeScale);
                interpolatedAngle = Quaternion.Inverse(interpolatedAngle); 

                Source.movementVector = Source.lookVector =  interpolatedAngle * Vector2.up;
            }

            yield return waitForEndOfFrame;
            
            collision = runAccelerationPercent > collideAcelerationPercentRequirement && (CheckCollision() || CheckStuck());
        
            if (collision && isServer)
            {
                break;
            }
        }

        if (collision)
        {
            Source.Push(Source.transform.rotation * collideBackPush);

            Explode();
        }
        else
        {
            while (SpeedAceleration > 0)
            {
                Source.movementVector = Source.lookVector;
                
                var timescale = Time.fixedDeltaTime * speedAcelerationMultiplyer * 5;

                if (SpeedAceleration - timescale > 0)
                {
                    SpeedAceleration -= timescale;
                }
                else
                {
                    SpeedAceleration = 0;
                }
                
                yield return waitForEndOfFrame;
            }
        }
    }

    private bool CheckCollision()
    {
        var collides = Physics.OverlapBox(transform.position + transform.rotation * colliderOffset, colliderScale / 2, transform.rotation);
        var isDamageDelivered = false;
        
        var damage = colliderDamage;
        damage.sender = Source;
        damage.pushDirection = Source.transform.rotation * damage.pushDirection;

        foreach (var collide in collides)
        {
            var report = Damage.Deliver(collide.gameObject, damage);
            Source.DamageDelivered(report);

            isDamageDelivered = isDamageDelivered || report.isDelivered;
        }

        return isDamageDelivered;
    }
    private bool CheckStuck()
    {
        return Source.characterController.velocity.magnitude <= stuckThresold;
    }
    
    private void Explode()
    {
        onExplode.Invoke();

        foreach (var collide in Physics.OverlapSphere(transform.position, explodeRange))
        {
            var damage = explodeDamage;
            damage.sender = Source;
            damage.pushDirection = Vector3.up * 0.4f +  (transform.position - collide.transform.position).normalized * 0.3f + transform.rotation * damage.pushDirection;

            var report = Damage.Deliver(collide.gameObject, damage);
            
            if (report.isDelivered)
            {
                Source.DamageDelivered(report);
            }
        }
    }

    private void FixedUpdate()
    {
        if (isOwned && IsPressed)
        {
            SetLookVector(Source.internalLookVector);
        }  
    }
    private void Start()
    {
        Source.isGroundedEvent += (state) => {
            if (!state && IsPressed )
            {
                Play();
            }
        };
        Source.onDamageRecieved += (damage) => {
            if (IsInProcess && damage.type is not Damage.Type.Effect and not Damage.Type.Magical and not Damage.Type.Balistic)
            {
                if (SpeedAceleration / maxSpeed >= collideAcelerationPercentRequirement)
                {
                    Source.stunlock = Mathf.Max(Source.stunlock, 1);
                }

                Stop();
            }
        };
    }
   
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(colliderOffset, colliderScale);
        Gizmos.DrawWireSphere(Vector3.zero, explodeRange);
    }

    [Command]
    private void SetLookVector(Vector2 value)
    {
        network_internal_lookVector = value;
    }
}