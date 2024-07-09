

using CharacterSystem.DamageMath;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CustomProperty : NetworkBehaviour
{
    [Space]
    [SerializeField, Range(0, 1000)]
    public float DefaultValue = 10;

    [SerializeField, Range(0, 1000)]
    public float MaxValue = 10;
    
    [Space]
    [SerializeField]
    public Color color = Color.yellow;
    
    [SerializeField]
    public Color fullChargeColor = Color.red;

    [Space]
    public UnityEvent<float> OnValueChanged = new();


    public float Value {
        get => IsActive ? _value : 0;
        set {
            if (IsActive)
            {
                var newValue = Mathf.Clamp(value, 0, MaxValue); 
                
                if (isServerOnly)
                {
                    OnValueChangedHook(_value, newValue);
                }
                
                _value = newValue;
            }
        }
    }

    [SyncVar]
    public bool IsActive = true;

    [SyncVar(hook = nameof(OnValueChangedHook))]
    private float _value;


    public void AddValue(float value)
    {
        Value += value;
    } 
    public void AddValue(DamageDeliveryReport report)
    {
        if (report.isDelivered)
        {
            AddValue(report.damage.value);
        }
    } 

    public void SubtractValue(float value)
    {
        Value -= value;
    } 
    public void SubtractValue(DamageDeliveryReport report)
    {
        if (report.isDelivered)
        {
            SubtractValue(report.damage.value);
        }
    } 

    public void MultiplyValue(float value)
    {
        Value *= value;
    }
    public void DivideValue(float value)
    {
        Value /= value;
    }

    public void RefillOnKill(DamageDeliveryReport report)
    {
        if (report.isLethal)
        {
            Value = MaxValue;
        }
    }

    protected virtual void Start()
    {        
        Value = Mathf.Min(DefaultValue, MaxValue);
    }
    protected virtual void OnValueChangedHook(float Old, float New)
    {
        OnValueChanged.Invoke(New);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CustomProperty), true)]
    public class CustomProperty_Editor : Editor
    {
        private new CustomProperty target => base.target as CustomProperty;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.LabelField(target.Value.ToString());
        }
    } 
#endif
}