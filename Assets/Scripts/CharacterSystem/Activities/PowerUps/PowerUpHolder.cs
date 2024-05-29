using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using System;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using CharacterSystem.Blocking;
using Unity.VisualScripting;
using UnityEngine.Events;


#if UNITY_EDITOR
using UnityEditor;
#endif


namespace CharacterSystem.PowerUps
{
    public interface IPowerUpActivator : 
        ISyncedActivitiesSource,
        IDamageSource,
        IDamagable
    {
        UnityEvent<PowerUp> onPowerUpChanged { get; } 

        int PowerUpId { get; set; }
        
        public PowerUp PowerUp {
            get => PowerUp.IdToPowerUpLink(PowerUpId);
            set {
                if (value.IsUnityNull())
                {
                    PowerUpId = -1;
                }
                else
                {
                    PowerUpId = Array.IndexOf(PowerUp.AllPowerUps, value);
                }
            }
        }
    }

    public class PowerUpHolder : SyncedActivity<IPowerUpActivator>
    {
        public PowerUp powerUp {
            get => Invoker.PowerUp;
            set => Invoker.PowerUp = value;
        }

        public int Id {
            get => Invoker.PowerUpId;
            set => Invoker.PowerUpId = value;
        }

        public UnityEvent<PowerUp> OnPowerUpChanged = new();

        public void Drop(bool DestroyWithoutGround = true)
        {
            if (HasOverrides()) return;

            Vector3 position = transform.position;
            
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, LayerMask.GetMask("Ground")))
            {
                position = hit.point + Vector3.up * 2;
            }
            else
            {
                if (DestroyWithoutGround) return;
            }

            var powerupGameObject = Instantiate(powerUp.prefab, position, Quaternion.identity);
        
            powerupGameObject.GetComponent<NetworkObject>().Spawn();

            if (IsServer)
            {
                Destroy(powerupGameObject, 10);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            if (HasOverrides()) return;

            if (other.TryGetComponent<PowerUpContainer>(out var container) && IsServer)
            {
                if (!container.powerUp.IsOneshot)
                {
                    if (powerUp == null)
                    {
                        Invoker.PowerUpId = container.Id;
                        
                        container.powerUp.OnPick(this);
                        container.NetworkObject.Despawn();
                    }
                }
                else
                {
                    container.powerUp.Activate(this);
                    container.NetworkObject.Despawn();
                }
            }
        }

        public void Activate()
        {
            if (IsServer)
            {
                Activate_ClientRpc(Id);
                if (!IsClient)
                {
                    Activate_Internal(Id);
                }
                
                Invoker.PowerUpId = -1;
            }
        }
        
        public override IEnumerator Process()
        {
            if (IsPressed && powerUp != null && Invoker.permissions.HasFlag(CharacterPermission.AllowPowerUps))
            {
                Activate();
            }

            yield break;
        }

        [ClientRpc]
        private void Activate_ClientRpc(int Id)
        {
            Activate_Internal(Id);
        }
        private void Activate_Internal(int Id)
        {
            if (Id >= 0 && Id < PowerUp.AllPowerUps.Length) 
            { 
                var powerUp = PowerUp.AllPowerUps[Id];
                
                powerUp.Activate(this);
            }
        }

#if UNITY_EDITOR


        [CustomEditor(typeof(PowerUpHolder), true)]
        public class PowerUpHolder_Editor : Editor
        {
            new private PowerUpHolder target => base.target as PowerUpHolder;

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                GUILayout.Label(target.powerUp?.GetType().Name ?? "None");
            }
        }

#endif
    }
}