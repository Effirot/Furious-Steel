using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Attacks;
using CharacterSystem.Blocking;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using CharacterSystem.PowerUps;
using Cinemachine;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UI;
using UnityEngine.VFX;
using static UnityEngine.InputSystem.InputAction;

[RequireComponent(typeof(NetworkCharacter))]
public class CharacterStealthGraphicHider : NetworkBehaviour
{
    [SerializeField] 
    private GameObject[] HiddableObjects; 

    [SerializeField] 
    private MeshRenderer[] HiddableMeshRenderers; 

    [SerializeField] 
    private SkinnedMeshRenderer[] HiddableSkinnedMeshRenderers; 

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
    private float transparency = 1; 

    private NetworkCharacter networkCharacter;

    private NetworkVariable<bool> isObjectHidden_network = new NetworkVariable<bool> (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    private async void Start()
    {
        await UniTask.WaitUntil(() => IsSpawned);

        networkCharacter = GetComponent<NetworkCharacter>();

        HiddableMeshRenderers = GetComponentsInChildren<MeshRenderer>();
        HiddableSkinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    private void LateUpdate()
    {
        var IsObservableInHideObject = stealthObjects.Exists(stealthObject => stealthObject.characterSteathers.Exists(item => item.networkCharacter == CharacterUIObserver.Singleton.observingCharacter));
        transparency = Mathf.Lerp(transparency, IsHidden ? (IsObservableInHideObject ? 0.5f : 0) : 1, 10 * Time.deltaTime); 

        var hideStatus = !IsHidden || IsOwner || IsObservableInHideObject;
        foreach (var item in HiddableObjects)
        {
            item.SetActive(hideStatus);
        }

        var shadowCastMode = IsHidden ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;

        foreach (var item in HiddableMeshRenderers)
        {
            var color = item.material.color;

            color.a = transparency;

            item.material.color = color; 
            item.shadowCastingMode = shadowCastMode;
        }

        foreach (var item in HiddableSkinnedMeshRenderers)
        {
            var color = item.material.color;

            color.a = transparency;

            item.material.color = color; 
            item.shadowCastingMode = shadowCastMode;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<StealthObject>(out var component))
        {
            stealthObjects.Add(component);
            component.characterSteathers.Add(this);
        }

        UpdateHideStatus();
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent<StealthObject>(out var component))
        {
            stealthObjects.Remove(component);
            component.characterSteathers.Remove(this);
        }

        UpdateHideStatus();
    }   

    private void UpdateHideStatus()
    {
        IsHidden = stealthObjects.Any();
    }
}