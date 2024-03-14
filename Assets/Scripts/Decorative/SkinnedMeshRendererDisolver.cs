using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class SkinnedMeshRendererDisolver : MonoBehaviour
{
    [SerializeField]
    private float value;

    SkinnedMeshRenderer skinnedMeshRenderer;

    private void Start()
    {
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
    }

    private void Update()
    {
        var color = skinnedMeshRenderer.material.color; 
        
        color.a -= value * Time.deltaTime;
    
        skinnedMeshRenderer.material.color = color;
    }
}