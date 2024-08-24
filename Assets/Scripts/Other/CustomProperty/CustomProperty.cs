

using CharacterSystem.DamageMath;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using CharacterSystem.Objects;



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
    
    [SerializeField]
    public bool roundToInt = false;

    [Space]
    [SerializeField]
    public Color color = Color.yellow;
    
    [SerializeField]
    public Color fullChargeColor = Color.red;

    [Space]
    [SerializeField]
    public UnityEvent<float> OnValueChanged = new();

    [SerializeField]
    public UnityEvent OnPropertyEmpty = new();
    
    [SerializeField]
    public UnityEvent OnPropertyFull = new();


    public float Value {
        get => IsActive ? _value : 0;
        set {
            if (IsActive)
            {
                var newValue = Mathf.Clamp(value, 0, MaxValue); 
                
                if (roundToInt)
                {
                    newValue = Mathf.RoundToInt(newValue);
                }

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
        if (report.isLethal && report.target is NetworkCharacter)
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
        if (New == MaxValue && Old < New)
        {
            OnPropertyFull.Invoke();
        }
        
        if (New == 0 && Old > New)
        {
            OnPropertyEmpty.Invoke();
        }

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