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



#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class RoomManager : NetworkBehaviour
{
    public struct AuthorizeArguments : INetworkSerializable
    {
        public static AuthorizeArguments This; 

        public FixedString128Bytes Name;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Name);
        }
    }
    
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
        public int WeaponIndex;
        public int CharacterIndex;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ColorScheme);
            serializer.SerializeValue(ref WeaponIndex);
            serializer.SerializeValue(ref CharacterIndex);
        }

        public override bool Equals(object obj)
        {
            if (obj is not SpawnArguments)
                return false;

            var stats = (SpawnArguments)obj;

            return 
                stats.ColorScheme == ColorScheme &&
                stats.WeaponIndex == WeaponIndex &&
                stats.CharacterIndex == CharacterIndex;
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

        }
    }
    
    public struct PublicClientData : INetworkSerializable, IEquatable<PublicClientData>
    {
        public ulong ID;

        public FixedString128Bytes Name;

        public SpawnArguments spawnArguments;
        public PlayerStatistics statistics;

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
            serializer.SerializeValue(ref spawnArguments);
            serializer.SerializeValue(ref statistics);
        }
    }
    private class PrivateClientInfo : IDisposable
    {
        public NetworkClient networkClient;

        public PlayerNetworkCharacter networkCharacter;
        public NetworkObject networkCharacterWeapon;

        public DateTime EnterTime;
        public DateTime DeathTime;
        public DateTime RespawnTime;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public delegate void SendChatMessageDelegate (PublicClientData publicClientData, FixedString512Bytes text);

    public static RoomManager Singleton { get; private set; }

    public static int AuthorizeTimeout = 5; 

    public static event SendChatMessageDelegate OnWriteToChat = delegate { };
    public static event SendChatMessageDelegate OnServerException = delegate { };

    public static event Action<ulong> OnCharacterAuthorized = delegate { }; 

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
    
    private Dictionary<ulong, PrivateClientInfo> privateClientsData = new ();
    
    private Dictionary<ulong, Coroutine> characterAuthorizeTimeout = new ();


    public SpawnArguments RandomArguments()
    {
        return new()
        {
            // characterColor = CharacterColor.Celestial,
            ColorScheme = (CharacterColorScheme) UnityEngine.Random.Range(0, Enum.GetNames(typeof(CharacterColorScheme)).Length),
            CharacterIndex = UnityEngine.Random.Range(0, characters.PrefabList.Count),
            WeaponIndex = UnityEngine.Random.Range(0, weapons.PrefabList.Count)
        };
    }

    public PublicClientData FindClientData(ulong ClientID)
    {
        foreach (var item in playersData)
        {
            if (item.ID == ClientID)
            {
                return item;
            }
        }
        
        throw new KeyNotFoundException("Player is not authorized");
    }
    public bool IsPlayerAuthorized (ulong ID)
    {
        return privateClientsData.ContainsKey(ID);
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

    public void Authorize(AuthorizeArguments authorizeInfo)
    {
        Authorize_ServerRpc(authorizeInfo);
    }

    public void Spawn(SpawnArguments args)
    {
        spawnArgs = args;

        Spawn_ServerRpc(args);
    }
    public void SpawnWithRandomArgs()
    {
        Spawn(RandomArguments());
    }

    public void WriteToChat(FixedString512Bytes text)
    {
        SendChatMessage_ServerRpc(text);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected_Event;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected_Event;
        }      

        if (IsClient)
        {
            Authorize(AuthorizeArguments.This);  
        }
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected_Event;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected_Event;
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

    private void OnClientConnected_Event(ulong ID)
    {
        Debug.Log("Someone is trying to connect . . .");
        characterAuthorizeTimeout.Add(ID, StartCoroutine(KickTimeOut(ID)));
    }
    private void OnClientDisconnected_Event(ulong ID)
    {
        if (IsPlayerAuthorized(ID))
        {
            var publicDataIndex = IndexOfPlayerData(data => data.ID == ID);

            Debug.Log($"{playersData[publicDataIndex].Name} is disconnected");

            playersData.RemoveAt(publicDataIndex);
            
            try 
            {
                if (privateClientsData[ID].networkCharacter != null)
                {
                    
                    privateClientsData[ID].networkCharacter.Kill();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
            finally
            {
                privateClientsData[ID].Dispose();
                privateClientsData.Remove(ID);
            }
        }
    }


    private IEnumerator KickTimeOut(ulong ID)
    {
        yield return new WaitForSecondsRealtime(AuthorizeTimeout);
        
        if (!IsPlayerAuthorized(ID))
        {
            NetworkManager.Singleton.DisconnectClient(ID, "Player is not authorized. Time out.");
            characterAuthorizeTimeout.Remove(ID);
        }
    }

    private PlayerNetworkCharacter SpawnCharacter (SpawnArguments args, ServerRpcParams Param)
    {
        var senderId = Param.Receive.SenderClientId;
        var client = privateClientsData[senderId];
        
        if (client.networkCharacter != null && client.networkCharacter.IsSpawned)
            return null;

        var spawnPoint = SpawnPoint.GetSpawnPoint().transform;

        args.CharacterIndex = Mathf.Clamp(args.CharacterIndex, 0, characters.PrefabList.Count - 1);

        // Spawn character
        var character = characters.PrefabList[args.CharacterIndex].Prefab;
        var characterGameObject = Instantiate(character, spawnPoint.position, spawnPoint.rotation);
    
        client.networkCharacter = characterGameObject.GetComponent<PlayerNetworkCharacter>();
        client.networkCharacter.NetworkObject.SpawnWithOwnership(senderId);

        return client.networkCharacter;
    }
    private void SetWeapon (SpawnArguments args, ServerRpcParams Param)
    {
        var senderId = Param.Receive.SenderClientId;
        var client = privateClientsData[senderId];
        
        if (client.networkCharacter == null)
            return;

        if (client.networkCharacterWeapon != null && client.networkCharacterWeapon.IsSpawned)
        {
            client.networkCharacterWeapon.Despawn();
            client.networkCharacterWeapon = null;
        }


        args.WeaponIndex = Mathf.Clamp(args.WeaponIndex, 0, weapons.PrefabList.Count - 1);

        // Spawn weapon
        var weapon = weapons.PrefabList[args.WeaponIndex].Prefab;
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
        if (DataIndex == -1)
            return;

        if (CharactersLimit <= privateClientsData.Count)
            return;
        
        var data = playersData[DataIndex];
        data.spawnArguments = args;
        playersData[DataIndex] = data;

        var player = SpawnCharacter(args, Param);
        SetWeapon(args, Param);

        if (player != null)
        {
            player.RefreshColor();
        }
    }

    [ServerRpc (RequireOwnership = false)]
    private void Authorize_ServerRpc(AuthorizeArguments authorizeInfo, ServerRpcParams Param = default)
    {
        var senderId = Param.Receive.SenderClientId;
        
        if (IsPlayerAuthorized(senderId))
            return;

        if (characterAuthorizeTimeout.ContainsKey(senderId))
        {
            StopCoroutine(characterAuthorizeTimeout[senderId]);
            characterAuthorizeTimeout.Remove(senderId);
        }
        
        privateClientsData.Add(senderId, new ()
        {
            networkClient = NetworkManager.Singleton.ConnectedClients[senderId],

            networkCharacter = null,
            networkCharacterWeapon = null,

            EnterTime = DateTime.Now,
            DeathTime = DateTime.Now,
            RespawnTime = DateTime.Now,
        });
        playersData.Add(new()
        {
            ID = senderId,

            Name = authorizeInfo.Name,

            statistics = PlayerStatistics.Empty()
        });

        Debug.Log($"Player {authorizeInfo.Name} is succesfully authorized with ID:{senderId}");

        OnCharacterAuthorized.Invoke(senderId);
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
           
        Debug.Log($"Message: {FindClientData(Param.Receive.SenderClientId).Name} - {text}");
    }

    [ClientRpc]
    private void SendChatMessage_ClientRpc(ulong clientID, FixedString512Bytes text)
    {
        var data = FindClientData(clientID);
        OnWriteToChat.Invoke(data, text);

        Debug.Log($"Message: {data.Name} - {text}");
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
