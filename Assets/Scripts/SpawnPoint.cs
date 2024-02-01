using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class SpawnPoint : MonoBehaviour
{
    private static List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    [SerializeField, Range(0.1f, 50f)]
    private float SafeZoneRange = 3;

    public bool IsSafe => !Physics.OverlapSphere(transform.position, SafeZoneRange, LayerMask.GetMask("Character")).Any();

    private void Awake()
    {
        spawnPoints.Add(this);
    }

    private void OnDestroy()
    {
        spawnPoints.Remove(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsSafe ? Color.green : Color.red;

        Gizmos.DrawWireSphere(transform.position, SafeZoneRange);
    
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, transform.rotation.y, 0) * Vector3.forward);
    }
}
