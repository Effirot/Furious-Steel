using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using TMPro;
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

    
    private void Awake()
    {
        if (networkCharacter.IsOwner)
        {
            transform.localScale = Vector3.one * 1.3f;
        }

        if (NicknameField != null && networkCharacter is PlayerNetworkCharacter)
        {
            var player = (PlayerNetworkCharacter)networkCharacter;
                            
            NicknameField.text = player.ClientData.Name.ToString();
        }

        if (HealthField != null)
        {
            if (networkCharacter is PlayerNetworkCharacter)
            {
                var player = (PlayerNetworkCharacter)networkCharacter;

                HealthField.fillRect.GetComponent<Graphic>().color = player.ClientData.spawnArguments.GetColor();
            }

            SetHealthSliderValue_Event(networkCharacter.health);
            networkCharacter.OnHealthChanged += SetHealthSliderValue_Event;
        }

        SubscribeToHolders();
    }
    private void OnDestroy()
    {
        UnsubscribeToHolders();

        if (HealthField != null)
        {
            networkCharacter.OnHealthChanged -= SetHealthSliderValue_Event;
        }
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

    private void SubscribeToHolders()
    {
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
}
