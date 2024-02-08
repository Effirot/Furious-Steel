using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowRotate : MonoBehaviour
{
    [SerializeField]
    private Vector3 Angle;

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(transform.eulerAngles + Angle * Time.deltaTime);
    }
}
