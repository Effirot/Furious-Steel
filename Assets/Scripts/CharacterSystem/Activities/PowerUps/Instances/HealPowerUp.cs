using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using UnityEngine;

public class HealPowerUp : PowerUp
{
    public override void Activate(PowerUpHolder holder)
    {
        holder.Character.Heal (new Damage() 
        {
            value = holder.Character.maxHealth / 4
        });
    }

    public override void OnPick(PowerUpHolder holder)
    {
    }

}
