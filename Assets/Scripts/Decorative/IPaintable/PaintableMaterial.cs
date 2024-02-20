

using UnityEngine;

public class PaintableMaterial : MonoBehaviour, IPaintable
{
    [SerializeField] 
    private SkinnedMeshRenderer paintTargetRenderer;
    [SerializeField] 
    private SkinnedMeshRenderer secondPaintTargetRenderer;

    public void SetColor(Color color)
    {
        if (paintTargetRenderer != null)
        {
            paintTargetRenderer.material.color = color;
        }
    }

    public void SetSecondColor(Color color)
    {
        if (secondPaintTargetRenderer != null)
        {
            secondPaintTargetRenderer.material.color = color;
        }
    }
}