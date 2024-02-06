using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using UnityEngine;

public class HealPowerUp : PowerUp
{
    public override GameObject prefab => throw new System.NotImplementedException();

    public const float HealingValue = 1000;

    public override void Activate(NetworkCharacter character)
    {
        character.health += HealingValue;
    }
}
