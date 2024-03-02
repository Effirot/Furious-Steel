using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class BindChargeToLightning : MonoBehaviour
{
    [SerializeField]
    private Vector3 direction; 

    [SerializeField]
    private float size; 

    private VisualEffect visualEffect;

    public void SetValue(float value)
    {
        visualEffect.SetVector3("Direction", direction * value);
        visualEffect.SetFloat("Width", size * value);
    }
    
    public void Play(DamageDeliveryReport damageDeliveryReport)
    {
        if (damageDeliveryReport.target != null)
        {
            visualEffect.SetVector3("Direction", damageDeliveryReport.target.transform.position);
        }

        visualEffect.Play();
    }

    private void Awake()
    {
        visualEffect = GetComponent<VisualEffect>();
    }

}
