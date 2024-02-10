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
            if (_observingCharacter != null)
            {
                virtualCamera.Follow = transform;
                _observingCharacter.OnHitEvent.RemoveListener(HealthChanged);

                for (int i = 0; i < drawers.Length; i++)
                {
                    if (i < holders.Length)
                    {
                        drawers[i].gameObject.SetActive(false);
                        holders[i].OnPowerUpChanged -= drawers[i].Draw;
                    } 
                }
            }

            _observingCharacter = value;

            if (value != null)
            {
                // Camera drawer
                virtualCamera.Follow = value.transform;
                
                HealthSlider.gameObject.SetActive(value.IsOwner);
                
                if (value.IsOwner) {
                    // Health drawer
                    HealthSlider.maxValue = value.maxHealth;
                    HealthSlider.value = value.health;
                    value.OnHitEvent.AddListener(HealthChanged);
                    
                    // PowerUp drawer
                    holders = value.GetComponentsInChildren<PowerUpHolder>();
                    for (int i = 0; i < drawers.Length; i++)
                    {
                        if (i < holders.Length)
                        {
                            drawers[i].gameObject.SetActive(true);
                            drawers[i].Draw(holders[i].powerUp);  
                            holders[i].OnPowerUpChanged += drawers[i].Draw;
                        }   
                        else
                        {
                            drawers[i].gameObject.SetActive(false);
                        }
                    }
                }
                
                controllers?.SetActive(value.IsOwner && value is PlayerNetworkCharacter);
            }
            else
            {
                HealthSlider.gameObject.SetActive(false);
                
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
    private Slider HealthSlider;

    [SerializeField]
    private CinemachineVirtualCamera virtualCamera; 

    [SerializeField]
    private PowerUpDrawer[] drawers;

    private PowerUpHolder[] holders;

    private void Awake()
    {
        Singleton = this;
    
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
