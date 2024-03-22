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

    }

    [RequireComponent(typeof(NetworkCharacter))]
    public class PowerUpHolder : SyncedActivities<IPowerUpActivator>
    {   
        [HideInInspector]
        public NetworkCharacter Character;
        
        public PowerUp powerUp => Id < 0 || Id >= PowerUp.AllPowerUps.Length ? null : PowerUp.AllPowerUps[Id];

        public int Id => network_powerUpId.Value;

        public event Action<PowerUp> OnPowerUpChanged = delegate { };

        private NetworkVariable<int> network_powerUpId = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public void Drop(bool DestroyWithoutGround = true)
        {
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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            network_powerUpId.OnValueChanged += (Old, New) => OnPowerUpChanged.Invoke(powerUp);
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsServer && powerUp != null)
            {
                Drop();
            }
        }

        protected override void OnStateChanged(bool IsPressed)
        {
            if (IsServer && powerUp != null && Invoker.permissions.HasFlag(CharacterPermission.AllowPowerUps))
            {
                Activate_ClientRpc(Id);
                
                if (!IsClient)
                {
                    Activate_Internal(Id);
                }
                
                network_powerUpId.Value = -1;
            }
        }

        private void Awake()
        {
            Character = GetComponent<NetworkCharacter>();
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

        private void OnTriggerStay(Collider other)
        {
            if (IsServer && powerUp == null)
            {
                if (other.TryGetComponent<PowerUpContainer>(out var container))
                {
                    network_powerUpId.Value = container.Id;
                    
                    container.NetworkObject.Despawn();

                    powerUp?.OnPick(this);
                }
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