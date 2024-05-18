using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Objects;
using CharacterSystem.PowerUps;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CharacterMiniUI : MonoBehaviour
{
    [SerializeField]
    private NetworkCharacter networkCharacter;
    
    [SerializeField]
    private TMP_Text NicknameField;
    
    [SerializeField]
    private Slider HealthField;

    [SerializeField]
    private PowerUpDrawer[] drawers;

    [SerializeField]
    private PowerUpHolder[] holders; 
    
    [SerializeField]
    private Slider UltimateField;

    
    private void Start()
    {
        if (networkCharacter.IsOwner)
        {
            transform.localScale = Vector3.one * 1.3f;
        }

        SetNickname();

        SubscribeToHealthBar();
        SubscribeToHolders();
        SubscribeToUltimate();
    }
    private void OnEnable()
    {
        transform.rotation = Camera.main.transform.rotation;
    }
    private void OnDestroy()
    {
        UnsubscribeToHolders();
        UnsubscribeToHealthBar();
    }
    
    private void LateUpdate()
    {
        transform.rotation = Camera.main.transform.rotation;
    }

    private void SetHealthSliderValue_Event(float value)
    {
        HealthField.maxValue = networkCharacter.maxHealth;
        HealthField.value = value;
    }


    private void SetNickname()
    {
        if (NicknameField != null && networkCharacter is PlayerNetworkCharacter)
        {
            var player = (PlayerNetworkCharacter)networkCharacter;
                            
            NicknameField.text = player.ClientData.Name.ToString();
        }
    }

    private void SubscribeToHealthBar()
    {
        if (HealthField != null)
        {
            SetHealthSliderValue_Event(networkCharacter.health);
            networkCharacter.onHealthChanged += SetHealthSliderValue_Event;
        }
    }
    private void UnsubscribeToHealthBar()
    {
        if (HealthField != null)
        {
            networkCharacter.onHealthChanged -= SetHealthSliderValue_Event;
        }
    }

    private void SubscribeToHolders()
    {
        holders = transform.parent.GetComponentsInChildren<PowerUpHolder>().Where(holder => !holder.HasOverrides()).ToArray();

        for (int i = 0; i < drawers.Length; i++)
        {
            if (i < holders.Length)
            {
                if (drawers[i] != null)
                {
                    drawers[i].gameObject.SetActive(true);
                    drawers[i].Draw(holders[i].powerUp);  
                    holders[i].OnPowerUpChanged += drawers[i].Draw;
                }
            }   
            else
            {
                drawers[i].gameObject.SetActive(false);
            }
        }
    }
    private void UnsubscribeToHolders()
    {
        for (int i = 0; i < drawers.Length; i++)
        {
            if (i < holders.Length)
            {
                drawers[i].gameObject.SetActive(false);
                holders[i].OnPowerUpChanged -= drawers[i].Draw;
            } 
        }
    }
    
    private void SubscribeToUltimate()
    {
        var ultimate = networkCharacter.GetComponentInChildren<UltimateDamageSource>();

        UltimateField.gameObject.SetActive(ultimate != null);
        if (ultimate != null)
        {
            UltimateField.maxValue = ultimate.RequireDamage;
            ultimate.OnValueChanged += OnUltimateValueChanged_event;
        }
    }

    private void OnUltimateValueChanged_event(float value)
    {
        UltimateField.value = value;
    }
}
