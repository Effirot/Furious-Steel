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
    ITeammate
{
    public delegate void OnDamageDeliveryReport(DamageDeliveryReport damageDeliveryReport);

    [field : SerializeField, Range (0.1f, 100)]
    public float speed = 1;

    [field : SerializeField, Range (0.95f, 1.05f)]
    private float speedTimeIncreacing = 1;

    [field : SerializeField, Range (0.1f, 10)]
    private float lifetime = 3;
  
    [field : SerializeField]
    public bool DamageOnHit { get; private set; }

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
    public Vector3 MoveDirection;
    [HideInInspector, SyncVar]
    public Vector3 position = Vector3.zero;

    public Damage lastRecievedDamage { get; set; }

    public float maxHealth { get => 0; }
    public float health { get => 0; set { return; } }
    public float stunlock { get => 0; set { return; } }
    public Team team { get => Summoner.team; }

    public IDamageSource Summoner {
        get {
            if (!summonerObject.IsUnityNull() && summonerObject.TryGetComponent<IDamageSource>(out var component))
            {
                return component;
            }
            
            return null;
        }
        private set {
            summonerObject = value?.gameObject ?? null;
        } 
    }


    [SyncVar]
    private GameObject summonerObject;

    public event Action<Damage> onDamageRecieved;
    


    public void Initialize (Vector3 direction, IDamageSource summoner, OnDamageDeliveryReport onDamageDeliveryReport = null)
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

    public bool Hit (Damage damage)
    {
        onDamageRecieved?.Invoke(damage);

        Kill(damage);

        return false;
    }
    public bool Heal (Damage damage)
    {        
        return false;
    }
    public void Push (Vector3 direction)
    {
        MoveDirection = direction;
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
            transform.position += MoveDirection * Time.fixedDeltaTime * speed;

            MoveDirection = Vector3.Lerp(MoveDirection, Physics.gravity, Gravity);
            

            position = transform.position;

            CheckGroundCollision();
        }

        if (MoveDirection.magnitude > 0)
        {
            transform.rotation = Quaternion.LookRotation(MoveDirection);
        }
    }
    protected virtual void LateUpdate()
    {
        if (isClient)
        {
            if (Vector3.Distance(transform.position, position) < 0.5f)
            {
                transform.position = Vector3.Lerp(transform.position, position, 22 * Time.deltaTime);
            }
            else
            {
                transform.position = position;
            }
        }
    }
    protected virtual void OnTriggerEnter (Collider other)
    {
        if (!DamageOnHit) return;

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

                if (other.TryGetComponent<IDamageSource>(out var source))
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
        if (Physics.Linecast(transform.position - MoveDirection, transform.position + MoveDirection, out var hit, LayerMask.GetMask("Ground", "Projectile"), QueryTriggerInteraction.Collide))
        {
            if (DamageOnHit)
            {
                Kill(default);
            }
            else
            {
                transform.position += hit.normal * hit.distance * Time.fixedDeltaTime * speed;
            }
        }
    }
}