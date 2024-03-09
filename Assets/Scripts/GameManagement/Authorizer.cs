using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(RoomManager))]
public sealed class Authorizer : NetworkBehaviour
{
    public struct AuthorizeArguments : INetworkSerializable
    {
        public static bool TryParse(byte[] bytes, out AuthorizeArguments value)
        {
            value = new AuthorizeArguments()
            {
                Name = Encoding.Unicode.GetString(bytes)
            };

            return true;
        }



        public FixedString64Bytes Name;

        public byte[] ConvertToBytes()
        {
            return Encoding.Unicode.GetBytes(Name.Value);
        }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Name);
        }
    }
    public struct AuthorizedPlayerData
    {
        public NetworkClient networkClient;

        public FixedString64Bytes Name;

        public DateTime AuthorizeTime;
    }

    public static Authorizer Singleton;

    public static bool AuthorizeOnConnect = true;

    public static AuthorizeArguments localAuthorizeArgs = new AuthorizeArguments() {
#if UNITY_EDITOR
        Name = "DEVELOPMENT",
#else
        Name = "Unnamed",
#endif
    };
    

    private Dictionary<ulong, AuthorizedPlayerData> authorizedPlayers = new();
    
    private RoomManager roomManager;


    public bool IsPlayerAuthorized (ulong ID)
    {
        return authorizedPlayers.ContainsKey(ID);
    }

    public override void OnNetworkSpawn()
    {
        Singleton = this;
        roomManager = GetComponent<RoomManager>();

        base.OnNetworkSpawn();

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected_Event;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected_Event;

            NetworkManager.Singleton.ConnectionApprovalCallback  += ConnectionAprovalCheck_Event;
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

    private void OnClientConnected_Event(ulong ID)
    {
        Debug.Log("Someone is trying to connect . . .");
    }
    private void OnClientDisconnected_Event(ulong ID)
    {
        if (IsPlayerAuthorized(ID))
        {
            Debug.Log($"{authorizedPlayers[ID].Name} is disconnected");

            roomManager.OnAuthorizedPlayerDisconnected(ID);
        }
    }

    private void ConnectionAprovalCheck_Event(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (request.Payload.Length == 0) 
        {
            response.Approved = false;

            return;
        }

        if (response.Approved = AuthorizeArguments.TryParse(request.Payload, out var authorizeArguments))
        {
            roomManager.OnPlayerAuthorized(request.ClientNetworkId, authorizeArguments);

            if (IsPlayerAuthorized(request.ClientNetworkId))
                return;
            
            var authorizeData = new AuthorizedPlayerData()
            {
                networkClient = NetworkManager.Singleton.ConnectedClients[request.ClientNetworkId],

                Name = authorizeArguments.Name,

                AuthorizeTime = DateTime.Now
            };

            authorizedPlayers.Add(request.ClientNetworkId, authorizeData);

            Debug.Log($"Player {authorizeArguments.Name} is succesfully authorized with ID:{request.ClientNetworkId}");
        }
    }
}
