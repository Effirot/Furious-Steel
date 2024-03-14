using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidbodyPusher : MonoBehaviour
{
    [SerializeField, Range(0, 20)]
    private float Range = 1;

    [SerializeField, Range(0, 20)]
    private float Force = 1;

    [SerializeField]
    private LayerMask layerMask;

    private void Start()
    {
        foreach (var item in Physics.OverlapSphere(transform.position, Range, layerMask))
        {
            item?.attachedRigidbody?.AddExplosionForce(Force * 100, transform.position, Range);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, Range);
    }

}
