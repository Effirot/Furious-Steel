

using System;
using CharacterSystem.DamageMath;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Prop : NetworkBehaviour,
    IDamagable,
    IPhysicObject,
    ITimeScalable
{

    public bool Undestroyable = false;

    [field : SerializeField]
    public float maxHealth { get; set; }
    
    [field : SerializeField, Range(0.01f, 10)]
    public float mass { get; set; } = 1;

    public float health { 
        get => network_health.Value; 
        set { 
            if (IsServer && !Undestroyable)
            {
                network_health.Value = value; 
            }
        }
    }

    public float stunlock { get => 0; set { } }
    public Damage lastRecievedDamage { get; set; }

    public Team team => null;

    public Vector3 velocity { 
        get => network_velocity.Value; 
        set 
        {
            if (IsServer)
            {
                network_velocity.Value = value;
            }
        } 
    }
    public Vector3 position { 
        get => network_position.Value; 
        set 
        {
            if (IsServer)
            {
                network_position.Value = value;
            }
        } 
    }
    
    public float PhysicTimeScale { get; set; } = 1;
    public float GravityScale { get; set; } = 1;


    public event Action<Damage> onDamageRecieved;

    private NetworkVariable<Vector3> network_velocity = new (Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> network_position = new (Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> network_health = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    protected Rigidbody rb { get; private set; }
    public float LocalTimeScale { get; set; } = 1;

    public bool IsGrounded { get; private set; } = false;

    public bool Heal(Damage damage)
    {
        return false;
    }
    public bool Hit(Damage damage)
    {
        health -= damage.value;

        onDamageRecieved?.Invoke(damage);

        Push(damage.pushDirection);

        return false;
    }

    public void Kill()
    {
        Destroy(gameObject);
    }

    public void Push(Vector3 direction)
    {
        velocity = direction;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        network_health.Value = maxHealth;
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        rb.useGravity = false;
    }

    protected virtual void FixedUpdate()
    {   
        if (IsSpawned)
        {
            CalculateVelocity();

            Move();
        }
    }

    protected void OnCollisionEnter(Collision collision)
    {
        
    }

    protected void OnCollisionStay(Collision collision)
    {
        IsGrounded = collision.contactCount != 0;
    }

    protected void OnCollisionExit(Collision collision)
    {
        IsGrounded = false;
    }

    private void CalculateVelocity()
    {
        var velocity = this.velocity;


        if (IsGrounded)
        {
            velocity.y = 0.1f;
        
            var interpolateValue = 8 * mass * Time.fixedDeltaTime * LocalTimeScale * PhysicTimeScale;
            
            velocity.x =  Mathf.Lerp(velocity.x, 0, interpolateValue);
            velocity.z =  Mathf.Lerp(velocity.z, 0, interpolateValue);
        }
        else
        {
            velocity.y = Mathf.Lerp(velocity.y, (IsGrounded ? -0.1f : Physics.gravity.y * GravityScale), 0.6f * Time.fixedDeltaTime * LocalTimeScale * PhysicTimeScale); 
            
            var timeScale = 10f * Time.fixedDeltaTime * LocalTimeScale * PhysicTimeScale;
        }

        this.velocity = velocity;
    }
    private void Move()
    {
        if (IsServer)
        {
            transform.position = (transform.position + (velocity * LocalTimeScale * PhysicTimeScale * Time.fixedDeltaTime));
            
            network_position.Value = transform.position;
        }
        else
        {
            if (Vector3.Distance(network_position.Value, transform.position) < 0.5f)
            {
                var direction = network_position.Value - transform.position;

                transform.position = transform.position + direction + (velocity * LocalTimeScale * PhysicTimeScale);
            }
            else
            {
                transform.position = network_position.Value;
            }
        }
    }
}