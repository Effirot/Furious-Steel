using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameObjectLink 
{
    Transform transform { get; }

    GameObject gameObject { get; }
}
