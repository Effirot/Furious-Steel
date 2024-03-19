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







#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class RoomManager : NetworkBehaviour
{

    
    public struct SpawnArguments : INetworkSerializable
    {
        public static SpawnArguments This; 

        public enum CharacterColorScheme : byte
        {
            Red,
            Green,
            Blue,
            Black,
            White,
            Orange,
            Purple,
            Pink,
            Cyan,

            Celestial,
        } 

        public CharacterColorScheme ColorScheme;
        public FixedString128Bytes WeaponName;
        public FixedString128Bytes CharacterName;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ColorScheme);
            serializer.SerializeValue(ref WeaponName);
            serializer.SerializeValue(ref CharacterName);
        }

        public override bool Equals(object obj)
        {
            if (obj is not SpawnArguments)
                return false;

            var stats = (SpawnArguments)obj;

            return 
                stats.ColorScheme == ColorScheme &&
                stats.WeaponName == WeaponName &&
                stats.CharacterName == CharacterName;
        }
        public override int GetHashCode()
        {
            return -1;
        }

        public Color GetColor()
        {
            return ColorScheme switch
            {
                CharacterColorScheme.Red => Color.red,
                CharacterColorScheme.Green => Color.green,
                CharacterColorScheme.Blue => Color.blue,
                CharacterColorScheme.Black => Color.black,
                CharacterColorScheme.White => Color.white,
                CharacterColorScheme.Orange => new Color(1, 1, 0),
                CharacterColorScheme.Purple => new Color(1, 0, 1) ,
                CharacterColorScheme.Pink => new Color(1, 0.5f, 1) ,
                CharacterColorScheme.Cyan => Color.cyan,

                CharacterColorScheme.Celestial => new Color(1, 1, 0.55f),

                _ => Color.red
            };
        }
        public Color GetSecondColor()
        {
            return ColorScheme switch
            {
                CharacterColorScheme.Red => Color.yellow,
                CharacterColorScheme.Green => Color.yellow,
                CharacterColorScheme.Blue => Color.cyan,
                CharacterColorScheme.Black => Color.black,
                CharacterColorScheme.White => Color.black,
                CharacterColorScheme.Orange => Color.white,
                CharacterColorScheme.Purple => Color.blue,
                CharacterColorScheme.Pink => Color.red,
                CharacterColorScheme.Cyan => Color.white,

                CharacterColorScheme.Celestial => new Color(1f, 0.75f, 0.56f),

                _ => Color.red
            };
        }
        private float Intensity(float value)
        {   
            return value;
        } 
    }
    public struct PlayerStatistics : INetworkSerializable
    {
        public static PlayerStatistics Empty() => new() 
        {
            KillStreak = 0,
            KillStreakTotal = 0,
        
            AssistsStreak = 0,
            AssistsStreakTotal = 0,
        
            Deads = 0,

            DeliveredDamage = 0,
            PowerUpsPicked = 0,
        };

        public int KillStreak;
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
                stats.KillStreak == KillStreak &&
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
            serializer.SerializeValue(ref KillStreak);
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

    [SerializeField, Range(1, 64)]
    private int CharactersLimit = 12;

    [SerializeField]
    private NetworkPrefabsList characters;
    [SerializeField]
    private NetworkPrefabsList weapons;


    public NetworkList<PublicClientData> playersData;

    [NonSerialized]
    public SpawnArguments spawnArgs;
    
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


    public void Spawn(SpawnArguments args)
    {
        spawnArgs = args;

        Spawn_ServerRpc(args);
    }
    public void SpawnWithRandomArgs()
    {
        Spawn(GetRandomArguments());
    }

    public void WriteToChat(FixedString512Bytes text)
    {
        SendChatMessage_ServerRpc(text);
    }

    public SpawnArguments GetRandomArguments()
    {
        return new()
        {
            ColorScheme = (CharacterColorScheme) UnityEngine.Random.Range(0, Enum.GetNames(typeof(CharacterColorScheme)).Length),
            CharacterName = characters.PrefabList[UnityEngine.Random.Range(0, characters.PrefabList.Count)].Prefab.name,
            WeaponName = weapons.PrefabList[UnityEngine.Random.Range(0, weapons.PrefabList.Count)].Prefab.name
        };
    }
    public int GetCharactersCount()
    {   
        int count = 0;

        foreach (var data in privatePlayersData)
        {
            if (data.Value.networkCharacter != null)
            {
                count++;
            }
        }

        return count;
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

    private PlayerNetworkCharacter SpawnCharacter (SpawnArguments args, ServerRpcParams Param)
    {
        var senderId = Param.Receive.SenderClientId;
        var client = privatePlayersData[senderId];
        
        if (client.networkCharacter != null && client.networkCharacter.IsSpawned)
            return null;
        
        var character = ResearchCharacterPrefab(args.CharacterName.Value);
        if (character == null)
            return null;      
        
        // Get spawn point
        var spawnPoint = SpawnPoint.GetSpawnPoint().transform;

        // Spawn character
        var characterGameObject = Instantiate(character, spawnPoint.position, spawnPoint.rotation);
    
        client.networkCharacter = characterGameObject.GetComponent<PlayerNetworkCharacter>();
        client.networkCharacter.NetworkObject.SpawnAsPlayerObject(senderId, true);

        return client.networkCharacter;
    }
    private void SetWeapon (SpawnArguments args, ServerRpcParams Param)
    {
        var senderId = Param.Receive.SenderClientId;
        var client = privatePlayersData[senderId];
        
        if (client.networkCharacter == null)
            return;

        var weapon = ResearchWeaponPrefab(args.WeaponName.Value);
        if (weapon == null)
            return;

        if (client.networkCharacterWeapon != null && client.networkCharacterWeapon.IsSpawned)
        {
            client.networkCharacterWeapon.Despawn();
            client.networkCharacterWeapon = null;
        }

        // Spawn weapon
        var weaponGameObject = Instantiate(weapon, Vector3.zero, Quaternion.identity);

        if (client.networkCharacter == null)
        {
            throw new InvalidOperationException();
        } 

        client.networkCharacterWeapon = weaponGameObject.GetComponent<NetworkObject>();

        client.networkCharacterWeapon.SpawnWithOwnership(senderId, true);
        weaponGameObject.transform.SetParent(client.networkCharacter.transform, false);
    }

    [ServerRpc (RequireOwnership = false)]
    private void Spawn_ServerRpc(SpawnArguments args, ServerRpcParams Param = default)
    {   
        var DataIndex = IndexOfPlayerData(a => a.ID == Param.Receive.SenderClientId);
        // var DataIndex = (int) Param.Receive.SenderClientId;
        if (DataIndex == -1)
            return;

        if (CharactersLimit <= GetCharactersCount())
            return;
        
        var data = playersData[DataIndex];
        data.spawnArguments = args;
        playersData[DataIndex] = data;

        var player = SpawnCharacter(args, Param);
        
        if (player != null)
        {
            SetWeapon(args, Param);

            player.RefreshColor();
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
