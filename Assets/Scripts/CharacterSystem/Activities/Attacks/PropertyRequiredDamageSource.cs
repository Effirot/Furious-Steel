

using CharacterSystem.Attacks;
using UnityEngine;

public class PropertyRequiredDamageSource : DamageSource
{
    [SerializeField, Range(0, 500)]
    private float MinRequiredValue = 0;

    [SerializeField]
    private bool StopOnValueLessThenMinimum = true;

    [SerializeField]
    private CustomProperty customProperty;

    public override bool IsActive => base.IsActive && customProperty.Value >= MinRequiredValue;

    private void Start()
    {
        customProperty.OnValueChanged.AddListener(value => {
            if (StopOnValueLessThenMinimum && value <= MinRequiredValue) {
                Stop();
            }
        });
    }
}