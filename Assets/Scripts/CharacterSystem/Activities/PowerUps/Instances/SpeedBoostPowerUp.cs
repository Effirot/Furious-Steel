using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using UnityEngine;


namespace CharacterSystem.PowerUps
{
    public class SpeedBoostPowerUp : PowerUp
    {
        public override void Activate(PowerUpHolder holder)
        {
            if (holder.IsServer)
            {
                foreach (var effects in holder.Invoker.gameObject.GetComponentsInChildren<CharacterEffectsHolder>())
                {
                    effects.AddEffect(new SpeedBoostEffect(10));
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