

using System;
using CharacterSystem.DamageMath;
using Mirror;
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

    public float stunlock { get => 0; set { } }
    public Damage lastRecievedDamage { get; set; }



    [HideInInspector, SyncVar]
    public float health;
    [HideInInspector, SyncVar]
    public Vector3 velocity;
    [HideInInspector, SyncVar]
    public Vector3 position;
    
    public float PhysicTimeScale { get; set; } = 1;
    public float GravityScale { get; set; } = 1;

    public Team team => null;

    public event Action<Damage> onDamageRecieved;

    protected Rigidbody rb { get; private set; }
    public float LocalTimeScale { get; set; } = 1;

    public bool IsGrounded { get; private set; } = false;
    
    float IDamagable.health { get => health; set => health = value; }
    Vector3 IPhysicObject.velocity { get => velocity; set => velocity = value; }

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

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        rb.useGravity = false;
        
        health = maxHealth;
    }

    protected virtual void FixedUpdate()
    {   
        if (NetworkManager.singleton?.isNetworkActive ?? false)
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
        if (isServer)
        {
            transform.position = (transform.position + (velocity * LocalTimeScale * PhysicTimeScale * Time.fixedDeltaTime));
            
            position = transform.position;
        }
        else
        {
            if (Vector3.Distance(position, transform.position) < 0.5f)
            {
                var direction = position - transform.position;

                transform.position = transform.position + direction + (velocity * LocalTimeScale * PhysicTimeScale);
            }
            else
            {
                transform.position = position;
            }
        }
    }
}