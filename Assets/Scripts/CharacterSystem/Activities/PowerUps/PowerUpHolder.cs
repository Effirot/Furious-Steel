using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PowerUpHolder : SyncedActivities
{
    [SerializeField]
    public NetworkCharacter Character;
    
    public PowerUp powerUp => Id < 0 || Id >= PowerUp.AllPowerUps.Length ? null : PowerUp.AllPowerUps[Id];

    public int Id => network_powerUpId.Value;

    public event Action<PowerUp> OnPowerUpChanged = delegate { };

    private NetworkVariable<int> network_powerUpId = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);


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
            Vector3 position = transform.position;
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, LayerMask.GetMask("Ground")))
            {
                position = hit.point + Vector3.up * 2;
            }

            var powerupGameObject = Instantiate(powerUp.prefab, position, Quaternion.identity);
        
            powerupGameObject.GetComponent<NetworkObject>().Spawn();
        }
    }

    protected override void OnStateChanged(bool IsPressed)
    {
        if (IsServer)
        {
            if (powerUp != null)
            {
                Activate_ClientRpc(Id);
                Activate_Internal(Id);
            }
            network_powerUpId.Value = -1;
        }
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