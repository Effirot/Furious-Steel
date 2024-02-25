using CharacterSystem.DamageMath;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

public class Projectile : NetworkBehaviour, 
    IDamagable
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

    private NetworkVariable<Vector3> network_position = new NetworkVariable<Vector3> (Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> network_moveDirection = new NetworkVariable<Vector3> (Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public bool Hit (Damage damage)
    {
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
        base.OnDestroy();
    }

    public override void OnDestroy() => Kill();
    public override void OnNetworkSpawn()
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

        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate ()
    {
        if (IsServer)
        {
            transform.position += MoveDirection * Time.fixedDeltaTime * speed;

            network_position.Value = transform.position;
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
    private void OnTriggerEnter(UnityEngine.Collider other)
    {
        // Debug.Log(other.gameObject.name);

        var damage = this.damage;
        damage.pushDirection = transform.rotation * damage.pushDirection;

        Damage.Deliver(other.gameObject, damage);
        
        if (IsServer)
        {
            Kill_ClientRpc();
            
            if (IsClient)
            {
                Kill();
            }
        }
    }

    [ClientRpc]
    private void Kill_ClientRpc ()
    {
        Kill();     
    }
}