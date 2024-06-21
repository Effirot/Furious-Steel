using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Mirror;
using UnityEngine;

public class DeathMenu : MonoBehaviour
{
#if UNITY_EDITOR
    public async void Respawn()
#else
    public void Respawn()
#endif
    {
        if (RoomManager.Singleton != null)
        {
#if UNITY_EDITOR
            if (!NetworkManager.singleton.isNetworkActive)
            {
                NetworkManager.singleton.StartHost();

                await Cysharp.Threading.Tasks.UniTask.WaitUntil(() => NetworkManager.singleton.isNetworkActive);
            }
#endif

            RoomManager.Singleton.Spawn(RoomManager.SpawnArguments.Local);
        }
    }

    private void Awake()
    {
        PlayerNetworkCharacter.OnOwnerPlayerCharacterSpawn += OnOwnerPlayerCharacterSpawn_Event;
        PlayerNetworkCharacter.OnOwnerPlayerCharacterDead += OnOwnerPlayerCharacterDead_Event;
    }

    private void OnDestroy()
    {
        PlayerNetworkCharacter.OnOwnerPlayerCharacterSpawn -= OnOwnerPlayerCharacterSpawn_Event;
        PlayerNetworkCharacter.OnOwnerPlayerCharacterDead -= OnOwnerPlayerCharacterDead_Event;
    }

    private void OnOwnerPlayerCharacterSpawn_Event(PlayerNetworkCharacter character)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }
    private void OnOwnerPlayerCharacterDead_Event(PlayerNetworkCharacter character)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
    }
}
