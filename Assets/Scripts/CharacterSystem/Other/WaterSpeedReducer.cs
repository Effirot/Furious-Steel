using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(IPhysicObject))]
public class WaterSpeedReducer : MonoBehaviour
{
    [SerializeField, Range(-1, 1)]
    private float physicsScaleReducing = 0.5f;

    [SerializeField, Range(-1, 1)]
    private float GravityScale = 0.5f;

    public bool isInWater { 
        get => m_isInWater;
        set 
        {   
            if (m_isInWater != value)
            {
                if (value)
                {
                    physicObject.PhysicTimeScale -= physicsScaleReducing;
                    physicObject.GravityScale -= GravityScale;
                }
                else
                {
                    physicObject.PhysicTimeScale += physicsScaleReducing;
                    physicObject.GravityScale += GravityScale;
                }
            }
            
            m_isInWater = value;
        }
    }
    
    private bool m_isInWater = false; 
    private IPhysicObject physicObject;
    
    private void Awake()
    {
        physicObject = GetComponent<IPhysicObject>();
    }
    private void OnTriggerEnter (Collider collider)
    {
        if (collider.gameObject.layer == 4)
        {
            isInWater = true;
        }
    }
    private void OnTriggerExit (Collider collider)
    {
        if (collider.gameObject.layer == 4)
        {
            isInWater = false;
        }
    }
}
