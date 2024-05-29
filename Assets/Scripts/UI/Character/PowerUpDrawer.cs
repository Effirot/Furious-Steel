using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Objects;
using CharacterSystem.PowerUps;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
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

            foreach (var colliders in instance.gameObject.GetComponents<Collider>())
            {
                Destroy(colliders);
            }
            foreach (var rigidbody in instance.gameObject.GetComponents<Rigidbody>())
            {
                Destroy(rigidbody);
            }
            foreach (var container in instance.gameObject.GetComponents<PowerUpContainer>())
            {
                Destroy(container);
            }
        }
    }

    public void Initialize(IPowerUpActivator activator)
    {        
        Draw(activator.PowerUp);
        activator.onPowerUpChanged.AddListener(Draw);
    }
}