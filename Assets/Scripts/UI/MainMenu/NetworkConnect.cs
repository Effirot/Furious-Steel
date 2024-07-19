using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class NetworkConnect : MonoBehaviour
{
    [SerializeField]
    private UnityEvent OnSuccesfullyConnect = new();
    [SerializeField]
    private UnityEvent OnUnsuccesfullyConnect = new();

    private static NetworkManager NetworkManager => NetworkManager.singleton;

    private static void StartHostOnLoad_Event(Scene scene, LoadSceneMode mode)
    {
        NetworkManager.StartHost();

        SceneManager.sceneLoaded -= StartHostOnLoad_Event;
    }
    
    public async void Connect() 
    {        
        NetworkManager.StartClient();
    
        await UniTask.WaitUntil(() => NetworkClient.isConnecting);

        if (NetworkClient.connection != null)
        {
            OnSuccesfullyConnect.Invoke();
        }
        else
        {
            OnUnsuccesfullyConnect.Invoke();

            // Shutdown();
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
        switch (NetworkManager.mode)
        {
            case NetworkManagerMode.ServerOnly:
                NetworkManager.StopServer();
                break; 
            
            case NetworkManagerMode.ClientOnly:
                NetworkManager.StopClient();
                break;

            case NetworkManagerMode.Host:
                NetworkManager.StopHost();
                break;
        }
    }

    public void SetIP(string IP)
    {    
        NetworkManager.networkAddress = IP;
    }
    public void SetPort(string Port)
    {
        var transport = NetworkManager.transport;

        if (NetworkManager.transport is PortTransport)
        {
            ((PortTransport)transport).Port = ushort.Parse(Port);
        }
    
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
