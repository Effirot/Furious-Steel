

using CharacterSystem.Attacks;
using UnityEngine;

[RequireComponent(typeof(CustomProperty))]
public class PropertyRequiredDamageSource : DamageSource
{
    [SerializeField, Range(0, 500)]
    private float MinRequiredValue = 0;

    private CustomProperty customProperty;

    private void Awake()
    {
        customProperty = GetComponent<CustomProperty>();
    }

    public override void StartAttack()
    {
        if (customProperty.Value >= MinRequiredValue)
        {
            base.StartAttack();
        }
    }
}