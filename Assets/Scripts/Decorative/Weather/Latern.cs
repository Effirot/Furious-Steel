

using UnityEngine;

[RequireComponent(typeof(Light))]
public class CharacterLatern : MonoBehaviour
{
    [Header("Optional")]
    [SerializeField]
    private CharacterStealthGraphicHider stealth;

    private Light Light;

    private float intensity;

    private void Awake()
    {
        Light = GetComponent<Light>();
        intensity = Light.intensity;
    }
    private void LateUpdate()
    {
#warning mak weather manager
        // Light.intensity = Mathf.Lerp (
        //     Light.intensity, 
        //     WeatherManager.Singleton.LaternsEnabled ? 
        //         (stealth != null && stealth.IsHidden ? (stealth.IsOwner ? intensity / 3 : 0) : intensity) : 0, 
        //     3 * Time.deltaTime);
    }
}