
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using Unity.Netcode.Transports.UTP;

public class DedicateServerManager : MonoBehaviour
{

    private static void StartServerOnLoad_Event(Scene scene, LoadSceneMode mode)
    {
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;

        NetworkManager.Singleton.StartServer();
        Debug.Log("Server was Started");

        SceneManager.sceneLoaded -= StartServerOnLoad_Event;
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnStartClient()
    {
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 2; 
    }
#elif UNITY_SERVER && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnStartDedicateServer()
    {
        Debug.Log("LoadingScene " + System.Environment.GetCommandLineArgs()[1]);
        
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport; 


        SceneManager.sceneLoaded += StartServerOnLoad_Event;
        SceneManager.LoadScene(System.Environment.GetCommandLineArgs()[1]);

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;       
    }
#endif
}