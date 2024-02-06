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
                virtualCamera.Follow = null;
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
    }

    private void OnDestroy()
    {
        Singleton = null;
    }

    private void HealthChanged(Damage damage)
    {
        HealthSlider.value = observingCharacter.health;
    }

    
}
