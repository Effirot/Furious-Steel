using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using UnityEngine;

public class DeathMenu : MonoBehaviour
{
    public void Respawn()
    {
        if (RoomManager.Singleton != null)
        {
            RoomManager.Singleton.SpawnWithRandomArgs();
        }
    }

    private void Awake()
    {
        PlayerNetworkCharacter.OnOwnerPlayerCharacterDead += OnOwnerPlayerCharacterDead_Event;
    }

    private void OnDestroy()
    {
        PlayerNetworkCharacter.OnOwnerPlayerCharacterDead -= OnOwnerPlayerCharacterDead_Event;
    }

    private void OnOwnerPlayerCharacterDead_Event(PlayerNetworkCharacter character)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
    }
}
