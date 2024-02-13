using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class CharacterUIObserver : MonoBehaviour
{
    public static CharacterUIObserver Singleton { get; private set; }

    public NetworkCharacter observingCharacter
    {
        get => _observingCharacter;
        set
        {
            _observingCharacter = value;

            if (value != null)
            {
                // Camera drawer
                virtualCamera.Follow = value.transform;
                
                controllers?.SetActive(value.IsOwner && value is PlayerNetworkCharacter);
            }
            else
            {                
                controllers?.SetActive(false);
            }

            StopAllCoroutines();

            if (value == null || !value.IsOwner)
            {
                StartCoroutine(ObserveRandomCharacter());
            }
        }
    }

    [SerializeField]
    private NetworkCharacter _observingCharacter = null;

    [SerializeField]
    private GameObject controllers;

    [SerializeField]
    private CinemachineVirtualCamera virtualCamera; 

    private void Awake()
    {
        Singleton = this;
        observingCharacter = null;
    
#if !UNITY_ANDROID 
        Destroy(controllers);

        controllers = null; 
#endif


        PlayerNetworkCharacter.OnPlayerCharacterSpawn += ObserveRandomCharacterCharacter_Event;
        PlayerNetworkCharacter.OnOwnerPlayerCharacterDead += ResetObserver_Event;
        PlayerNetworkCharacter.OnOwnerPlayerCharacterSpawn += ObserveCharacter_Event;
    }

    private void OnDestroy()
    {
        Singleton = null;

        PlayerNetworkCharacter.OnPlayerCharacterSpawn -= ObserveRandomCharacterCharacter_Event;
        PlayerNetworkCharacter.OnOwnerPlayerCharacterDead -= ResetObserver_Event;
        PlayerNetworkCharacter.OnOwnerPlayerCharacterSpawn -= ObserveCharacter_Event;
    }

    private void ObserveRandomCharacterCharacter_Event(PlayerNetworkCharacter character)
    {
        if (observingCharacter == null)
        {
            observingCharacter = character;
        }
    }
    private void ObserveCharacter_Event(PlayerNetworkCharacter character)
    {
        observingCharacter = character;
    }
    private void ResetObserver_Event(PlayerNetworkCharacter character)
    {
        observingCharacter = null;
    }

    private IEnumerator ObserveRandomCharacter()
    {
        yield return new WaitForSecondsRealtime(5);

        if (PlayerNetworkCharacter.Players.Count == 0)
        {
            observingCharacter = null;
        }
        else
        {
            observingCharacter = PlayerNetworkCharacter.Players[Random.Range(0, PlayerNetworkCharacter.Players.Count - 1)];
        }
    }
}
