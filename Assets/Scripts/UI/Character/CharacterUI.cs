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

public class CharacterUI : MonoBehaviour
{
    [SerializeField]
    private bool LookToCamera = false;

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

    [SerializeField]
    private TMP_Text ComboField;

    [SerializeField]
    private List<PowerUpDrawer> powerUpDrawers = new();


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
        else
        {
            gameObject.SetActive(!NetworkCharacter.IsOwner);

            UpdateValue();
        }
    }

#if !UNITY_SERVER || UNITY_EDITOR
    private void OnEnable()
    {
        if (LookToCamera)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }
    private void LateUpdate()
    {
        if (LookToCamera)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }
#endif


    private async void UpdateValue()
    {
        RemoveAllAdditiveUI();

        await UniTask.WaitForSeconds(0.1f);
        
        if (NetworkCharacter == null) 
        {

        }
        else
        {
            if (CustomPropertyDrawerPrefab != null)
            {
                foreach (var property in NetworkCharacter.gameObject.GetComponentsInChildren<CustomProperty>())
                {                
                    var CustomPropertyDrawerObject = Instantiate(CustomPropertyDrawerPrefab, CustomPropertyDrawerPrefab.transform.parent);

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
            
            if (ComboField != null)
            {
                ComboField.gameObject.SetActive(false);
                if (NetworkCharacter is IDamageSource)
                {
                    var damageSource = NetworkCharacter as IDamageSource;

                    damageSource.onComboChanged += (value) => {

                        ComboField.gameObject.SetActive(value > 0);

                        ComboField.text = value.ToString(); 
                    };
                }
            }

            var holders = NetworkCharacter.gameObject.GetComponentsInChildren<PowerUpHolder>().Where(holder => !holder.HasOverrides()).ToArray();
            for (int i = 0; i < Mathf.Min(holders.Length, powerUpDrawers.Count()); i++)
            {
                powerUpDrawers[i].Initialize(holders[i]);
            }
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