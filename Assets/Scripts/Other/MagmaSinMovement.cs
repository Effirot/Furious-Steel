using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MagmaSinMovement : MonoBehaviour
{
    private float height = 0;

    private Vector3 startPosition = Vector3.zero;

    private Vector3 resultPosition => startPosition + Vector3.up * height;

   private void Awake()
    {
        startPosition = transform.position;
        transform.position = resultPosition;
    }
    
    private void FixedUpdate()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            height = (Mathf.Sin(Time.time / 10f) + 0.6f) * 2.2f;

            transform.position = Vector3.Lerp(transform.position, resultPosition, 0.05f);
        }
    }
}
