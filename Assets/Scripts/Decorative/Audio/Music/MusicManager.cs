using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Singleton { get; private set; } = null;

    public float stressLevel { 
        get => _stressLevel;
        set {
            _stressLevel = Mathf.Clamp01(value);
        }
    }

    private float _stressLevel = 0;

    [SerializeField]
    private MusicManagerAsset defaultAsset;

    public MusicManagerAsset asset { 
        get => _asset; 
        set 
        {
            _asset = value;
        }
    }

    private MusicManagerAsset _asset;

    private void Awake()
    {
        Singleton = this;
    }
    private void Start()
    {
        if (asset == null)
        {
            asset = defaultAsset;
        }
    }
}
