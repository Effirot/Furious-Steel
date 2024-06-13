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

    public override bool IsActive => base.IsActive && chargeValue.Value >= chargeValue.MaxValue;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        chargeValue = GetComponent<CustomProperty>();
        chargeValue.IsActive = !HasOverrides();
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();


        if (Source != null)
        {
            Source.onDamageDelivered -= OnDamageDelivered_Event;
        }
    }

    public override void Play()
    {        
        if (IsActive)
        {
            base.Play();

            if (ClearCharge)
            {
                chargeValue.Value = 0;
            }
        }
    }

    protected override void Start()
    {
        base.Start();
        
        Source.onDamageDelivered += OnDamageDelivered_Event;
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