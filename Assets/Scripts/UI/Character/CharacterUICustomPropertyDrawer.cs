
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class CharacterUICustomPropertyDrawer : MonoBehaviour
{
    public float Value {
        get => slider.value;
        set => slider.value = value;
    }

    public float MaxValue {
        get => slider.maxValue;
        set => slider.maxValue = value;
    }

    private CustomProperty customProperty;
    
    private Slider slider => GetComponent<Slider>();

    public void Initialize(CustomProperty customProperty)
    {
        this.customProperty = customProperty;

        MaxValue = customProperty.MaxValue;
        
        UpdateValue(customProperty.Value);

        customProperty.OnValueChanged.AddListener(UpdateValue);
    }

    private void UpdateValue(float value)
    {
        Value = value;

        if (Value == customProperty.MaxValue)
        {
            slider.targetGraphic.color = customProperty.fullChargeColor;
        }
        else
        {
            slider.targetGraphic.color = customProperty.color;
        }
    }
}