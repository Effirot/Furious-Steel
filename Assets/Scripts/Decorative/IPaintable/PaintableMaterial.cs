

using UnityEngine;

public class PaintableMaterial : MonoBehaviour, IPaintable
{
    [SerializeField] 
    private Material paintTargetMaterial;
    [SerializeField] 
    private Material secondPaintTargetMaterial;

    public void SetColor(Color color)
    {
        paintTargetMaterial.color = color;
    }

    public void SetSecondColor(Color color)
    {
        secondPaintTargetMaterial.color = color;
    }
}