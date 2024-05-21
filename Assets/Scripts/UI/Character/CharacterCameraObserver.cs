using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCameraObserver : MonoBehaviour
{
    public static CharacterCameraObserver Singleton { get; private set; }

    public Transform observingTransform
    {
        get => _observingCharacter;
        set
        {
            _observingCharacter = value;

            if (value != null)
            {
                // Camera drawer
                virtualCamera.Follow = value.transform;
            }

            StopAllCoroutines();

            {
                if (value == null || !(value.gameObject.TryGetComponent<PlayerNetworkCharacter>(out var character) && character.IsOwner))
                {
                    StartCoroutine(ObserveRandomCharacter());
                }
            }
        }
    }

    [SerializeField]
    private Transform _observingCharacter = null;

    [SerializeField]
    private CinemachineVirtualCamera virtualCamera; 

    private void Awake()
    {
        Singleton = this;
        observingTransform = null;
    
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
        if (observingTransform == null)
        {
            observingTransform = character.transform;
        }
    }
    private void ObserveCharacter_Event(PlayerNetworkCharacter character)
    {
        observingTransform = character.transform;
    }
    private void ResetObserver_Event(PlayerNetworkCharacter character)
    {
        observingTransform = null;
    }

    private IEnumerator ObserveRandomCharacter()
    {
        yield return new WaitForSecondsRealtime(5);

        if (PlayerNetworkCharacter.Players.Count == 0)
        {
            observingTransform = null;
        }
        else
        {
            observingTransform = PlayerNetworkCharacter.Players[Random.Range(0, PlayerNetworkCharacter.Players.Count - 1)].transform;
        }
    }
}
