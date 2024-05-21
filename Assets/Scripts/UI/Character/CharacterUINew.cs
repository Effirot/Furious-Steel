using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Attacks;
using CharacterSystem.Objects;
using CharacterSystem.PowerUps;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CharacterUINew : MonoBehaviour
{
    [SerializeField]
    private NetworkCharacter _networkCharacter;

    [SerializeField]
    private GameObject CustomPropertyDrawerPrefab;
    
    [SerializeField]
    private TMP_Text NameField;

    [SerializeField]
    private TMP_Text HealthField;

    [SerializeField]
    private Slider HealthSlider;


    private List<GameObject> additiveInstantiatedUIGameObjects = new();

    public NetworkCharacter NetworkCharacter {
        get => _networkCharacter;
        set {
            _networkCharacter = value;

            UpdateValue();
        }
    }

    private void Start()
    {
        if (NetworkCharacter == null)
        {
            NetworkCharacter = PlayerNetworkCharacter.Owner;

            PlayerNetworkCharacter.OnOwnerPlayerCharacterSpawn += (character) => NetworkCharacter = character;
        }
    }
 
    private  async void UpdateValue()
    {
        RemoveAllAdditiveUI();

        if (NetworkCharacter == null) return;
        
        await UniTask.WaitForSeconds(0.2f);

        if (CustomPropertyDrawerPrefab != null)
        {
            Debug.LogError(NetworkCharacter.gameObject.name);
            foreach (var property in NetworkCharacter.gameObject.GetComponentsInChildren<CustomProperty>())
            {                
                var CustomPropertyDrawerObject = Instantiate(CustomPropertyDrawerPrefab, CustomPropertyDrawerPrefab.transform);

                CustomPropertyDrawerObject.SetActive(true);
                CustomPropertyDrawerObject.GetComponent<CharacterUICustomPropertyDrawer>().Initialize(property);

                additiveInstantiatedUIGameObjects.Add(CustomPropertyDrawerObject);
            }
        }

        if (HealthSlider != null)
        {
            HealthSlider.maxValue = NetworkCharacter.maxHealth;
            HealthSlider.value = NetworkCharacter.health;
            
            NetworkCharacter.onHealthChanged += (Value) => HealthSlider.value = Value;
            
            var rect = HealthSlider.transform as RectTransform;
            rect.sizeDelta = new Vector2 (HealthSlider.maxValue * 4f, rect.sizeDelta.y);
        }

        if (HealthField != null)
        {
            HealthField.text = _networkCharacter.health.ToString();
            _networkCharacter.onHealthChanged += (Value) => HealthField.text = Mathf.RoundToInt(Value).ToString();
        }

        if (NameField != null)
        {
            NameField.text = NetworkCharacter.name;
        }
    }

    private void RemoveAllAdditiveUI()
    {
        while (additiveInstantiatedUIGameObjects.Any())
        {
            Destroy(additiveInstantiatedUIGameObjects[0]);

            additiveInstantiatedUIGameObjects.RemoveAt(0);
        }

        additiveInstantiatedUIGameObjects.Clear();
    }
}