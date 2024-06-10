

using UnityEngine;

[CreateAssetMenu(fileName = "MusicManagerAsset", menuName = "MusicAsset", order = 0)]
public class MusicManagerAsset : ScriptableObject
{
    public AudioClip[] audioClips;

    public MusicManagerAssetValuePerStress[] VolumeValues;
}

public class MusicManagerAssetValuePerStress
{
    public float StressValue = 1;

    public float[] VolumeValues = new float[0];
}