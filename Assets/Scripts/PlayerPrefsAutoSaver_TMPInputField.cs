using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_InputField))]
public class PlayerPrefsAutoSaver_TMPInputField : MonoBehaviour
{
    [SerializeField]
    private string Key;
    
    [SerializeField]
    private string DefaultValue;

    private TMP_InputField field;

    private void Awake()
    {
        field = GetComponent<TMP_InputField>();
    
        Load();

        field.onValueChanged.AddListener(Save); 
    }
    private void OnDestroy()
    {
        field.onValueChanged.RemoveListener(Save); 
    }
   
    private void Save(string value)
    {
        PlayerPrefs.SetString(Key, value);
    }

    private void Load()
    {
        field.text = PlayerPrefs.GetString(Key, DefaultValue);
    }

}
