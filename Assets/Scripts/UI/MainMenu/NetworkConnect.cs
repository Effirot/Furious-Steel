using System.Collections;
using System.Collections.Generic;
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

    private static void OnCharacterDisconnected_Event(ulong ID)
    {
        SceneManager.LoadScene(0);

        NetworkManager.Singleton.OnClientDisconnectCallback -= OnCharacterDisconnected_Event;
    }
    private static void StartHostOnLoad_Event(Scene scene, LoadSceneMode mode)
    {
        NetworkManager.Singleton.StartHost();

        SceneManager.sceneLoaded -= StartHostOnLoad_Event;
    }
    
    public void Connect() 
    {        
        if (NetworkManager.Singleton.StartClient())
        {
            
            OnSuccesfullyConnect.Invoke();

            NetworkManager.Singleton.OnClientDisconnectCallback += OnCharacterDisconnected_Event;
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
        NetworkManager.Singleton.StartHost();
    }
    public void Shutdown()
    {
        NetworkManager.Singleton.Shutdown();
    }

    public void SetIP(string IP)
    {
        var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
    
        transport.ConnectionData.Address = IP;
    }
    public void SetPort(string Port)
    {
        var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
    
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
