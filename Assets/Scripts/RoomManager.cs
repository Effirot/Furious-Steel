using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using static RoomManager.SpawnArguments;

[DisallowMultipleComponent]
public class RoomManager : NetworkBehaviour
{
    public delegate void SendChatMessageDelegate (FixedString128Bytes senderName, FixedString512Bytes text);

    public static RoomManager Singleton { get; private set; }

    public static event SendChatMessageDelegate OnWriteToChat = delegate { };


    [SerializeField]
    private NetworkPrefabsList characters;
    
    [SerializeField]
    private NetworkPrefabsList weapons;

    private List<PlayerNetworkCharacter> playerCharacters = new List<PlayerNetworkCharacter>();


    public SpawnArguments RandomArguments()
    {
        return new()
        {
            Color = (CharacterColor) UnityEngine.Random.Range(0, Enum.GetNames(typeof(CharacterColor)).Length),
            CharacterIndex = UnityEngine.Random.Range(0, characters.PrefabList.Count),
            WeaponIndex = 1
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

        SpawnWithRandomArgs();
    }

    private void Awake()
    {
        Singleton = this;
    }




    [ServerRpc (RequireOwnership = false)]
    private void Spawn_ServerRpc(SpawnArguments args, ServerRpcParams Param = default)
    {
        if (IsPlayersCharacterAlreadySpawned())
            return;

        var spawnPoint = SpawnPoint.GetSpawnPoint().transform;

        args.CharacterIndex = Mathf.Clamp(args.CharacterIndex, 0, characters.PrefabList.Count - 1);
        args.WeaponIndex = Mathf.Clamp(args.WeaponIndex, 0, weapons.PrefabList.Count - 1);

        // Spawn character
        var character = characters.PrefabList[args.CharacterIndex].Prefab;
    
        var characterGameObject = Instantiate(character, spawnPoint.position, spawnPoint.rotation);
        characterGameObject.GetComponent<NetworkObject>().SpawnWithOwnership(Param.Receive.SenderClientId, true);

        // Spawn weapon
        var weapon = weapons.PrefabList[args.WeaponIndex].Prefab;
    
        var weaponGameObject = Instantiate(weapon, characterGameObject.transform);
        weaponGameObject.GetComponent<NetworkObject>().SpawnWithOwnership(Param.Receive.SenderClientId, true);
        weaponGameObject.transform.SetParent(characterGameObject.transform);


        playerCharacters.Add(characterGameObject.GetComponent<PlayerNetworkCharacter>());




        bool IsPlayersCharacterAlreadySpawned()
        {
            return playerCharacters.Exists(character => character.NetworkObject.OwnerClientId == Param.Receive.SenderClientId);
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
}
