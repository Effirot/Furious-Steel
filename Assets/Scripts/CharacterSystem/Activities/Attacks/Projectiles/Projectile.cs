using System;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using Unity.Cinemachine;
using Cysharp.Threading.Tasks;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;
using Telepathy;

public class Projectile : NetworkBehaviour, 
    IDamagable,
    IPhysicObject,
    ITeammate
{
    public delegate void OnDamageDeliveryReport(DamageDeliveryReport damageDeliveryReport);

    [SerializeField, Range (0.1f, 100)]
    public float speed = 1;

    [SerializeField, Range (0.95f, 1.05f)]
    private float speedTimeIncreacing = 1;

    [SerializeField, Range (0.1f, 10)]
    private float lifetime = 3;
 
    [SerializeField, Range (0f, 1f)]
    private float bounciness = 0.1f;
 
    [SerializeField, Range (0f, 1f)]
    private float friction = 0f;
  
    [SerializeField]
    public bool DamageOnHit;

    [SerializeField]
    public bool DestroyOnGroundHit = true;

    [field : SerializeField]
    public bool AllowDeflecting { get; private set; }

    [field : SerializeField, Range(0, 0.1f)]
    public float Gravity { get; private set; } = 0;

    [SerializeField]
    public UnityEvent onDespawnEvent = new UnityEvent();

    [SerializeField]
    public Damage damage;

    public OnDamageDeliveryReport onDamageDeliveryReport;
    
    [HideInInspector, SyncVar]
    public Vector3 velocity;
    [HideInInspector, SyncVar]
    public Vector3 position = Vector3.zero;

    public Damage lastRecievedDamage { get; set; }

    public float maxHealth { get => 0; }
    public float health { get => 0; set { return; } }
    public float stunlock { get => 0; set { return; } }
    public Team team { get => Summoner.team; }

    public IAttackSource Summoner {
        get {
            if (!summonerObject.IsUnityNull() && summonerObject.TryGetComponent<IAttackSource>(out var component))
            {
                return component;
            }
            
            return null;
        }
        private set {
            summonerObject = value?.gameObject ?? null;
        } 
    }

    Vector3 IPhysicObject.velocity { get => velocity; set => velocity = value; }
    float IPhysicObject.mass { get => 0; set { return; } }
    float IPhysicObject.PhysicTimeScale { get => 0; set { return; } }
    float IPhysicObject.GravityScale { get => 0; set { return; } }


    [SyncVar]
    private GameObject summonerObject;

    public event Action<Damage> onDamageRecieved;
    
    public void Initialize (Vector3 direction, IAttackSource summoner, OnDamageDeliveryReport onDamageDeliveryReport = null)
    {
        if (!NetworkServer.spawned.ContainsKey(netIdentity.netId))
        {
            NetworkServer.Spawn(gameObject);
            
            position = transform.position;
        }

        Push (direction);

        Summoner = summoner;


        this.onDamageDeliveryReport = onDamageDeliveryReport;
    }

    public bool Hit (ref Damage damage)
    {
        onDamageRecieved?.Invoke(damage);

        Kill(damage);

        Debug.Log(damage);

        return false;
    }
    public bool Heal (ref Damage damage)
    {        
        return false;
    }
    public void Push (Vector3 direction)
    {
        velocity = direction;
    }

    public virtual void Kill (Damage damage)
    {
        NetworkServer.Destroy(gameObject);
    }

    protected virtual async void Start()
    {
        if (isServer)
        {
            position = transform.position;
        }
        else
        {
            transform.position = position;
        }

        await UniTask.WaitForSeconds(lifetime);

        if (!this.IsUnityNull())
        {
            Kill (default);
        }
    }
    protected virtual void OnDestroy()
    {
        onDespawnEvent.Invoke();

        if (isClient)
        {
            transform.position = position;
        }
    }

    protected virtual void FixedUpdate ()
    {
        speed *= speedTimeIncreacing;

        if (isServer)
        {
            transform.position += velocity * Time.fixedDeltaTime * speed;

            velocity = Vector3.Lerp(velocity, Physics.gravity, Gravity);
            

            position = transform.position;

            CheckGroundCollision();
        }

        if (velocity.magnitude > 0)
        {
            transform.rotation = Quaternion.LookRotation(velocity);
        }
    }
    protected virtual void LateUpdate()
    {
        if (isClient)
        {
            if (Vector3.Distance(transform.position, position) < 0.4f)
            {
                transform.position = Vector3.Lerp(transform.position, position, 25f * Time.deltaTime);
            }
            else
            {
                transform.position = position;
            }
        }
    }
    protected virtual void OnTriggerEnter (Collider other)
    {
        if (!DamageOnHit || !isServer) return;

        var damage = this.damage;
        damage.pushDirection = transform.rotation * damage.pushDirection;
        damage.sender = Summoner;

        var report = Damage.Deliver(other.gameObject, damage);

        if (report.isDelivered)
        {
            if (report.isBlocked && AllowDeflecting)
            {
                Push(other.transform.forward);

                speed *= 1.5f;
                lifetime += 5;

                if (other.TryGetComponent<IAttackSource>(out var source))
                {
                    Summoner = source;
                }

                return;
            }
            else
            {
                Kill(default);
            }
        }

        onDamageDeliveryReport?.Invoke(report);
    }

    private void CheckGroundCollision()
    {
        if (Physics.Linecast(transform.position - velocity, transform.position + velocity, out var hit, LayerMask.GetMask("Ground"), QueryTriggerInteraction.Collide))
        {
            if (DestroyOnGroundHit)
            {
                position = transform.position = hit.point;
                Kill(default);
            }
            else
            {
                var deltaAngle = Quaternion.FromToRotation(hit.normal, Vector3.down);
                var deltaVelocity = deltaAngle * velocity;

                deltaVelocity.y = Mathf.Min(0, -deltaVelocity.y) * bounciness;
                deltaVelocity.x = Mathf.MoveTowards(deltaVelocity.x, 0, Time.fixedTime * (10 * friction));
                deltaVelocity.z = Mathf.MoveTowards(deltaVelocity.z, 0, Time.fixedTime * (10 * friction));

                velocity = Quaternion.Inverse(deltaAngle) * deltaVelocity;
            }
        }
    }
}