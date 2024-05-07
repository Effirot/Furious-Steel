using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowRotate : MonoBehaviour
{
    [SerializeField]
    private Vector3 Angle;

    private void LateUpdate()
    {
        transform.eulerAngles = transform.eulerAngles + Angle * Time.deltaTime;
    }
}
