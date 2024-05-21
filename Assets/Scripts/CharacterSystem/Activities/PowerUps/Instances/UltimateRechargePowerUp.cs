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
                foreach (var ultimate in holder.Invoker.gameObject.GetComponentsInChildren<UltimateDamageSource>())
                {
                    ultimate.chargeValue.Value = ultimate.chargeValue.MaxValue;
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