

using System;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Effiry.Items;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public class ConnectedPlayerData : NetworkBehaviour
{
    [Serializable]
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

    [Serializable]
    public struct SpawnArguments
    {
        public static SpawnArguments Local; 

        public string CharacterItemJson;
        public string WeaponItemJson;
        public string TrinketItemJson;
    }

    private static Transform connectionsFolder {
        get {
            var result = GameObject.Find("--- Connections");
            
            if (result == null) {
                result = new GameObject("--- Connections");
                result.transform.SetAsFirstSibling();
            }

            return result.transform; 
        }
    }   

    public static List<ConnectedPlayerData> All = new ();

    public static ConnectedPlayerData Local => All.Find(data => data.isOwned); 

    [SyncVar(hook = nameof(OnNameChanged))]
    public string Name;
    
    [SyncVar]
    public SpawnArguments spawnArguments = new SpawnArguments();

    [SyncVar]
    public PlayerStatistics statistics = PlayerStatistics.Empty();

    private void Awake()
    {
        transform.SetParent(connectionsFolder);

        Damage.damageDeliveryPipeline += AgregateDamageReport;

        All.Add(this);
    }
    private void OnDestroy()
    {
        Damage.damageDeliveryPipeline -= AgregateDamageReport;
        
        All.Remove(this);
    }

    private void AgregateDamageReport(DamageDeliveryReport damage)
    {
        
    }
    private void OnNameChanged(string Old, string New)
    {
        if (NetworkClient.localPlayer != null)
        {
            NetworkClient.localPlayer.gameObject.name = Name;
        }

        gameObject.name = Name;
    }


    public Item JsonToItem (string json)
    {
        if (json == null || json.Length <= 0)
            return null;

        return Item.FromJsonString(json);
    }
    
    [Client, Command(requiresAuthority = false)]
    public void Spawn(SpawnArguments args, NetworkConnectionToClient sender = null)
    {
        if (NetworkClient.localPlayer != null)
            return;

        spawnArguments = args;

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
                character.SetWeapon(JsonToItem(args.WeaponItemJson));
            }
            catch (Exception e) 
            {
                Debug.LogException(e);
            }
        }
    }

    
    private PlayerNetworkCharacter SpawnCharacter (Item item, NetworkConnectionToClient networkConnection)
    {
        if (item == null)
            return null;

        return SpawnCharacter(item.TypeName, networkConnection);
    }
    private PlayerNetworkCharacter SpawnCharacter (string name, NetworkConnectionToClient networkConnection)
    {   
        var character = RoomManager.Singleton.ResearchCharacterPrefab(name);
        
        // Get spawn point
        var spawnPoint = SpawnPoint.GetSpawnPoint().transform;

        // Spawn character
        var characterGameObject = GameObject.Instantiate(character, spawnPoint.position, spawnPoint.rotation);
        
        NetworkServer.Spawn(characterGameObject, networkConnection);
        NetworkServer.AddPlayerForConnection(networkConnection, characterGameObject);
        characterGameObject.GetComponent<NetworkIdentity>().AssignClientAuthority(networkConnection);

        return characterGameObject.GetComponent<PlayerNetworkCharacter>();
    }

}