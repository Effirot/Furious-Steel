using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class PowerUpDrawer : MonoBehaviour
{
    private Transform instance;

    public void Draw(PowerUp powerUp)
    {
        Destroy(instance?.gameObject);
        instance = null;
        
        if (powerUp != null)
        {
            instance = Instantiate(powerUp.prefab, transform).transform;
            instance.localPosition = Vector3.zero;
            instance.gameObject.layer = LayerMask.NameToLayer("UI");
        }
    }
}