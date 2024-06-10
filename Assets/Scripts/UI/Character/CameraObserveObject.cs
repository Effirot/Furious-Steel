using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CameraObserveObject : MonoBehaviour,
    IObservableObject
{
    [field : SerializeField]
    public Transform ObservingPoint { get; set; }

    public bool IsOwner => false;
}
