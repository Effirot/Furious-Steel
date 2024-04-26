using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SwitchableRectTransformInterpolateToTarget : MonoBehaviour
{
    [field : SerializeField]
    public bool Value { get; set; } = true; 
    
    [field : SerializeField]
    public RectTransform Target1 { get; set; } 
    
    [field : SerializeField]
    public RectTransform Target2 { get; set; } 

    [field : SerializeField, Range(0, 1)]
    public float Speed { get; set; } = 0.2f;


    private RectTransform rectTransform => transform as RectTransform; 

    private void OnEnable() => OnValidate();

    private void OnValidate()
    {
        var target = Value ? Target1 : Target2;

        if (target != null)
        {
            rectTransform.localScale = target.localScale;
            rectTransform.sizeDelta = target.sizeDelta;
            rectTransform.rotation = target.rotation;
            rectTransform.position = target.position;
        }
    }

    private void LateUpdate()
    {
        var target = Value ? Target1 : Target2;

        if (target != null)
        {
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, target.localScale, Speed);
            rectTransform.sizeDelta = Vector3.Lerp(rectTransform.sizeDelta, target.sizeDelta, Speed);
            rectTransform.rotation = Quaternion.Lerp(rectTransform.rotation, target.rotation, Speed);
            rectTransform.position = Vector3.Lerp(rectTransform.position, target.position, Speed);
        }
    }
}
