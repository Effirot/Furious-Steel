using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ExecuteOnLoadMethod
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    public static void OnLoad()
    {
        GameObject.DontDestroyOnLoad(GameObject.Instantiate(Resources.Load<GameObject>("[NetworkManager]")));
        GameObject.DontDestroyOnLoad(GameObject.Instantiate(Resources.Load<GameObject>("[MainMenu]"))); 
    }
}
