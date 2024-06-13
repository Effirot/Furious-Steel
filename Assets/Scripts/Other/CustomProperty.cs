

using CharacterSystem.DamageMath;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

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
        get => IsActive ? network_value.Value : 0;
        set {
            if (IsServer && IsActive)
            {
                network_value.Value = Mathf.Clamp(value, 0, MaxValue);
            }
        }
    }
    public bool IsActive {
        get => network_IsActive.Value;
        set {
            if (IsServer)
            {
                network_IsActive.Value = value;
            }
        }
    }

    protected NetworkVariable<float> network_value = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    protected NetworkVariable<bool> network_IsActive = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        Value = Mathf.Min(DefaultValue, MaxValue);

        network_value.OnValueChanged += (Old, New) => OnValueChanged.Invoke(New);
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
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