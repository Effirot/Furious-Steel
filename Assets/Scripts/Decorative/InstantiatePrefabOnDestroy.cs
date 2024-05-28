using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantiatePrefabOnDestroy : MonoBehaviour
{
    [SerializeField]
    private GameObject targetObject;

    [SerializeField, Range(0, 20)]
    private float removeObjectAfter = 5;

    private void OnDisable()
    {
        var spawnedObject = GameObject.Instantiate(targetObject, transform.position, transform.rotation);

        spawnedObject.SetActive(true);
        
        Destroy(spawnedObject, removeObjectAfter);
    }
}
