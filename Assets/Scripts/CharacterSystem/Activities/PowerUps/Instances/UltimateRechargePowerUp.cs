using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using UnityEngine;


namespace CharacterSystem.PowerUps
{
    public class UltimateRechargePowerUp : PowerUp
    {
        public override void Activate(PowerUpHolder holder)
        {
            if (holder.IsServer)
            {
                foreach (var property in holder.Invoker.gameObject.GetComponentsInChildren<CustomProperty>())
                {
                    property.Value = property.MaxValue;
                }
            }
        }

        public override bool IsValid(PowerUpHolder holder)
        {
            return holder is BagPowerUpHolder;
        }


        public override void OnPick(PowerUpHolder holder)
        {
            
        }

    }
}