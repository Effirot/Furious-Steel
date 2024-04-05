using System;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
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
    [field : SerializeField, Range (0.1f, 100)]
    private float speed = 1;

    [field : SerializeField, Range (0.1f, 10)]
    private float lifetime = 3;

    [field : SerializeField]
    public VisualEffectAsset OnDestroyEffect { get; private set; }
    
    [field : SerializeField]
    public VisualEffect OnHitEffect { get; private set; }

    [SerializeField]
    public UnityEvent onDespawnEvent = new UnityEvent();

    [SerializeField]
    private Damage damage;
    
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

    public IDamageSource Summoner { get; private set; }

    public event Action<Damage> onDamageRecieved;

    public int TeamIndex => Summoner.TeamIndex;


    private NetworkVariable<Vector3> network_position = new NetworkVariable<Vector3> (Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> network_moveDirection = new NetworkVariable<Vector3> (Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);



    public void Initialize (Vector3 direction, IDamageSource summoner)
    {
        Push (direction);

        Summoner = summoner;
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

    public void Kill ()
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

        await UniTask.WaitForSeconds(5);

        Kill ();
    }
    public override void OnNetworkDespawn ()
    {
        base.OnNetworkDespawn();

        onDespawnEvent.Invoke();
    }

    private void FixedUpdate ()
    {
        if (!IsSpawned) return;

        if (IsServer)
        {
            transform.position += MoveDirection * Time.fixedDeltaTime * speed;

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
    private void OnTriggerEnter (Collider other)
    {
        if (!IsSpawned) return;

        var damage = this.damage;
        damage.pushDirection = transform.rotation * damage.pushDirection;
        damage.sender = Summoner;

        var report = Damage.Deliver(other.gameObject, damage);

        if (IsServer)
        {
            if (report.isDelivered)
            {
                if (report.isBlocked)
                {
                    Push(other.transform.forward);

                    speed *= 2f;

                    if (other.TryGetComponent<IDamageSource>(out var source))
                    {
                        Summoner = source;
                    }
                }
                else
                {
                    Kill();
                }
            }
        }
    }

    private void CheckGroundCollision()
    {
        if (Physics.Raycast(transform.position, MoveDirection, MoveDirection.magnitude, LayerMask.GetMask("Ground")))
        {
            Kill();
        }
    }
}