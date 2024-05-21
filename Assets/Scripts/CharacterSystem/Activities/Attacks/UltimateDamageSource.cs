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
        chargeValue = GetComponent<CustomProperty>();
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (Invoker != null)
        {
            Invoker.onDamageDelivered -= OnDamageDelivered_Event;
        }
    }

    public override void StartAttack()
    {
        if (chargeValue.MaxValue <= chargeValue.Value && 
            Invoker.permissions.HasFlag(CharacterPermission.AllowAttacking) &&
            !Invoker.isStunned && 
            IsPerforming &&
            !IsAttacking && 
            !HasOverrides())
        {
            if (ClearCharge)
            {
                chargeValue.Value = 0;
            }

            base.StartAttackForced();
        }
    }

    
    protected virtual void Start()
    {        
        Invoker.onDamageDelivered += OnDamageDelivered_Event;
    }

    private void OnDamageDelivered_Event(DamageDeliveryReport report)
    {
        if (IsServer && report.isDelivered && report.damage.RechargeUltimate)
        {
            chargeValue.AddValue(report);
        }
    }
}