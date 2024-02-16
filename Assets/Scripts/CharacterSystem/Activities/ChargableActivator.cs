using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class ChargableActivator : SyncedActivities<ISyncedActivitiesSource>
{
    [SerializeField, Range(0, 1000)]
    private float RequireValue;
    
    [SerializeField, Range(0, 1000)]
    private float Value = 250;

    public UnityEvent OnActivate = new();

    public void AddValue(float value)
    {
        RequireValue = Mathf.Clamp(RequireValue + value, 0, RequireValue);
    }

    public void AddValue(Damage value)
    {
        AddValue(value.value);
    }

    protected override void OnStateChanged(bool IsPressed)
    {
        if (Value >= RequireValue && IsPressed)
        {
            OnActivate.Invoke();
        }   
    }
}