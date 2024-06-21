using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Effects;
using CharacterSystem.Objects;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

public class PlatePlayerNetworkCharacter : PlayerNetworkCharacter
{
    [Space]
    [Header("Custom block effect")]
    [SerializeField]
    private Damage HealingEffect = new Damage(30, null, 0, Vector3.zero, Damage.Type.Effect, new SpeedBoostEffect(3, 5));

    public override bool Hit(Damage damage)
    {
        var result = base.Hit(damage);

        if (result)
        {
            HealingEffect.sender = this;
            HealingEffect.value = -Mathf.Abs(HealingEffect.value);
            Damage.Deliver(this, HealingEffect);

            foreach (var effect in HealingEffect.Effects)
            { 
                GetComponent<CharacterEffectsHolder>().AddEffect(effect);
            }
        }

        return result;
    }
}
