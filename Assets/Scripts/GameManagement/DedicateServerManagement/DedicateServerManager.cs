
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using Cysharp.Threading.Tasks;
using Mirror;

public class DedicateServerManager : MonoBehaviour
{

    private static void StartServerOnLoad_Event(Scene scene, LoadSceneMode mode)
    {
        NetworkManager.singleton.StartServer();
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
    }
#endif
}