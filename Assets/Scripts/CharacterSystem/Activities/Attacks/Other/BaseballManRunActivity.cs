

using System.Collections;
using System.Security.Cryptography;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class BaseballManRunActivity : SyncedActivity<PlayerNetworkCharacter>
{
    [SerializeField, Range(0, 35)]
    private float maxSpeed = 35;

    [SerializeField, Range(0.5f, 3)]
    private float speedAcelerationMultiplyer = 1.5f;
    
    [SerializeField, Range(50f, 0.01f)]
    private float lookVectorAceleration = 0.03f;

    [SerializeField, Range(1, 8)]
    private float stuckThresold = 4;

    [SerializeField, Range(0, 1)]
    private float collideAcelerationPercentRequirement = 0.4f;

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
            Source.CurrentSpeed += value - speedAceleration;

            speedAceleration = value;
        }
    }
    private float speedAceleration = 0;
    
    private NetworkVariable<Vector2> network_internal_lookVector = new(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Source.isGroundedEvent += (state) => {
            if (!state && IsPressed )
            {
                Play();
            }
        };
        Source.onDamageRecieved += (damage) => {
            if (IsInProcess && damage.type != Damage.Type.Effect)
            {
                Source.stunlock += 1;

                Stop();
            }
        };
    }

    public override void Play()
    {
        if (Source.JumpButtonState && !IsInProcess && !Source.isStunned && Source.permissions.HasFlag(CharacterPermission.AllowAttacking))
        {
            SpeedAceleration = 0;

            base.Play();
        }
    }
    public override void Stop()
    {
        onStop.Invoke();

        if (IsInProcess)
        {            
            SpeedAceleration = 0;
            
            Permissions = CharacterPermission.Default;
        }

        base.Stop();
    }

    public override IEnumerator Process()
    {
        Permissions = CharacterPermission.AllowGravity;

        yield return new WaitUntil(() => Source.IsGrounded);

        onStartRunning.Invoke();
        
        SpeedAceleration = 0f;

        var waitForEndOfFrame = new WaitForEndOfFrame();
        
        var collision = false;
        var runAccelerationPercent = 0f;       

        while (IsPressed)
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



            var timescale = Time.fixedDeltaTime * speedAcelerationMultiplyer;

            if (SpeedAceleration + timescale < maxSpeed)
            {
                SpeedAceleration += timescale;
            }
            else
            {
                SpeedAceleration = maxSpeed;
            }

            if (IsServer)
            {
                var angle = Vector2.SignedAngle(Source.lookVector, Vector2.up);
                var newAngle = Vector2.SignedAngle(network_internal_lookVector.Value, Vector2.up);

                var interpolatedAngle = Quaternion.RotateTowards(Quaternion.Euler(0, 0, angle), Quaternion.Euler(0, 0, newAngle), lookVectorAceleration * Time.fixedDeltaTime);
                interpolatedAngle = Quaternion.Inverse(interpolatedAngle); 

                Source.movementVector = Source.lookVector =  interpolatedAngle * Vector2.up;
            }

            yield return waitForEndOfFrame;
            
            collision = runAccelerationPercent > collideAcelerationPercentRequirement && (CheckCollision() || CheckStuck());
        
            if (collision && IsServer)
            {
                break;
            }
        }

        if (collision)
        {
            Source.Push(Source.transform.rotation * collideBackPush);

            Explode();

            if (IsServer)
            {
                Explode_ClientRpc();
            }
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
        if (IsOwner && IsPressed)
        {
            network_internal_lookVector.Value = Source.internalLookVector;
        }  
    }

    [ClientRpc]
    private void Explode_ClientRpc()
    {
        onExplode.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(colliderOffset, colliderScale);
        Gizmos.DrawWireSphere(Vector3.zero, explodeRange);
    }
}