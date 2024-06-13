using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
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
                    lockedFollowPosition = followPosition.Value;
                    break;
            
                case LockState.LockOnTarget:
                    lockedTarget = target;
                    break;
            }
        }
    }

    [NonSerialized]
    public NetworkVariable<Vector3> followPosition = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Transform target;

    private Vector3 lockedFollowPosition;
    private Transform lockedTarget;

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
        if (IsServer)
        {
            if (target != null)
            {
                followPosition.Value = followPoint.InverseTransformPoint(target.transform.position + (transform.rotation * AdditivePosition));
            }    
            else 
            {
                followPosition.Value = Vector3.zero;
            }
        }
        
    }
    private void LateUpdate()
    {
        followPoint.localPosition = Vector3.Lerp(followPoint.localPosition, followPosition.Value, 25f * Time.deltaTime);
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