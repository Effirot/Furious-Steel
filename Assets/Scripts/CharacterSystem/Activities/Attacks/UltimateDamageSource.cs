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

[RequireComponent(typeof(CustomProperty))]
public class UltimateDamageSource : DamageSource
{
    [SerializeField]
    private bool ClearCharge = true; 

    [HideInInspector]
    public CustomProperty chargeValue; 

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        chargeValue = GetComponent<CustomProperty>();
        chargeValue.IsActive = !HasOverrides();
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();


        if (Invoker != null)
        {
            Invoker.onDamageDelivered -= OnDamageDelivered_Event;
        }
    }

    public override void Play()
    {        
        if (chargeValue.Value >= chargeValue.MaxValue && IsActive)
        {
            if (ClearCharge)
            {
                chargeValue.Value = 0;
            }

            base.Play();
        }
    }

    protected override void Start()
    {
        base.Start();
        
        Invoker.onDamageDelivered += OnDamageDelivered_Event;
    }

    private void OnDamageDelivered_Event(DamageDeliveryReport report)
    {
        if (report.isDelivered && 
            !report.isBlocked && 
            report.damage.RechargeUltimate)
        {
            chargeValue.AddValue(report);
        }
    }
}