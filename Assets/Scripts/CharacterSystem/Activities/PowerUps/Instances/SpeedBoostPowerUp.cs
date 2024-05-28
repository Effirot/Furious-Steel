using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using UnityEngine;


namespace CharacterSystem.PowerUps
{
    public class SpeedBoostPowerUp : PowerUp
    {
        public override bool IsOneshot => true;
        
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

        public override void OnPick(PowerUpHolder holder)
        {
            
        }
    }
}