using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Attacks;
using CharacterSystem.Blocking;
using CharacterSystem.DamageMath;
using CharacterSystem.PowerUps;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UI;
using UnityEngine.VFX;
using static UnityEngine.InputSystem.InputAction;

public class StealthGraphicHider : MonoBehaviour
{
    [SerializeField] 
    private GameObject[] HiddableObjects; 

    private List<StealthObject> stealthObjects = new(); 

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<StealthObject>(out var component))
        {
            stealthObjects.Add(component);
        }

        UpdateHiddenObjects();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent<StealthObject>(out var component))
        {
            stealthObjects.Remove(component);
        }

        UpdateHiddenObjects();
    }   

    private void UpdateHiddenObjects()
    {
        foreach (var item in HiddableObjects)
        {
            item.SetActive(!stealthObjects.Any());
        }
    }
}