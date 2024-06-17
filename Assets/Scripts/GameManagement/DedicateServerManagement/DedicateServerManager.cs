
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using Cysharp.Threading.Tasks;

public class DedicateServerManager : MonoBehaviour
{

    private static void StartServerOnLoad_Event(Scene scene, LoadSceneMode mode)
    {
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;

        NetworkManager.Singleton.StartServer();
        Debug.Log("Server was Started");

        SceneManager.sceneLoaded -= StartServerOnLoad_Event;
    }

#if !UNITY_SERVER || UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnStartClient()
    {
        Application.targetFrameRate = 240;
        QualitySettings.vSyncCount = 0; 

        GameObject.DontDestroyOnLoad(GameObject.Instantiate(Resources.Load<GameObject>("NetworkManager")));
        GameObject.DontDestroyOnLoad(GameObject.Instantiate(Resources.Load<GameObject>("MainMenu")));
    }
#else 
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnStartDedicateServer()
    {
        GameObject.DontDestroyOnLoad(GameObject.Instantiate(Resources.Load<GameObject>("NetworkManager")));

        Debug.Log("LoadingScene " + System.Environment.GetCommandLineArgs()[1]);
        
        SceneManager.sceneLoaded += StartServerOnLoad_Event;
        SceneManager.LoadScene(System.Environment.GetCommandLineArgs()[1]);

        Application.targetFrameRate = 240;
        QualitySettings.vSyncCount = 0;

        PuncherServer server = new PuncherServer();
        server.Start(new IPEndPoint(IPAddress.Any, 7777));
    }
#endif
}