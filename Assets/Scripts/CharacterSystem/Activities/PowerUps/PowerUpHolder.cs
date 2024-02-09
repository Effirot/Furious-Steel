using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PowerUpHolder : SyncedActivities
{
    [SerializeField]
    public NetworkCharacter Character;
    
    public PowerUp powerUp => Id < 0 || Id >= PowerUp.AllPowerUps.Length ? null : PowerUp.AllPowerUps[Id];

    public int Id => network_powerUpId.Value;

    private NetworkVariable<int> network_powerUpId = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

    protected override void OnStateChanged(bool IsPressed)
    {
        if (powerUp != null)
        {
            powerUp.Activate(this);

        }
        if (IsServer)
        {
            network_powerUpId.Value = -1;
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