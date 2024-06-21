using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Cysharp.Threading.Tasks;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkCharacter))]
public class CharacterStealthGraphicHider : NetworkBehaviour
{
    [SerializeField] 
    private GameObject[] HiddableObjects; 

    [SerializeField] 
    private SkinnedMeshRenderer[] HiddableSkinnedMeshRenderers; 

    [SerializeField] 
    private UnityEvent OnCharacterHide = new UnityEvent();
    [SerializeField] 
    private UnityEvent OnCharacterUnhide = new UnityEvent();

    [SyncVar(hook = nameof(OnHideStateChanged))]
    public bool IsHidden = false;

    private List<StealthObject> stealthObjects = new();
    private float transparency = 1;
    private NetworkCharacter character; 


    private Coroutine OnHitTimeOut;

    public override void OnStartClient()
    {
        base.OnStartClient();

        character = GetComponent<NetworkCharacter>();

        character.onDamageRecieved += UnhideWhileGetDamage;

        HiddableSkinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();    
    }
    public override void OnStopClient()
    {
        base.OnStopClient();

        foreach(var component in stealthObjects)
        {
            if (component != null && component.characterSteathers.Contains(this))
            {
                component.characterSteathers.Remove(this);
            }
        }

        character.onDamageRecieved -= UnhideWhileGetDamage;
    }

    private void LateUpdate()
    {
        var IsObservableInHideObject = stealthObjects.Exists(
            stealthObject => stealthObject.characterSteathers.Exists(item => item != null && System.Object.ReferenceEquals(item.character, CharacterCameraObserver.Singleton.ObservingObject)));
        
        var hideStatus = !IsHidden || IsObservableInHideObject;
        
        transparency = Mathf.Lerp(transparency, IsHidden ? (IsObservableInHideObject ? 0.5f : 0) : 1, 20 * Time.deltaTime); 

        foreach (var item in HiddableObjects)
        {
            item.SetActive(hideStatus);
        }

        var shadowCastMode = IsHidden ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;

        foreach (var item in HiddableSkinnedMeshRenderers)
        {
            var material = item.materials.FirstOrDefault();
            
            if (material != null)
            {
                var color = material.color;

                color.a = transparency;

                material.color = color; 
                item.shadowCastingMode = shadowCastMode;
            }
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

    private void UnhideWhileGetDamage(Damage damage)
    {
        if (OnHitTimeOut != null)
        {
            StopCoroutine(OnHitTimeOut);
            OnHitTimeOut = null;
        }

        OnHitTimeOut = StartCoroutine(OnHitRoutine());
    }
    private IEnumerator OnHitRoutine()
    {
        yield return new WaitForSeconds(1); 

        OnHitTimeOut = null;
    }

    private void OnHideStateChanged(bool Old, bool New)
    {
        if (New)
        {
            OnCharacterHide.Invoke();
        }
        else
        {
            OnCharacterUnhide.Invoke();
        }
    }
}