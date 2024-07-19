
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
#if !UNITY_SERVER || UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnStartClient()
    {
        GameObject.DontDestroyOnLoad(GameObject.Instantiate(Resources.Load<GameObject>("NetworkManager")));
    }
#else 
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnStartDedicateServer()
    {
        GameObject.DontDestroyOnLoad(GameObject.Instantiate(Resources.Load<GameObject>("NetworkManager")));

        Debug.Log("LoadingScene " + System.Environment.GetCommandLineArgs()[1]);
        
        SceneManager.LoadScene(System.Environment.GetCommandLineArgs()[1]);
    }
#endif
}