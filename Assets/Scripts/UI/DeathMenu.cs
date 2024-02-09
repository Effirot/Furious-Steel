using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using UnityEngine;

public class DeathMenu : MonoBehaviour
{
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
        foreach (GameObject child in transform)
        {
            child.SetActive(true);
        }
    }
}
