using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

public class CharacterStepListener : MonoBehaviour
{
    [SerializeField]
    private UnityEvent OnStepEvent = new();

    private void Step()
    {
        OnStepEvent.Invoke();
    }
}
