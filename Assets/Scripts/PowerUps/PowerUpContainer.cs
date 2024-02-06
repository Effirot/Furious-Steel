using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;

public class PowerUpContainer : NetworkBehaviour
{
    public PowerUp powerUp => Id == -1 ? null : PowerUp.AllPowerUps[Id];

    public int Id => network_powerUpId.Value;

    private NetworkVariable<int> network_powerUpId = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
}