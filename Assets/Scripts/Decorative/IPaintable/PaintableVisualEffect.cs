

using UnityEngine;
using UnityEngine.VFX;

public class PaintableVisualEffect : MonoBehaviour, IPaintable
{
    [SerializeField]
    private VisualEffect paintTargetVisualEffect;

    public void SetColor(Color color)
    {
        if (paintTargetVisualEffect.HasVector4("Color"))
        {
            paintTargetVisualEffect.SetVector4("Color", color * 10);
        }
    }

    public void SetSecondColor(Color color)
    {
        if (paintTargetVisualEffect.HasVector4("SecondColor"))
        {
            paintTargetVisualEffect.SetVector4("SecondColor", color * 8);
        }
    }
}