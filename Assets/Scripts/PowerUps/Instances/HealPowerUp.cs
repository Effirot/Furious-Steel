using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using UnityEngine;

public class HealPowerUp : PowerUp
{
    public override GameObject prefab => throw new System.NotImplementedException();

    public override void Activate(PowerUpHolder holder)
    {
        holder.Character.health += holder.Character.MaxHealth / 4;
    }

    public override void OnPick(PowerUpHolder holder)
    {

    }

}
