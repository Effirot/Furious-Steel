#if UNITY_SERVER && !UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

public class DedicateServerManager : MonoBehaviour
{

    private static void StartServerOnLoad_Event(Scene scene, LoadSceneMode mode)
    {
        NetworkManager.Singleton.StartServer();
        Debug.Log("Server was Started");

        SceneManager.sceneLoaded -= StartServerOnLoad_Event;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnStartDedicateServer()
    {
        Debug.Log("LoadingScene " + System.Environment.GetCommandLineArgs()[1]);
        
        SceneManager.sceneLoaded += StartServerOnLoad_Event;
        SceneManager.LoadScene(System.Environment.GetCommandLineArgs()[1]);

    }

    public void ExecuteCommand(string command)
    {

    }


}
#endif