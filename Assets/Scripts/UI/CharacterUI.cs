using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class CharacterUI : MonoBehaviour
{
    public static CharacterUI Singleton { get; private set; }

    public NetworkCharacter observingCharacter
    {
        get => _observingCharacter;
        set
        {
            if (_observingCharacter != null)
            {
                virtualCamera.Follow = transform;
                _observingCharacter.OnHitEvent.RemoveListener(HealthChanged);
            }

            _observingCharacter = value;

            if (value != null)
            {
                virtualCamera.Follow = value.transform;

                HealthSlider.gameObject.SetActive(true);
                HealthSlider.maxValue = value.MaxHealth;
                HealthSlider.value = value.health;
                value.OnHitEvent.AddListener(HealthChanged);

                controllers?.SetActive(value.IsOwner && value is PlayerNetworkCharacter);
            }
            else 
            {
                HealthSlider.gameObject.SetActive(false);
                
                controllers?.SetActive(false);
            }


            StopAllCoroutines();

            if (!value.IsOwner)
            {
                StartCoroutine(ObserveRandomCharacter());
            }
            
        }
    }

    [SerializeField]
    private NetworkCharacter _observingCharacter;

    [SerializeField]
    private GameObject controllers;

    [SerializeField]
    private Slider HealthSlider;

    [SerializeField]
    private CinemachineVirtualCamera virtualCamera; 



    private void Awake()
    {
        Singleton = this;
    
#if !UNITY_ANDROID 
        Destroy(controllers);

        controllers = null; 
#endif

        PlayerNetworkCharacter.OnPlayerCharacterSpawn += ObserveRandomCharacterCharacter_Event;
        PlayerNetworkCharacter.OnOwnerPlayerCharacterSpawn += ObserveCharacter_Event;
    }

    private void OnDestroy()
    {
        Singleton = null;

        PlayerNetworkCharacter.OnPlayerCharacterSpawn -= ObserveRandomCharacterCharacter_Event;
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

    private void HealthChanged(Damage damage)
    {
        HealthSlider.value = observingCharacter.health;
    }

    private IEnumerator ObserveRandomCharacter()
    {
        yield return new WaitForSecondsRealtime(5);

        observingCharacter = PlayerNetworkCharacter.Players[Random.Range(0, PlayerNetworkCharacter.Players.Count - 1)];
    }
}
