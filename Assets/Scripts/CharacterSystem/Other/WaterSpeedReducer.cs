using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkCharacter))]
public class WaterSpeedReducer : MonoBehaviour
{
    [SerializeField, Range(0, 20f)]
    private float speedReducing = 4f;


    public bool isInWater { 
        get => m_isInWater;
        set 
        {   
            if (m_isInWater != value)
            {
                if (value)
                {
                    character.CurrentSpeed -= speedReducing;
                }
                else
                {
                    character.CurrentSpeed += speedReducing;
                }
            }
            
            m_isInWater = value;
        }
    }
    
    private bool m_isInWater = false; 
    private NetworkCharacter character;
    
    private void Awake()
    {
        character = GetComponent<NetworkCharacter>();
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
