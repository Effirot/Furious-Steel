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
            var ultimate = holder.Character.GetComponentInChildren<UltimateDamageSource>();

            if (ultimate != null)
            {
                ultimate.DeliveredDamage = ultimate.RequireDamage;
            }
        }

        public override void OnPick(PowerUpHolder holder)
        {
            
        }

    }
}