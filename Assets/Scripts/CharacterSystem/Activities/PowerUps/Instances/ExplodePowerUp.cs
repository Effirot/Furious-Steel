using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using CharacterSystem.PowerUps;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public class ExplodePowerUp : PowerUp
{
    private static GameObject effectPrefab = Resources.Load<GameObject>("PowerUps/Prefabs/ExplodeEffect");
    
    public override void Activate(PowerUpHolder holder)
    {
        if (holder.IsServer)
        {
            foreach (var collider in Physics.OverlapSphere(holder.transform.position, 4))
            {
                if (collider.gameObject == holder.gameObject)
                    continue;

                var VectorToTarget = holder.transform.position - collider.transform.position;
                VectorToTarget.Normalize();

                Damage.Deliver(collider.gameObject, new Damage()
                {
                    value = 50,
                    stunlock = 0.8f,
                    pushDirection = Vector3.up / 1.5f,
                    type = Damage.Type.Unblockable,
                    sender = holder.Invoker
                });
            }
        }

        GameObject.Destroy(GameObject.Instantiate(effectPrefab, holder.transform.position, holder.transform.rotation), 5);
    }
}