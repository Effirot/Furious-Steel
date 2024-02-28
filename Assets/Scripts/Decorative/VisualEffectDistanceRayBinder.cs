using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class VisualEffectDistanceRayBinder : MonoBehaviour
{
    [SerializeField, Range(0, 30)]
    private float MaxDistance = 5;

    private void Update()
    {
        
    }
}
