using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BlockPitchIncrieser : MonoBehaviour
{
    [SerializeField, Range(0.5f, 2)]
    private float MinPicth = 0.7f;

    [SerializeField, Range(0.5f, 2)]
    private float MaxPicth = 1.25f;

    [SerializeField, Range(0.1f, 2)]
    private float PitchStep = 0.15f;

    [SerializeField, Range(0.03f, 1)]
    private float PitchReducingPerSecond = 0.03f;
    
    private AudioSource audioSource;

    public void Add()
    {
        audioSource.pitch = Mathf.Clamp(audioSource.pitch + PitchStep, MinPicth, MaxPicth);
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

#if UNITY_SERVER
    private void Update()
    {
        audioSource.pitch = Mathf.Clamp(audioSource.pitch - PitchReducingPerSecond * Time.deltaTime, MinPicth, MaxPicth);
    }
#endif
}
