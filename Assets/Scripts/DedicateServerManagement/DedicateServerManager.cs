#if UNITY_SERVER 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Text.RegularExpressions;

public class DedicateServerManager : MonoBehaviour
{

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnStartDedicateServer()
    {
        string[] Args = System.Environment.GetCommandLineArgs ();

        Debug.Log(string.Join(", ", Args));

        // foreach (Group item in Regex.Matches(Args, @"\s*-*([\w|\s]*)"))
        foreach (var item in Args)
        {
            ExecuteArgument(item);
        }

        NetworkManager.Singleton.StartServer();

        Debug.Log("Server was Started");
    }

    private static void ExecuteArgument(string argument)
    {
        Debug.Log("Completing " + argument);

        var splitArgs = Regex.Split(argument, @"/s+");

        try
        {
            switch (splitArgs[0].ToLower())
            {
                case "--load-scene":
                    Debug.Log("Loading scene: " + splitArgs[1]);
                break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }


    public void ExecuteCommand(string command)
    {

    }


}
#endif