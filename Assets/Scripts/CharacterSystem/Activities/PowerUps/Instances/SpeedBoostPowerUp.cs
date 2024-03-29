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
                foreach (var effects in holder.Character.GetComponentsInChildren<CharacterEffectsHolder>())
                {
                    effects.AddEffect(new SpeedBoostEffect(5));
                }
            }
        }

        public override void OnPick(PowerUpHolder holder)
        {
            
        }
    }
}