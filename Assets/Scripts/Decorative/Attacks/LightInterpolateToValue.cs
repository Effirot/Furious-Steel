using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightInterpolateToValue : MonoBehaviour
{
#if !UNITY_SERVER || UNITY_EDITOR
    [SerializeField]
    private float speed = 0.1f;
    
    [SerializeField]
    private float targetValue = 0;

    private Light m_light;
    
    private void Awake()
    {
        m_light = GetComponent<Light>();
    }

    private void Update()
    {
        m_light.intensity = Mathf.Lerp(m_light.intensity, targetValue, speed);
    }
#endif
}
