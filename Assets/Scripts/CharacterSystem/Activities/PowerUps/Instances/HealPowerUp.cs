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
            Damage.Deliver(holder.Source, new Damage(new RegenerationEffect(7, holder.Source.maxHealth / 7f)));
        }
        
        public override void OnPick(PowerUpHolder holder)
        {
            
        }

    }
}