

using CharacterSystem.Attacks;
using UnityEngine;

public class PropertyRequiredDamageSource : DamageSource
{
    [SerializeField, Range(0, 500)]
    private float MinRequiredValue = 0;

    [SerializeField]
    private CustomProperty customProperty;

    public override void Play()
    {
        if (customProperty.Value >= MinRequiredValue)
        {
            base.Play();
        }
    }
}