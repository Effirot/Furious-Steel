

using UnityEngine;

[RequireComponent(typeof(Light))]
public class Latern : MonoBehaviour
{
    new private Light light;
    private float intensity;

    private void Awake()
    {
        light = GetComponent<Light>();
        intensity = light.intensity;
    }
    private void LateUpdate()
    {
        light.intensity = Mathf.Lerp (light.intensity, WeatherManager.Singleton.LaternsEnabled ? intensity : 0, Time.deltaTime);
        
    }
}