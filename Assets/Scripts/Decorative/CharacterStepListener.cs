using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.VFX;

public class CharacterStepListener : MonoBehaviour
{
    [SerializeField]
    private CinemachineImpulseSource impulseSource;
    
    [SerializeField]
    private VisualEffect effect;
    
    [SerializeField]
    new private AudioSource audio;

    private void Step()
    {
        if (audio != null)
        {
            audio.Play();
        }
        if (effect != null)
        {
            effect.Play();
        }
        if (impulseSource!= null)
        {
            impulseSource.GenerateImpulse();
        }
    }
}
