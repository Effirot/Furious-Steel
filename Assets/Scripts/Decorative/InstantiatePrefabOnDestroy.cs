using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InstantiatePrefabOnDestroy : MonoBehaviour
{
    [SerializeField]
    private GameObject targetObject;

    [SerializeField, Range(0, 20)]
    private float removeObjectAfter = 5;

    [SerializeField, Range(0, 20)]
    private bool randomRotation = true;

#if !UNITY_SERVER || UNITY_EDITOR
    private void OnDisable()
    {
        var spawnedObject = GameObject.Instantiate(targetObject, transform.position, Quaternion.Euler(0, Random.Range(-180, 180), 0));

        spawnedObject.SetActive(true);
        
        Destroy(spawnedObject, removeObjectAfter);
    }
#endif
}
