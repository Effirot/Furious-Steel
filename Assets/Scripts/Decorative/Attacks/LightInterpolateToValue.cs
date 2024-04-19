using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightInterpolateToValue : MonoBehaviour
{
    [SerializeField]
    private float speed = 0.1f;
    
    [SerializeField]
    private float targetValue = 0;

    private new Light light;
    
    private void Awake()
    {
        light = GetComponent<Light>();
    }

    private void Update()
    {
        light.intensity = Mathf.Lerp(light.intensity, targetValue, speed);
    }
}
