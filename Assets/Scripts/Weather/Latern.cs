

using UnityEngine;

[RequireComponent(typeof(Light))]
public class CharacterLatern : MonoBehaviour
{
    [Header("Optional")]
    [SerializeField]
    private CharacterStealthGraphicHider stealth;

    new private Light light;

    private float intensity;

    private void Awake()
    {
        light = GetComponent<Light>();
        intensity = light.intensity;
    }
    private void LateUpdate()
    {
        light.intensity = Mathf.Lerp (
            light.intensity, 
            WeatherManager.Singleton.LaternsEnabled ? 
                (stealth != null && stealth.IsHidden ? (stealth.IsOwner ? intensity / 3 : 0) : intensity) : 0, 
            3 * Time.deltaTime);
    }
}