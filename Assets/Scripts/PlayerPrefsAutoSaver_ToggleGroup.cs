using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ToggleGroup))]
public class PlayerPrefsAutoSaver_ToggleGroup : MonoBehaviour
{
    [SerializeField]
    private string Key;

    private ToggleGroup group;
    private List<Toggle> toggles => typeof(ToggleGroup).GetField("m_Toggles", 
                                            BindingFlags.Public | 
                                            BindingFlags.NonPublic | 
                                            BindingFlags.Instance).GetValue(group) as List<Toggle>;

    private void Start()
    {
        group = GetComponent<ToggleGroup>();
    
        Load();

        foreach(var toggle in toggles)
        {
            toggle.onValueChanged.AddListener(Save);
        }
    }
    private void OnDestroy()
    {
        foreach(var toggle in toggles)
        {
            toggle.onValueChanged.RemoveListener(Save);
        }
    }

    private void Save(bool eventValue)
    {
        var toggles = this.toggles;

        var value = string.Join(",", toggles.Where(toggle => toggle.isOn).Select(toggle => toggle.transform.GetSiblingIndex()));

        PlayerPrefs.SetString(Key, value);
    }

    private void Load()
    {
        var toggles = this.toggles;

        var values = PlayerPrefs.GetString(Key, "-1").Split(",").Select(value => int.Parse(value));

        if (toggles != null)
        {
            foreach(var toggle in toggles)
            {
                toggle.isOn = values.Contains(toggle.transform.GetSiblingIndex()) && toggle.IsInteractable() && toggle.isActiveAndEnabled;
            }
        }
    }

}
