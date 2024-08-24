

using System;
using System.Linq;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif

[CreateAssetMenu(fileName = "MusicManagerAsset", menuName = "MusicAsset", order = 0)]
public class MusicManagerAsset : ScriptableObject
{
    public AudioClip[] audioClips;

    public MusicManagerAssetValuePerStress[] ClipVolumeValues;

    private void OnValidate()
    {
        foreach (var value in ClipVolumeValues)
        {
            if (value.VolumeValues.Length != audioClips.Length)
            {
                var buffer = value.VolumeValues;
                value.VolumeValues = new float[audioClips.Length];
                
                for (int i = 0; i < buffer.Length && i < value.VolumeValues.Length; i++)
                {
                    buffer[i] = value.VolumeValues[i];
                }
            }
        }
    }
}

[Serializable]
public sealed class MusicManagerAssetValuePerStress
{
    [Range(0, 1f)]
    public float StressValue = 1;

    [Range(0, 1.25f)]
    public float[] VolumeValues = new float[0];

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MusicManagerAssetValuePerStress))]
    private class MusicManagerAssetValuePerStress_Editor : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            container.Add(new PropertyField(property.FindPropertyRelative("StressValue")));

            SerializedProperty arraySizeProp = property.FindPropertyRelative("VolumeValues");

            for (int i = 0; i < arraySizeProp.arraySize; i++)
            {
                EditorGUI.indentLevel++;

                var field = new PropertyField(arraySizeProp.GetArrayElementAtIndex(i));
                field.label = "";
                container.Add(field);

                EditorGUI.indentLevel--;
            }

            return container;
        }
    }
#endif 
}