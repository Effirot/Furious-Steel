using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class SphereColliderAutoAim : NetworkBehaviour
{
    public enum LockState
    {
        None,
        LockOnTarget,
        LockOnPosition,
    }

    [SerializeField]
    private float SearchRadius;

    [SerializeField]
    private Vector3 SearchSpherePoint;

    [SerializeField]
    private Vector3 AdditivePosition;

    [SerializeField]
    private Transform followPoint;

    [SerializeField]
    private LockState lockState = LockState.None;

    private IEnumerable<Collider> colliders;

    public void SetLockState(int lockStateIndex)
    {
        lockState = (LockState)lockStateIndex;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        StartCoroutine(CheckTargets());
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        StopAllCoroutines();
    }

    private void FixedUpdate()
    {
        Collider targetCollider = null;
        float minDistance = 1000000;

        foreach (var item in colliders)
        {
            var distance = Vector3.Distance(item.transform.position, transform.position);
            if (distance < minDistance)
            {
                targetCollider = item;
                minDistance = distance;
            } 
        }

        if (targetCollider != null)
        {
            followPoint.position = targetCollider.transform.position + (transform.rotation * AdditivePosition);
        }    
        else 
        {
            followPoint.localPosition = Vector3.zero;
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireSphere(SearchSpherePoint, SearchRadius);
    }

    private IEnumerator CheckTargets()
    {
        var wait = new WaitForSeconds(0.1f);

        while (true)
        {
            colliders = Physics.OverlapSphere(transform.position + (transform.rotation * SearchSpherePoint), SearchRadius)
                .Where(collider => collider != null && collider.gameObject.TryGetComponent<IDamagable>(out _));

            yield return wait;
        }
    } 

}