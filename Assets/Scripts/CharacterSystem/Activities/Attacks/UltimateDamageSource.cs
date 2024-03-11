using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class UltimateDamageSource : DamageSource
{
    [SerializeField, Range(0, 2000)]
    public float RequireDamage = 500;

    public UnityEvent OnUltimateReady = new();

    public event Action<float> OnValueChanged = delegate { };

    public float DeliveredDamage
    {
        get => network_delivereDamageValue.Value;
        set 
        {
            if (IsServer)
            {
                network_delivereDamageValue.Value = value;
            }
        }
    }

    private NetworkVariable<float> network_delivereDamageValue = new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        network_delivereDamageValue.OnValueChanged -= ExecuteOnValueChangedEvent;
        
        if (Invoker != null)
        {
            Invoker.OnDamageDelivered -= OnDamageDelivered_Event;
        }
    }

    public override void StartAttack()
    {
        if (DeliveredDamage >= RequireDamage && 
            Invoker.permissions.HasFlag(CharacterPermission.AllowUltimate) &&
            Invoker.permissions.HasFlag(CharacterPermission.AllowAttacking) &&
            !Invoker.isStunned && 
            IsPerforming &&
            !IsAttacking)
        {
            DeliveredDamage = 0;

            base.StartAttackForced();
        }
    }

    private void Start()
    {
        network_delivereDamageValue.OnValueChanged += ExecuteOnValueChangedEvent;
        Invoker.OnDamageDelivered += OnDamageDelivered_Event;
    }

    private void OnDamageDelivered_Event(DamageDeliveryReport report)
    {
        if (IsServer && report.damage.RechargeUltimate)
        {
            var newValue = Mathf.Clamp(DeliveredDamage + report.damage.value, 0, RequireDamage);
            
            if (DeliveredDamage != newValue)
            {
                OnUltimateReady.Invoke();

                DeliveredDamage = Mathf.Clamp(DeliveredDamage + report.damage.value, 0, RequireDamage);
            }
        }
    }
    private void ExecuteOnValueChangedEvent(float Old, float New)
    {
        OnValueChanged?.Invoke(New);
    }
}