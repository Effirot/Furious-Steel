using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class NetworkConnect : MonoBehaviour
{
    [SerializeField]
    private UnityEvent OnSuccesfullyConnect = new();
    [SerializeField]
    private UnityEvent OnUnsuccesfullyConnect = new();

    private static NetworkManager NetworkManager => NetworkManager.Singleton;

    private static void OnCharacterDisconnected_Event(ulong ID)
    {
        SceneManager.LoadScene(0);

        NetworkManager.OnClientDisconnectCallback -= OnCharacterDisconnected_Event;
    }
    private static void StartHostOnLoad_Event(Scene scene, LoadSceneMode mode)
    {
        NetworkManager.StartHost();

        SceneManager.sceneLoaded -= StartHostOnLoad_Event;
    }
    
    public async void Connect() 
    {        
        if (NetworkManager.StartClient())
        {
            await UniTask.WhenAny( 
                UniTask.WaitUntil(() => NetworkManager.IsConnectedClient), 
                UniTask.WaitForSeconds(10));

            if (!NetworkManager.IsConnectedClient)
            {
                OnUnsuccesfullyConnect.Invoke();

                NetworkManager.Shutdown();
            }

            OnSuccesfullyConnect.Invoke();

            NetworkManager.OnClientDisconnectCallback += OnCharacterDisconnected_Event;
        }
        else
        {
            OnUnsuccesfullyConnect.Invoke();
        }
    }
    public void StartHostOnLoad()
    {
        SceneManager.sceneLoaded += StartHostOnLoad_Event;
    }
    public void StartHost()
    {
        NetworkManager.StartHost();
    }
    public void Shutdown()
    {
        NetworkManager.Shutdown();
    }

    public void SetIP(string IP)
    {
        var transport = (UnityTransport)NetworkManager.NetworkConfig.NetworkTransport;
    
        transport.ConnectionData.Address = IP;
    }
    public void SetPort(string Port)
    {
        var transport = (UnityTransport)NetworkManager.NetworkConfig.NetworkTransport;
    
        transport.ConnectionData.Port = ushort.Parse(Port);
    }

    public void LoadScene(int ID)
    {
        SceneManager.LoadScene(ID);
    }
    public void LoadScene(string Name)
    {
        SceneManager.LoadScene(Name);
    }
    public void LoadSceneAdditive(int ID)
    {
        SceneManager.LoadScene(ID, LoadSceneMode.Additive);
    }
    public void LoadSceneAdditive(string Name)
    {
        SceneManager.LoadScene(Name, LoadSceneMode.Additive);
    }
}
