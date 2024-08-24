using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MeshRendererDisolver : MonoBehaviour
{
    [SerializeField]
    private float value;

    MeshRenderer meshRenderer;

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

#if !UNITY_SERVER || UNITY_EDITOR
    private void Update()
    {
        var color = meshRenderer.material.color; 
        
        color.a -= value * Time.deltaTime;
    
        meshRenderer.material.color = color;
    }
#endif
}