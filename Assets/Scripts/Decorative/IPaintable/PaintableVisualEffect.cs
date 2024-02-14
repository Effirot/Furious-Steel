

using UnityEngine;
using UnityEngine.VFX;

public class PaintableVisualEffect : MonoBehaviour, IPaintable
{
    [SerializeField]
    private VisualEffect paintTargetVisualEffect;

    [SerializeField, Range(0, 100)]
    private float intensity = 10;

    [SerializeField, Range(0, 100)]
    private float intensitySecondary = 10; 

    public void SetColor(Color color)
    {
        if (paintTargetVisualEffect.HasVector4("Color"))
        {
            paintTargetVisualEffect.SetVector4("Color", color * intensity);
        }
    }

    public void SetSecondColor(Color color)
    {
        if (paintTargetVisualEffect.HasVector4("SecondColor"))
        {
            paintTargetVisualEffect.SetVector4("SecondColor", color * intensitySecondary);
        }
    }
}