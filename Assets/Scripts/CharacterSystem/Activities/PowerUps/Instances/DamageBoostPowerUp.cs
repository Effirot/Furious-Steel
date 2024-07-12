using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Effects;
using CharacterSystem.Objects;
using UnityEngine;


namespace CharacterSystem.PowerUps
{
    public class DamageBoostPowerUp : PowerUp
    {
        public override bool IsOneshot => true;
        
        public override void Activate(PowerUpHolder holder)
        {
            Damage.Deliver(holder.Source, new Damage(new DamageBoostEffect(5, 2.5f)));
        }

        public override void OnPick(PowerUpHolder holder)
        {
            
        }
    }
}