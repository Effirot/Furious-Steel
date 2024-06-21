using System.Collections;
using System.Collections.Generic;
using Mirror;
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
        if (NetworkManager.singleton)
        {
            height = (Mathf.Sin((float)NetworkTime.time / 12f) + 0.6f) * 2.9f;

            transform.position = Vector3.Lerp(transform.position, resultPosition, 0.05f);
        }
    }
}
