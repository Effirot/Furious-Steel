using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class PowerUpHolder : SyncedActivities
{
    [SerializeField]
    private NetworkCharacter Character;
    
    public PowerUp powerUp => Id < 0 || Id >= PowerUp.AllPowerUps.Length ? null : PowerUp.AllPowerUps[Id];

    public int Id => network_powerUpId.Value;

    private NetworkVariable<int> network_powerUpId = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

    protected override void OnStateChanged(bool IsPressed)
    {
        if (IsServer && IsPressed)
        {
            ActivatePowerUp_ClientRpc(Id);

            network_powerUpId.Value = -1;
        }
    }

    [ClientRpc]
    private void ActivatePowerUp_ClientRpc(int ID)
    {
        if (powerUp != null)
        {
            PowerUp.AllPowerUps[ID].Activate(Character);
        }
    } 

    private void OnTriggerStay(Collider other)
    {
        if (IsServer)
        {
            if (other.TryGetComponent<PowerUpContainer>(out var container))
            {
                network_powerUpId.Value = container.Id;
                
                container.NetworkObject.Despawn();
            }
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(PowerUpHolder), true)]
    public class PowerUpHolder_Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            
        }
    }

#endif
}