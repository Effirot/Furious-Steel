using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioSourceRandomize : MonoBehaviour
{
    [SerializeField, Range(0, 3)]
    private float MaxPitch = 1.2f;

    [SerializeField, Range(0, 3)]
    private float MinPitch = 0.8f;

    [SerializeField, Range(0, 3)]
    private float MaxVolume = 1.2f;

    [SerializeField, Range(0, 3)]
    private float MinVolume = 0.8f;


    public void PlayRandom()
    {
        Randomize();

        var sources = GetComponents<AudioSource>();
        sources[Random.Range(0, sources.Length - 1)].Play();
    }

    public void Randomize()
    {
        var sources = GetComponents<AudioSource>();

        foreach (var source in sources)
        {
            source.pitch = Random.Range(MinPitch, MaxPitch);
            source.volume = Random.Range(MinVolume, MaxVolume);
        }
    }

    private void Awake()
    {
        Randomize();
    }
}
