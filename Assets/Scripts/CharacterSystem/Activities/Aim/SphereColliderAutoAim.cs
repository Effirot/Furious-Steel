using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Mirror;
using Unity.VisualScripting;
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
    private LayerMask layer;


    [SerializeField]
    private LockState _lockState = LockState.None;


    public LockState lockState
    {
        get => _lockState;
        set {
            _lockState = value;

            switch (value)
            {
                case LockState.LockOnPosition:
                    lockedFollowPosition = followPosition;
                    break;
            
                case LockState.LockOnTarget:
                    lockedTarget = target;
                    break;
            }
        }
    }

    [NonSerialized, SyncVar]
    public Vector3 followPosition = Vector3.zero;

    private Transform target;

    private Vector3 lockedFollowPosition;
    private Transform lockedTarget;

    public void SetLockState(int lockStateIndex)
    {
        lockState = (LockState)lockStateIndex;
    }

    private void Start()
    {
        StartCoroutine(CheckTargets());
    }
    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void FixedUpdate()
    {    
        if (isServer)
        {
            if (target != null)
            {
                followPosition = followPoint.InverseTransformPoint(target.transform.position + (transform.rotation * AdditivePosition));
            }    
            else 
            {
                followPosition = Vector3.zero;
            }
        }
        
    }
    private void LateUpdate()
    {
        followPoint.localPosition = Vector3.Lerp(followPoint.localPosition, followPosition, 25f * Time.deltaTime);
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
            var colliders = Physics.OverlapSphere(transform.position + (transform.rotation * SearchSpherePoint), SearchRadius, layer)
                .Where(collider => collider != null && collider.gameObject.TryGetComponent<IDamagable>(out _));
            
            Collider targetCollider = null;
            float minDistance = float.MaxValue;

            foreach (var collider in colliders) {
                
                var distance = Vector3.Distance(collider.transform.position, transform.position);
                if (distance < minDistance)
                {
                    targetCollider = collider;
                    minDistance = distance;
                } 
            }

            if (targetCollider != null)
            {
                target = targetCollider.transform;
            }
            else
            {
                target = null;
            }

            yield return wait;
        }
    } 
}