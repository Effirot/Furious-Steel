using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField]
    private Vector3 pushDirection;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IPhysicObject>(out var IPhysicObject))
        {
            IPhysicObject.Push(pushDirection);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(transform.position, pushDirection * 2);
    }
}
