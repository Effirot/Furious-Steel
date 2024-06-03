using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using UnityEngine;


namespace CharacterSystem.PowerUps
{
    public class UltimateRechargePowerUp : PowerUp
    {
        public override bool IsOneshot => true;
        
        public override void Activate(PowerUpHolder holder)
        {
            if (holder.IsServer)
            {
                foreach (var property in holder.Source.gameObject.GetComponentsInChildren<CustomProperty>())
                {
                    property.Value = property.MaxValue;
                }
            }
        }

        public override void OnPick(PowerUpHolder holder)
        {
            
        }

    }
}