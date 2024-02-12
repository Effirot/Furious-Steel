

using UnityEngine;
using UnityEngine.VFX;

public class PaintableVisualEffect : MonoBehaviour, IPaintable
{
    [SerializeField]
    private VisualEffect paintTargetVisualEffect;

    public void SetColor(Color color)
    {
        paintTargetVisualEffect.SetVector4("Color", color);
    }

    public void SetSecondColor(Color color)
    {
        paintTargetVisualEffect.SetVector4("SecondColor", color);
    }
}