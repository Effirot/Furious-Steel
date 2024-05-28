using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using System;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using CharacterSystem.Blocking;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CharacterSystem.PowerUps
{
    public sealed class BagPowerUpHolder : PowerUpHolder
    {
        protected override void OnTriggerStay(Collider other)
        {
            if (HasOverrides()) return;

            if (other.TryGetComponent<PowerUpContainer>(out var container) && IsServer)
            {
                if (container.powerUp.IsOneshot)
                {
                    if (powerUp == null)
                    {
                        powerUp = container.powerUp;
                        
                        container.powerUp.OnPick(this);
                        container.NetworkObject.Despawn();
                    }
                }
            }
        }
    }
}