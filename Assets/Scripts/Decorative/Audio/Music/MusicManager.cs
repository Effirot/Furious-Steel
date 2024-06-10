using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
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

    private void Start()
    {
        if (asset == null)
        {
            asset = defaultAsset;
        }
    }
}
