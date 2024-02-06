using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

namespace CharacterSystem.Objects
{
    [DisallowMultipleComponent]
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
        private UnityEvent<Damage> OnHitEvent = new ();

        [SerializeField]
        private UnityEvent<float> OnLossHealth = new ();

        public float health { 
            get => network_Health.Value; 
            set { 
                if (IsServer)
                {
                    network_Health.Value = value; 
                }
            }
        }
        public float stunlock { get => 0; set { return; } }

        public virtual void Hit(Damage damage)
        {

            if (!Undestroyable) 
            {
                health -= damage.Value;

                if (health <= 0 && IsServer)
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
            
            OnHitEvent.Invoke(damage);
        }
        public virtual void Heal(float value)
        { 

        }
        public void Kill()
        {
            if (IsServer)
            {
                NetworkObject.Despawn();
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
}

