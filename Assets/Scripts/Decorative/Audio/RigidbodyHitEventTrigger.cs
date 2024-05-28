using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyHitEventTrigger : MonoBehaviour
{
    [SerializeField, Range(0, 100)]
    private float HitMagnitude = 2;

    [SerializeField]
    private UnityEvent<Collision> Event = new();
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.impulse.magnitude >= HitMagnitude)
        {
            Event.Invoke(collision);
        }
    }
}
