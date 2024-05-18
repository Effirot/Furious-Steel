using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using UnityEngine;


namespace CharacterSystem.PowerUps
{
    public class HealPowerUp : PowerUp
    {
        public override void Activate(PowerUpHolder holder)
        {
            holder.Invoker.Heal(new Damage(70, null, 0, Vector3.zero, Damage.Type.Unblockable));
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