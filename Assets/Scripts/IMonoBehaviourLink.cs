using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMonoBehaviourLink 
{
    Transform transform { get; }

    GameObject gameObject { get; }
}
