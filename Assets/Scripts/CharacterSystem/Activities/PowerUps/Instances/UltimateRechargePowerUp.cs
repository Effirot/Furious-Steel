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
                foreach (var ultimate in holder.Character.GetComponentsInChildren<UltimateDamageSource>())
                {
                    ultimate.DeliveredDamage = ultimate.RequireDamage;
                }
            }
        }

        public override void OnPick(PowerUpHolder holder)
        {
            
        }

    }
}