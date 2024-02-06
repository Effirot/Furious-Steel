using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static RoomManager.SpawnArguments;

[DisallowMultipleComponent]
public class RoomManager : NetworkBehaviour
{
    public struct SpawnArguments : INetworkSerializable
    {
        public enum CharacterColor
        {
            Red,
            Green,
            Blue,
            Black,
            Gray,
            White,
            Orange,
            Purple,
            Pink,
            Cyan,
        } 

        public CharacterColor Color;
        public int WeaponIndex;
        public int CharacterIndex;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Color);
            serializer.SerializeValue(ref WeaponIndex);
            serializer.SerializeValue(ref CharacterIndex);
        }
    }

    protected class ClientInfo 
    {
        protected internal ClientInfo() { }

        public NetworkClient networkClient;

        public PlayerNetworkCharacter networkCharacter;

        public DateTime EnterTime;
        public DateTime DeathTime;
        public DateTime RespawnTime;

        public float DeliveredDamage;

        public SpawnArguments spawnArguments;

    }

    public delegate void SendChatMessageDelegate (FixedString128Bytes senderName, FixedString512Bytes text);

    public static RoomManager Singleton { get; private set; }

    public static event SendChatMessageDelegate OnWriteToChat = delegate { };


    [SerializeField]
    private NetworkPrefabsList characters;
    
    [SerializeField]
    private NetworkPrefabsList weapons;

    [SerializeField]
    public UnityEvent OnOwnerCharacterDead = new ();

    private Dictionary<ulong, ClientInfo> clients = new Dictionary<ulong, ClientInfo>();


    public SpawnArguments RandomArguments()
    {
        return new()
        {
            Color = (CharacterColor) UnityEngine.Random.Range(0, Enum.GetNames(typeof(CharacterColor)).Length),
            CharacterIndex = UnityEngine.Random.Range(0, characters.PrefabList.Count),
            WeaponIndex = UnityEngine.Random.Range(0, weapons.PrefabList.Count)
        };
    }

    public void Spawn(SpawnArguments args)
    {
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

        // if (IsHost)
        // {
        //     OnClientConnected_Event(NetworkManager.LocalClientId);
        // }

        StartCoroutine(LateSpawn());

        IEnumerator LateSpawn()
        {
            yield return new WaitForSeconds(1);

            SpawnWithRandomArgs();
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
    }

    private void OnClientConnected_Event(ulong ID)
    {
        var client = new ClientInfo()
        {
            networkClient = NetworkManager.Singleton.ConnectedClients[ID],

            networkCharacter = null,

            EnterTime = DateTime.Now,
            DeathTime = DateTime.Now,
            RespawnTime = DateTime.Now,

            DeliveredDamage = 0,

            spawnArguments = new SpawnArguments(),
        };

        clients.Add(ID, client);
    }
    private void OnClientDisconnected_Event(ulong ID)
    {
        clients.Remove(ID);
    }

    [ServerRpc (RequireOwnership = false)]
    private void Spawn_ServerRpc(SpawnArguments args, ServerRpcParams Param = default)
    {
        var senderId = Param.Receive.SenderClientId;

        if (IsPlayersCharacterAlreadySpawned())
            return;

        var spawnPoint = SpawnPoint.GetSpawnPoint().transform;

        args.CharacterIndex = Mathf.Clamp(args.CharacterIndex, 0, characters.PrefabList.Count - 1);
        args.WeaponIndex = Mathf.Clamp(args.WeaponIndex, 0, weapons.PrefabList.Count - 1);

        // Spawn character
        var character = characters.PrefabList[args.CharacterIndex].Prefab;
        var characterGameObject = Instantiate(character, spawnPoint.position, spawnPoint.rotation);

        // Spawn weapon
        var weapon = weapons.PrefabList[args.WeaponIndex].Prefab;
        var weaponGameObject = Instantiate(weapon);

        clients[senderId].networkCharacter = characterGameObject.GetComponent<PlayerNetworkCharacter>();
        if (clients[senderId].networkCharacter == null)
        {
            throw new InvalidOperationException();
        } 

        clients[senderId].spawnArguments = args;


        // Spawn
        characterGameObject.GetComponent<NetworkObject>().SpawnWithOwnership(senderId, true);
        weaponGameObject.GetComponent<NetworkObject>().SpawnWithOwnership(senderId, true);
        weaponGameObject.transform.SetParent(characterGameObject.transform, false);
        // weaponGameObject.transform.localPosition = Vector3.zero;
        // weaponGameObject.transform.localRotation = Quaternion.identity;

        bool IsPlayersCharacterAlreadySpawned()
        {
            return clients[senderId].networkCharacter != null;
        }
    }

    [ServerRpc (RequireOwnership = false)]
    private void SendChatMessage_ServerRpc(FixedString512Bytes text, ServerRpcParams Param = default)
    {
        SendChatMessage_ClientRpc("UNKNOWN", text);
    }

    [ClientRpc]
    private void SendChatMessage_ClientRpc(FixedString128Bytes senderName, FixedString512Bytes text)
    {
        OnWriteToChat.Invoke(senderName, text);
    }
}
