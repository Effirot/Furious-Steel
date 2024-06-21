using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using static RoomManager.SpawnArguments;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using System.Linq;
using Unity.VisualScripting;
using Effiry.Items;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class RoomManager : NetworkBehaviour
{
    public struct SpawnArguments
    {
        public static SpawnArguments Local; 

        public string CharacterItemJson;
        public string WeaponItemJson;
        public string TrinketItemJson;
    }
    public struct PlayerStatistics
    {
        public static PlayerStatistics Empty() => new() 
        {
            Points = 0,
        
            KillStreakTotal = 0,
        
            AssistsStreak = 0,
            AssistsStreakTotal = 0,
        
            Deads = 0,

            DeliveredDamage = 0,
            PowerUpsPicked = 0,

            EnterTime = DateTime.Now,
            DeathTime = DateTime.Now,
            RespawnTime = DateTime.Now,
        };

        public DateTime EnterTime;
        public DateTime DeathTime;
        public DateTime RespawnTime;

        public int Points;

        public int KillStreakTotal;
        
        public int AssistsStreak;
        public int AssistsStreakTotal;
        
        public int Deads;

        public float DeliveredDamage;
        public float PowerUpsPicked;
    }

    public struct PublicClientData
    {
        public int ID;

        public FixedString64Bytes Name;
        
        public short Ping;

        public SpawnArguments spawnArguments;
        public PlayerStatistics statistics;
    }

    public delegate void SendChatMessageDelegate (PublicClientData publicClientData, FixedString512Bytes text);

    public static RoomManager Singleton { get; private set; }


    public static event SendChatMessageDelegate OnWriteToChat = delegate { };
    public static event SendChatMessageDelegate OnServerException = delegate { };

    public static readonly PublicClientData ServerData = new PublicClientData() { ID = 0, Name = "[SERVER]" };

    [SerializeField]
    private List<GameObject> characters;
    [SerializeField]
    private List<GameObject> weapons;
    [SerializeField]
    private List<GameObject> trinkets;

    [NonSerialized]
    public SpawnArguments spawnArgs;

    public SyncList<PublicClientData> playersData = new (new PublicClientData[0]);


    public bool TryFindClientData(int ClientID, out PublicClientData publicClientData)
    {
        publicClientData = new();

        foreach (var item in playersData)
        {
            if (item.ID == ClientID)
            {
                publicClientData = item;

                return true;
            }
        }

        return false;
    }
    public int IndexOfPlayerData(Predicate<PublicClientData> alghoritm)
    {
        for (int i = 0; i < playersData.Count; i++)
        {
            var data = playersData[i];

            if (alghoritm.Invoke(data))
            {
                return i;
            }
        }

        return -1;
    }
    public Item JsonToItem (string json)
    {
        if (json == null || json.Length <= 0)
            return null;

        return Item.FromJsonString(json);
    }

    public async void Spawn(SpawnArguments args)
    {
        spawnArgs = args;

        await UniTask.WaitUntil(() => NetworkClient.ready);

        Spawn_Command(args);
    }

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

#warning Player Authorization
    // public async void OnPlayerAuthorized(int ID, Authorizer.AuthorizeArguments authorizedPlayerData)
    // {
    //     await UniTask.WaitUntil(() => NetworkManager.singleton.isNetworkActive); 
        
    //     if (ID != 0)
    //     {
    //         await UniTask.WaitForSeconds(0.3f); 
    //     }
        
    //     playersData.Add(new()
    //     {
    //         ID = ID,

    //         Name = authorizedPlayerData.Name,

    //         statistics = PlayerStatistics.Empty()
    //     });
    // }
    // public void OnAuthorizedPlayerDisconnected(int ID)
    // {
    //     var publicDataIndex = IndexOfPlayerData(data => data.ID == ID);

    //     if (playersData != null)
    //     {
    //         playersData?.RemoveAt(publicDataIndex);
    //     }
    // }

    private void Awake()
    {
        Singleton = this;
    }
    protected override void OnValidate()
    {
        base.OnValidate();

        Singleton = this;
    }


    private PlayerNetworkCharacter SpawnCharacter (Item item, NetworkConnectionToClient networkConnection)
    {
        if (item == null)
            return null;

        return SpawnCharacter(item.TypeName, networkConnection);
    }
    private PlayerNetworkCharacter SpawnCharacter (string name, NetworkConnectionToClient networkConnection)
    {   
        var character = ResearchCharacterPrefab(name);
        
        // Get spawn point
        var spawnPoint = SpawnPoint.GetSpawnPoint().transform;

        // Spawn character
        var characterGameObject = Instantiate(character, spawnPoint.position, spawnPoint.rotation);
        
        NetworkServer.Spawn(characterGameObject, networkConnection);
        NetworkServer.AddPlayerForConnection(networkConnection, characterGameObject);
        characterGameObject.GetComponent<NetworkIdentity>().AssignClientAuthority(networkConnection);

        return characterGameObject.GetComponent<PlayerNetworkCharacter>();
    }
   

    [Client, Command(requiresAuthority = false)]
    private void Spawn_Command(SpawnArguments args, NetworkConnectionToClient sender = null)
    {   
        // var DataIndex = IndexOfPlayerData(a => a.ID == sender.connectionId);
        // if (DataIndex == -1)
        //     return;
        
        // var data = playersData[DataIndex];
        // data.spawnArguments = args;
        // playersData[DataIndex] = data;

        var character = SpawnCharacter(JsonToItem(args.CharacterItemJson), sender); 
        
        if (!character.IsUnityNull())
        {
            try {
                character.SetTrinket(JsonToItem(args.TrinketItemJson));
            }
            catch (Exception e) 
            {
                Debug.LogException(e);
            }

            try {
                character.SetWeapon(JsonToItem(args.WeaponItemJson), sender);
            }
            catch (Exception e) 
            {
                Debug.LogException(e);
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(RoomManager))]
    public class RoomManager_Editor : Editor
    {
        public new RoomManager target => base.target as RoomManager;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (target.playersData != null)
            {
                foreach (var player in target.playersData)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField(player.ID.ToString());
                    EditorGUILayout.LabelField(player.Name.Value);

                    EditorGUILayout.EndHorizontal();

                }
            }

        }
    }
#endif
}
