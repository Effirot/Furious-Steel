using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using UnityEngine;


namespace CharacterSystem.PowerUps
{
    public class HealPowerUp : PowerUp
    {
        public override bool IsOneshot => true;

        public override void Activate(PowerUpHolder holder)
        {
            Damage.Deliver(holder.Invoker, new Damage(new RegenerationEffect(7, holder.Invoker.maxHealth / 7f)));
        }
        
        public override void OnPick(PowerUpHolder holder)
        {
            
        }

    }
}