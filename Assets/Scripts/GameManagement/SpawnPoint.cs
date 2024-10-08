using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class SpawnPoint : MonoBehaviour
{
    private static List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
    
    public static SpawnPoint GetSpawnPoint()
    {
        var points = spawnPoints.Where(point => point.IsSafe).ToArray();

        if (points.Length > 0)
        {
            return points[Random.Range(0, points.Count() - 1)];
        }
        else
        {
            var point = spawnPoints[Random.Range(0, spawnPoints.Count() - 1)];
            
            return point;
        }
    }


    [SerializeField, Range(0.1f, 50f)]
    private float SafeZoneRange = 3;

    public bool IsSafe => !intruders.Any();
    public Collider[] intruders => Physics.OverlapSphere(transform.position, SafeZoneRange, LayerMask.GetMask("Character"));

    public void PushAll ()
    {
        // foreach(var item in intruders)
        // {
        //     item.
        // }
    }

    private void Awake ()
    {
        spawnPoints.Add(this);
    }
    private void OnDestroy ()
    {
        spawnPoints.Remove(this);
    }

    private void OnDrawGizmos ()
    {
        Gizmos.color = IsSafe ? Color.green : Color.red;

        Gizmos.DrawWireSphere(transform.position, SafeZoneRange);
    
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, transform.eulerAngles.y, 0) * Vector3.forward);
    }
}
