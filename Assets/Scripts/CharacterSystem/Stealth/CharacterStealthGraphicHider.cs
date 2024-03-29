using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Objects;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
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
    private NetworkCharacter character; 

    private NetworkVariable<bool> isObjectHidden_network = new NetworkVariable<bool> (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        character = GetComponent<NetworkCharacter>();
    }
    public override void OnNetworkDespawn()
    {
        foreach(var component in stealthObjects)
        {
            if (component != null && component.characterSteathers.Contains(this))
            {
                component.characterSteathers.Remove(this);
            }
        }
    }

    private async void Start()
    {
        await UniTask.WaitUntil(() => IsSpawned);

        HiddableMeshRenderers = GetComponentsInChildren<MeshRenderer>();
        HiddableSkinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    private void LateUpdate()
    {
        var IsObservableInHideObject = stealthObjects.Exists(
            stealthObject => stealthObject.characterSteathers.Exists(item => item != null && item.transform == CharacterUIObserver.Singleton.observingCharacter));
        
        var hideStatus = !IsHidden || IsObservableInHideObject;
        
        transparency = Mathf.Lerp(transparency, IsHidden ? (IsObservableInHideObject ? 0.5f : 0) : 1, 20 * Time.deltaTime); 

        foreach (var item in HiddableObjects)
        {
            item.SetActive(hideStatus);
        }

        var shadowCastMode = IsHidden ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;

        foreach (var item in HiddableMeshRenderers)
        {
            if (item != null && item.material != null)
            {
                var color = item.material.color;

                color.a = transparency;

                item.material.color = color; 
                item.shadowCastingMode = shadowCastMode;
            }
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