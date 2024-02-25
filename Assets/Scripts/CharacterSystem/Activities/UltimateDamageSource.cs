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
        if (network_delivereDamageValue.Value >= RequireDamage && Invoker.permissions.HasFlag(CharacterPermission.AllowUltimate))
        {
            network_delivereDamageValue.Value = 0;

            base.StartAttack();
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
            var newValue = Mathf.Clamp(network_delivereDamageValue.Value + report.damage.value, 0, RequireDamage);
            
            if (network_delivereDamageValue.Value != newValue)
            {
                OnUltimateReady.Invoke();

                network_delivereDamageValue.Value = Mathf.Clamp(network_delivereDamageValue.Value + report.damage.value, 0, RequireDamage);
            }
        }
    }
    private void ExecuteOnValueChangedEvent(float Old, float New)
    {
        OnValueChanged?.Invoke(New);
    }
}