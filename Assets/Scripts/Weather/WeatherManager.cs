

using System;
using Unity.Netcode;
using UnityEngine;
using NVRP = Unity.Netcode.NetworkVariableReadPermission;
using NVWP = Unity.Netcode.NetworkVariableWritePermission;

public class WeatherManager : NetworkBehaviour
{
    public static WeatherManager Singleton { get; private set; }

    public bool LaternsEnabled { 
        get => LaternsEnabled_network.Value;
        set => LaternsEnabled_network.Value = value;
    }

    public bool RainEnabled { 
        get => RainEnabled_network.Value;
        set => RainEnabled_network.Value = value;
    }

    public DateTime TimeEnabled { 
        get => Time_network.Value;
    }

    [SerializeField]
    private NetworkVariable<DateTime> Time_network = new(DateTime.Now, NVRP.Everyone, NVWP.Server); 
    [SerializeField]
    private NetworkVariable<bool> LaternsEnabled_network = new(false, NVRP.Everyone, NVWP.Server); 
    [SerializeField]
    private NetworkVariable<bool> RainEnabled_network = new(false, NVRP.Everyone, NVWP.Server); 

    private void Awake()
    {
        Singleton = this;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        Singleton = null;
    }
} 