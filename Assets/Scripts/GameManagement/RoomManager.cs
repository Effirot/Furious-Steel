using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Mirror;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using Effiry.Items;
using UnityEngine.SceneManagement;
using CharacterSystem.Objects;

using static ConnectedPlayerData;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class RoomManager : NetworkManager
{
    public struct SetTimescaleMessage : NetworkMessage
    {
        public float NewValue;
    }

    public static RoomManager Singleton { get; private set; }

    [Space]
    public GameObject playerDataObjectPrfab; 
    
    [Space]
    [SerializeField]
    private List<GameObject> characters;
    [SerializeField]
    private List<GameObject> weapons;
    [SerializeField]
    private List<GameObject> trinkets;
    
    private Dictionary<NetworkConnection, ConnectedPlayerData> connectionData = new();


    public GameObject ResearchCharacterPrefab(string name)
    {
        foreach (var item in characters)
        {
            if (item.name == name)
            {
                return item;
            }
        }

        return null;
    }
    public GameObject ResearchWeaponPrefab(string name)
    {
        foreach (var item in weapons)
        {
            if (item.name == name)
            {
                return item;
            }
        }

        return null;
    }
    public GameObject ResearchTrinketPrefab(string name)
    {
        foreach (var item in trinkets)
        {
            if (item.name == name)
            {
                return item;
            }
        }

        return null;
    }


    public void SetTimescale(float newValue)
    {
        if (!NetworkServer.active)
            throw new Exception();

        Time.timeScale = newValue;

        var timescaleMessage = new SetTimescaleMessage()
        {
            NewValue = newValue
        }; 

        NetworkServer.SendToAll(timescaleMessage);
    }
    public void SetTimescale(float newValue, NetworkConnectionToClient networkConnectionTo)
    {
        if (!NetworkServer.active)
            throw new Exception();

        var timescaleMessage = new SetTimescaleMessage()
        {
            NewValue = newValue
        }; 

        Time.timeScale = newValue;

        networkConnectionTo.Send(timescaleMessage);
    }

    
    public void SetGameRuleConfig(GameRules gameRules)
    {
        if (!NetworkServer.active)
            throw new Exception();

        var syncGameRulesMessage = new SyncGameRulesMessage()
        {
            data = gameRules
        };

        NetworkServer.SendToAll(syncGameRulesMessage);
    }
    public void SetGameRuleConfig(GameRules gameRules, NetworkConnectionToClient networkConnectionTo)
    {
        if (!NetworkServer.active)
            throw new Exception();

        var syncGameRulesMessage = new SyncGameRulesMessage()
        {
            data = gameRules
        };

        networkConnectionTo.Send(syncGameRulesMessage);
    }


    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<SetTimescaleMessage>(OnSetTimescale_Handler);
        NetworkClient.RegisterHandler<SyncGameRulesMessage>(GameRules.SetAsCurrent);
    }
    public override void OnStartServer()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            SceneManager.LoadScene(1); 
        }
    }

    public override async void OnClientConnect()
    {
        base.OnClientConnect();

        await UniTask.WaitForSeconds(5);

        NetworkClient.PrepareToSpawnSceneObjects();
    }
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        if (SceneManager.GetActiveScene().buildIndex != 0)
        {
            SceneManager.LoadScene(0); 
        }

        connectionData.Clear();

        Time.timeScale = 0;
        GameRules.Reset();
    }

    public override async void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        Debug.Log("Someone is trying to connect . . . ");
        
        if (connectionData.ContainsKey(conn))
            return;
        
        await UniTask.WaitUntil(() => conn.isReady);
        
        AddPlayerData(conn);
        SetGameRuleConfig(GameRules.current, conn);

        if (Time.timeScale != 1)
        {
            SetTimescale(Time.timeScale, conn);
        }
    }
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        if (connectionData.ContainsKey(conn))
        {
            var data = connectionData[conn];

            NetworkServer.Destroy(data.gameObject);
            
            Debug.Log($"{data.Name} is disconnected");
            connectionData.Remove(conn);
        }

        NetworkServer.DestroyPlayerForConnection(conn);
    }

    public override void Awake()
    {
        base.Awake();

        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        Singleton = this;
    }
    public override void OnValidate()
    {
        base.OnValidate();

        Singleton = this;
    }

    private void OnSetTimescale_Handler(SetTimescaleMessage setTimescale)
    {
        Time.timeScale = setTimescale.NewValue;
    }

    private void AddPlayerData(NetworkConnectionToClient conn)
    {
        var playerDataObject = Instantiate(playerDataObjectPrfab);
        
        NetworkServer.Spawn(playerDataObject, conn);
        playerDataObject.GetComponent<NetworkIdentity>().AssignClientAuthority(conn);

        var playerData = playerDataObject.GetComponent<ConnectedPlayerData>();
        
        playerData.Name = conn.connectionId.ToString();

        Debug.Log($"{playerData.Name} is connected");
        
        connectionData.Add(conn, playerData);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        networkSceneName = onlineScene = scene.name;

        if (NetworkClient.active && !NetworkServer.active)
        {
            NetworkClient.PrepareToSpawnSceneObjects();
        }
    }
} 
