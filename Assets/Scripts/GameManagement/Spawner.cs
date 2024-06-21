using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Spawner : NetworkBehaviour
{
    [SerializeField]
    private bool SpawnOnStartup = true;
    
    [SerializeField, Range(0, 120)]
    private float spawnInterval = 10f;

    [SerializeField]
    private GameObject prefab;

    private GameObject instance = null;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (isServer)
        {
            if (SpawnOnStartup)
            {
                Spawn();
            }

            StartCoroutine(SpawnProcess());
        }
    }
    public override void OnStopServer()
    {
        base.OnStopServer();

        if (isServer)
        {
            StopAllCoroutines();
        }
    }

    private void Spawn()
    {
        var prefabObject = Instantiate(prefab, transform.position, Quaternion.identity);
        
        prefabObject.name = prefab.name;
        prefabObject.SetActive(true);

        instance = prefabObject; 
        NetworkServer.Spawn(instance, NetworkServer.localConnection);

        instance.transform.SetParent(netIdentity.transform);
    
        Debug.Log($"Object was spawned: {instance.name}");
    }

    private IEnumerator SpawnProcess()
    {
        while (true)
        {
            if (instance == null)
            {
                yield return new WaitForSeconds(spawnInterval);

                Spawn();
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}
