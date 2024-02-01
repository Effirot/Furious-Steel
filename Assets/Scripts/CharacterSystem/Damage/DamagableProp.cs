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


    public virtual void SendDamage(Damage damage)
    {
        if (!Undestroyable) 
        {
            Health -= damage.Value;

            if (Health <= 0 && IsServer)
            {
                NetworkObject.Despawn(false);
            }
        }
        
        var VecrtorToTarget = transform.position - damage.Sender.transform.position;
        VecrtorToTarget.Normalize();

        if (TryGetComponent<Rigidbody>(out var rigidbody))
        {
            rigidbody.AddForce(VecrtorToTarget * damage.PushForce);
        }
        
        if (OnHitEffect != null)
        {
            if (OnHitEffect.HasVector3("Direction"))
            {
                OnHitEffect.SetVector3("Direction", VecrtorToTarget * damage.Value);
            }

            OnHitEffect.Play();
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
