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

public class StealthGraphicHider : NetworkBehaviour
{
    [SerializeField] 
    private GameObject[] HiddableObjects; 

    public bool IsHidden 
    { 
        get => isObjectHidden_network.Value;
        set 
        {
            if (IsServer)
            {
                isObjectHidden_network.Value = value;
            }
        }
    }

    private List<StealthObject> stealthObjects = new();

    private NetworkVariable<bool> isObjectHidden_network = new NetworkVariable<bool> (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        isObjectHidden_network.OnValueChanged += (Old, New) => UpdateHiddenObjects();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<StealthObject>(out var component))
        {
            stealthObjects.Add(component);
        }

        UpdateHideStatus();
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent<StealthObject>(out var component))
        {
            stealthObjects.Remove(component);
        }

        UpdateHideStatus();
    }   

    private void UpdateHideStatus()
    {
        IsHidden = !stealthObjects.Any();
    }
    private void UpdateHiddenObjects()
    {
        foreach (var item in HiddableObjects)
        {
            item?.SetActive(IsHidden);
        }
    }
}