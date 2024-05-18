using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantiatePrefabOnDestroy : MonoBehaviour
{
    [SerializeField]
    private GameObject Object;

    [SerializeField, Range(0, 20)]
    private float removeObjectAfter = 5;

    private void OnDestroy()
    {
        var gameObject = Instantiate(Object, transform.position, transform.rotation);

        gameObject.SetActive(true);
        
        Destroy(gameObject, removeObjectAfter);
    }
}
