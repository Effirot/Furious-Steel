using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

public class DamagableProp :
    NetworkBehaviour,
    IDamagable
{
    [SerializeField]
    private bool Undestroyable = false;

    [SerializeField]
    private NetworkVariable<float> network_Health = new (100, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

    [field : SerializeField]
    public VisualEffect OnHitEffect { get; private set; }

    [SerializeField]
    private UnityEvent<float> OnGetDamage = new ();

    [SerializeField]
    private UnityEvent<float> OnLossHealth = new ();

    public float Health { 
        get => network_Health.Value; 
        set { 
            if (IsServer)
            {
                network_Health.Value = value; 
            }
        }
    }
    public float Stunlock { get => 0; set { return; } }


    public void SendDamage(Damage damage)
    {
        if (Undestroyable) return;

        OnGetDamage.Invoke(damage.Value);

        Health -= damage.Value;

        if (Health <= 0)
        {
            Destroy();
        }
    }

    private void Destroy()
    {
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}
