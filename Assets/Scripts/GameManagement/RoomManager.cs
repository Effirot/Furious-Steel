using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using static RoomManager.SpawnArguments;
using System.Net.Sockets;
using static Authorizer;
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
    public struct SpawnArguments : INetworkSerializable
    {
        public static SpawnArguments Local; 

        public FixedString512Bytes CharacterItemJson;
        public FixedString512Bytes WeaponItemJson;
        public FixedString512Bytes TrinketItemJson;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref WeaponItemJson);
            serializer.SerializeValue(ref CharacterItemJson);
            serializer.SerializeValue(ref TrinketItemJson);
        }

        public override bool Equals(object obj)
        {
            if (obj is not SpawnArguments)
                return false;

            var stats = (SpawnArguments)obj;

            return 
                stats.WeaponItemJson == WeaponItemJson &&
                stats.CharacterItemJson == CharacterItemJson;
        }
        public override int GetHashCode()
        {
            return -1;
        }
    }
    public struct PlayerStatistics : INetworkSerializable
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
        };

        public int Points;

        public int KillStreakTotal;
        
        public int AssistsStreak;
        public int AssistsStreakTotal;
        
        public int Deads;

        public float DeliveredDamage;
        public float PowerUpsPicked;

        public override bool Equals(object obj)
        {
            if (obj is not PlayerStatistics)
                return false;

            var stats = (PlayerStatistics)obj;

            return 
                stats.Points == Points &&
                stats.KillStreakTotal == KillStreakTotal &&
                stats.AssistsStreak == AssistsStreak &&
                stats.AssistsStreakTotal == AssistsStreakTotal &&
                stats.Deads == Deads &&
                stats.DeliveredDamage == DeliveredDamage &&
                stats.PowerUpsPicked == PowerUpsPicked;
        }
        public override int GetHashCode()
        {
            return -1;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Points);
            serializer.SerializeValue(ref KillStreakTotal);
            serializer.SerializeValue(ref AssistsStreak);
            serializer.SerializeValue(ref AssistsStreakTotal);
            serializer.SerializeValue(ref Deads);
            serializer.SerializeValue(ref DeliveredDamage);
            serializer.SerializeValue(ref PowerUpsPicked);
        }
    }

    public struct PublicClientData : INetworkSerializable, IEquatable<PublicClientData>
    {
        public ulong ID;

        public FixedString64Bytes Name;
        
        public short Ping;

        public SpawnArguments spawnArguments;
        public PlayerStatistics statistics;

        public DateTime EnterTime;
        public DateTime DeathTime;
        public DateTime RespawnTime;

        public bool Equals(PublicClientData other)
        {
            return 
                other.ID.Equals(ID) &&
                other.Name.Equals(Name) &&
                other.statistics.Equals(statistics) &&
                other.spawnArguments.Equals(spawnArguments);
        }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ID);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Ping);

            serializer.SerializeValue(ref EnterTime);
            serializer.SerializeValue(ref DeathTime);
            serializer.SerializeValue(ref RespawnTime);

            serializer.SerializeValue(ref spawnArguments);
            serializer.SerializeValue(ref statistics);
        }
    }
    private class PrivateClientInfo : IDisposable
    {
        public NetworkClient networkClient;

        public PlayerNetworkCharacter networkCharacter;
        public NetworkObject networkCharacterWeapon;
        public NetworkObject networkCharacterTrinket;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public delegate void SendChatMessageDelegate (PublicClientData publicClientData, FixedString512Bytes text);

    public static RoomManager Singleton { get; private set; }


    public static event SendChatMessageDelegate OnWriteToChat = delegate { };
    public static event SendChatMessageDelegate OnServerException = delegate { };

    public static readonly PublicClientData ServerData = new PublicClientData() { ID = 0, Name = "[SERVER]" };

    [SerializeField]
    private NetworkPrefabsList characters;
    [SerializeField]
    private NetworkPrefabsList weapons;
    [SerializeField]
    private NetworkPrefabsList trinkets;

    [NonSerialized]
    public SpawnArguments spawnArgs;
    public NetworkList<PublicClientData> playersData;

    private Dictionary<ulong, PrivateClientInfo> privatePlayersData = new ();


    public bool TryFindClientData(ulong ClientID, out PublicClientData publicClientData)
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


    public void Spawn(SpawnArguments args)
    {
        spawnArgs = args;

        Spawn_ServerRpc(args);
    }

    public void WriteToChat(FixedString512Bytes text)
    {
        SendChatMessage_ServerRpc(text);
    }

    public GameObject ResearchCharacterPrefab(string name)
    {
        foreach (var item in characters.PrefabList)
        {
            if (item.Prefab.name == name)
            {
                return item.Prefab;
            }
        }

        return null;
    }
    public GameObject ResearchWeaponPrefab(string name)
    {
        foreach (var item in weapons.PrefabList)
        {
            if (item.Prefab.name == name)
            {
                return item.Prefab;
            }
        }

        return weapons.PrefabList[0].Prefab;
    }
    public GameObject ResearchTrinketPrefab(string name)
    {
        foreach (var item in trinkets.PrefabList)
        {
            if (item.Prefab.name == name)
            {
                return item.Prefab;
            }
        }

        return null;
    }

    public async void OnPlayerAuthorized(ulong ID, Authorizer.AuthorizeArguments authorizedPlayerData)
    {
        privatePlayersData.Add(ID, new ()
        {
            networkCharacter = null,
            networkCharacterWeapon = null,
        });

        await UniTask.WaitUntil(() => NetworkManager.IsListening); 
        
        if (ID != 0)
        {
            await UniTask.WaitForSeconds(0.3f); 
        }
        
        playersData.Add(new()
        {
            ID = ID,

            Name = authorizedPlayerData.Name,

            EnterTime = DateTime.Now,
            DeathTime = DateTime.Now,
            RespawnTime = DateTime.Now,

            statistics = PlayerStatistics.Empty()
        });
    }
    public void OnAuthorizedPlayerDisconnected(ulong ID)
    {
        var publicDataIndex = IndexOfPlayerData(data => data.ID == ID);
        
        // if (privateClientsData[ID].networkCharacter != null)
        // {
        //     privateClientsData[ID].networkCharacter.Kill();
        // }

        playersData?.RemoveAt(publicDataIndex);
        
        if (privatePlayersData.ContainsKey(ID))
        {
            privatePlayersData[ID].Dispose();
            privatePlayersData.Remove(ID);
        }
    }


    private void Awake()
    {
        Singleton = this;

        playersData = new (new PublicClientData[0], NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    }
    private void OnValidate()
    {
        Singleton = this;
    }


    private PlayerNetworkCharacter SpawnCharacter (Item item, ServerRpcParams Param)
    {
        if (item == null)
            return null;

            var character = SpawnCharacter(item.TypeName, Param);
        
        if (character.TryGetComponent<ItemBinder>(out var component))
        {
            component.item = item;
        }    

        return character;
    }
    private PlayerNetworkCharacter SpawnCharacter (string name, ServerRpcParams Param)
    {
        var senderId = Param.Receive.SenderClientId;
        var client = privatePlayersData[senderId];
        
        
        var character = ResearchCharacterPrefab(name);

        if (client.networkCharacter != null && client.networkCharacter.IsSpawned || character == null)
            return null;
        
        // Get spawn point
        var spawnPoint = SpawnPoint.GetSpawnPoint().transform;

        // Spawn character
        var characterGameObject = Instantiate(character, spawnPoint.position, spawnPoint.rotation);
    
        client.networkCharacter = characterGameObject.GetComponent<PlayerNetworkCharacter>();
        client.networkCharacter.NetworkObject.SpawnAsPlayerObject(senderId, true);

        return client.networkCharacter;
    }
   
    private NetworkObject SetWeapon (Item item, ServerRpcParams Param)
    {
        if (item == null)
            return null;

        var weapon = SetWeapon (item.TypeName, Param);

        if (weapon.TryGetComponent<ItemBinder>(out var component))
        {
            component.item = item;
        }    

        return weapon;
    }
    private NetworkObject SetWeapon (string WeaponName, ServerRpcParams Param)
    {
        var senderId = Param.Receive.SenderClientId;
        var client = privatePlayersData[senderId];
        var weapon = ResearchWeaponPrefab(WeaponName);

        if (client.networkCharacter == null || weapon == null)
            return null;

        if (client.networkCharacterWeapon != null)
        {
            Destroy(client.networkCharacterWeapon);
        }

        // Spawn weapon
        var weaponGameObject = Instantiate(weapon, Vector3.zero, Quaternion.identity);
        var weaponNetObject = client.networkCharacterWeapon = weaponGameObject.GetComponent<NetworkObject>();

        weaponNetObject.SpawnWithOwnership(senderId, true);
        weaponGameObject.transform.SetParent(client.networkCharacter.transform, false);

        client.networkCharacter.OnWeaponChanged(weaponNetObject);

        return weaponNetObject;
    }
   
    private NetworkObject SetTrinket (Item item, ServerRpcParams Param)
    {
        if (item == null)
            return null;

        var trinket = SetTrinket(item.TypeName, Param);

        if (trinket.TryGetComponent<ItemBinder>(out var component))
        {
            component.item = item;
        } 

        return trinket;
    }
    private NetworkObject SetTrinket (string TrinketName, ServerRpcParams Param)
    {
        var senderId = Param.Receive.SenderClientId;
        var client = privatePlayersData[senderId];
        var trinket = ResearchTrinketPrefab(TrinketName);

        if (client.networkCharacter == null || trinket == null)
            return null;
        
        if (client.networkCharacterTrinket != null)
        {
            Destroy(client.networkCharacterTrinket);
        }

        // Spawn trinket
        var trinketGameObject = Instantiate(trinket, Vector3.zero, Quaternion.identity);
        var trinketNetObject = client.networkCharacterTrinket = trinketGameObject.GetComponent<NetworkObject>();

        trinketNetObject.SpawnWithOwnership(senderId, true);
        trinketGameObject.transform.SetParent(client.networkCharacter.transform, false);

        client.networkCharacter.OnTrinketChanged(trinketNetObject);

        return trinketNetObject;
    }


    [ServerRpc (RequireOwnership = false)]
    private void Spawn_ServerRpc(SpawnArguments args, ServerRpcParams Param = default)
    {   
        var DataIndex = IndexOfPlayerData(a => a.ID == Param.Receive.SenderClientId);
        if (DataIndex == -1)
            return;
        
        var data = playersData[DataIndex];
        data.spawnArguments = args;
        playersData[DataIndex] = data;

        if (!SpawnCharacter(JsonToItem(args.CharacterItemJson.Value), Param).IsUnityNull())
        {
            try {
                SetTrinket(JsonToItem(args.TrinketItemJson.Value), Param);
            }
            catch { }

            try {
                SetWeapon(JsonToItem(args.WeaponItemJson.Value), Param);
            }
            catch { }
        }
    }
    [ClientRpc]
    private void ServerException_ClientRpc(FixedString512Bytes message, ClientRpcParams rpcParams = default)
    {
        OnServerException.Invoke(ServerData, message);
    } 

    [ServerRpc (RequireOwnership = false)]
    private void SendChatMessage_ServerRpc(FixedString512Bytes text, ServerRpcParams Param = default)
    {
        SendChatMessage_ClientRpc(Param.Receive.SenderClientId, text);
        
        if (TryFindClientData(Param.Receive.SenderClientId, out var data))
        {
            Debug.Log($"Message: {data.Name} - {text}");
        }
    }
    [ClientRpc]
    private void SendChatMessage_ClientRpc(ulong clientID, FixedString512Bytes text)
    {
        if (TryFindClientData(clientID, out var data))
        {
            OnWriteToChat.Invoke(data, text);

            Debug.Log($"Message: {data.Name} - {text}");
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
