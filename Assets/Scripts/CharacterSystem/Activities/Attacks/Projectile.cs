using System;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

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
    

    public Vector3 MoveDirection 
    {
        get => network_moveDirection.Value;
        set 
        {
            if (IsServer)
            {
                network_moveDirection.Value = value;
            }
        }
    }

    public float health { get => 0; set { return; } }
    public float stunlock { get => 0; set { return; } }

    public IDamageSource Summoner {
        get {
            if (network_SummonerId.Value != ulong.MinValue)
            {
                var dictionary = NetworkManager.Singleton.SpawnManager.SpawnedObjects;

                if (dictionary.ContainsKey(network_SummonerId.Value) && dictionary[network_SummonerId.Value].TryGetComponent<IDamageSource>(out var component))
                {
                    return component;
                }
            }

            return null;
        }
        private set {
            network_SummonerId.Value = value?.gameObject?.GetComponent<NetworkObject>()?.NetworkObjectId ?? ulong.MinValue;
        } 
    }

    public event Action<Damage> onDamageRecieved;

    public int TeamIndex => Summoner.TeamIndex;


    private NetworkVariable<ulong> network_SummonerId = new NetworkVariable<ulong> (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> network_position = new NetworkVariable<Vector3> (Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> network_moveDirection = new NetworkVariable<Vector3> (Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    public void Initialize (Vector3 direction, IDamageSource summoner, OnDamageDeliveryReport onDamageDeliveryReport = null)
    {
        if (!IsSpawned)
        {
            NetworkObject.Spawn();
        }

        Push (direction);

        Summoner = summoner;

        this.onDamageDeliveryReport = onDamageDeliveryReport;
    }

    public bool Hit (Damage damage)
    {
        onDamageRecieved?.Invoke(damage);

        if (IsServer)
        {
            MoveDirection = damage.pushDirection;
        }

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

    public virtual void Kill ()
    {
        if (IsSpawned && IsServer)
        {
            NetworkObject.Despawn();
        }
    }

    public async override void OnNetworkSpawn ()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            network_position.Value = transform.position;
        }
        else
        {
            transform.position = network_position.Value;
        }

        await UniTask.WaitForSeconds(lifetime);

        if (!this.IsUnityNull())
        {
            Kill ();
        }
    }
    public override void OnNetworkDespawn ()
    {
        base.OnNetworkDespawn();

        onDespawnEvent.Invoke();
    }

    protected virtual void FixedUpdate ()
    {
        if (!IsSpawned) return;

        speed *= speedTimeIncreacing;

        if (IsServer)
        {
            transform.position += MoveDirection * Time.fixedDeltaTime * speed;

            MoveDirection = Vector3.Lerp(MoveDirection, Physics.gravity, Gravity);
            

            network_position.Value = transform.position;

            CheckGroundCollision();
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, network_position.Value, 0.2f);
        }

        if (network_moveDirection.Value.magnitude > 0)
        {
            transform.rotation = Quaternion.LookRotation(network_moveDirection.Value);
        }
    }
    protected virtual void OnTriggerEnter (Collider other)
    {
        if (!IsSpawned) return;
        if (!DamageOnHit) return;

        var damage = this.damage;
        damage.pushDirection = transform.rotation * damage.pushDirection;
        damage.sender = Summoner;

        var report = Damage.Deliver(other.gameObject, damage);

        if (IsServer)
        {
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
                    Kill();
                }
            }
        }

        onDamageDeliveryReport?.Invoke(report);
    }

    private void CheckGroundCollision()
    {
        if (Physics.Raycast(transform.position, MoveDirection, MoveDirection.magnitude, LayerMask.GetMask("Ground")))
        {
            Kill();
        }
    }
}