using CharacterSystem.DamageMath;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class Projectile : NetworkBehaviour, 
    IDamagable
{
    [field : SerializeField]
    public VisualEffectAsset OnDestroyEffect { get; private set; }
    
    [field : SerializeField]
    public VisualEffect OnHitEffect { get; private set; }
    
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

    private void FixedUpdate ()
    {
        if (IsServer)
        {
            transform.position += MoveDirection * Time.fixedDeltaTime;

            network_position.Value = transform.position;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, network_position.Value, 0.2f);
        }
    }
    private void OnCollisionEnter (Collision collision)
    {
        if (IsServer)
        {
            Kill_ClientRpc();
            
            if (IsClient)
            {
                Kill();
            }
        }
    }

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
        
    }

    [ClientRpc]
    private void Kill_ClientRpc ()
    {
        Kill();
    }
}