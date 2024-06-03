

using System;
using CharacterSystem.DamageMath;
using Unity.Netcode;
using UnityEngine;

public class Prop : NetworkBehaviour,
    IDamagable
{

    public bool Undestroyable = false;

    [field : SerializeField]
    public float maxHealth { get; set; }

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

    public event Action<Damage> onDamageRecieved;

    private NetworkVariable<float> network_health = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


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
        if (gameObject.TryGetComponent<Rigidbody>(out var rigidbody))
        {
            direction += Vector3.up / 10;
            
            rigidbody.AddForce(direction * 1000);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        network_health.Value = maxHealth;
    }
}