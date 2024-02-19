

using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class PaintableVisualEffect : MonoBehaviour, IPaintable
{
    [SerializeField, Range(0, 100)]
    private float intensity = 10;

    [SerializeField, Range(0, 100)]
    private float intensitySecondary = 10; 
 
    private VisualEffect paintTargetVisualEffect;

    public void SetColor(Color color)
    {
#if !UNITY_SERVER || UNITY_EDITOR
        if (paintTargetVisualEffect.HasVector4("Color"))
        {
            paintTargetVisualEffect.SetVector4("Color", color * intensity);
        }
#endif
    }

    public void SetSecondColor(Color color)
    {
#if !UNITY_SERVER || UNITY_EDITOR
        if (paintTargetVisualEffect.HasVector4("SecondColor"))
        {
            paintTargetVisualEffect.SetVector4("SecondColor", color * intensitySecondary);
        }
#endif
    }

    private void Awake()
    {
#if !UNITY_SERVER || UNITY_EDITOR
        if (paintTargetVisualEffect == null)
        {
            paintTargetVisualEffect = GetComponent<VisualEffect>();
        }
#endif
    }
}