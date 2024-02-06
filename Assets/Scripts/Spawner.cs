using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Spawner : NetworkBehaviour
{
    [SerializeField, Range(0, 120)]
    private float spawnInterval = 10f;

    [SerializeField]
    private GameObject prefab;

    private NetworkObject instance = null;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            StartCoroutine(SpawnProcess());
        }
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer)
        {
            StopAllCoroutines();
        }
    }

    private void Spawn()
    {
        var prefabObject = Instantiate(prefab, transform.position, Quaternion.identity);

        instance = prefabObject.GetComponent<NetworkObject>();
        instance.Spawn();
    }

    private IEnumerator SpawnProcess()
    {
        while (true)
        {
            if (instance != null || !instance.IsSpawned)
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
