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
    private static VisualEffectAsset visualEffect = Resources.Load<VisualEffectAsset>("Effects/Explode");
    
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
                    stunlock = 1,
                    pushDirection = (Vector3.up + VectorToTarget).normalized,
                    type = Damage.Type.Unblockable,
                    sender = holder.Invoker
                });
            }
        }

        InvokeEffect();

        void InvokeEffect()
        {
            var effectObject = new GameObject(visualEffect.name);
            effectObject.transform.position = holder.transform.position;

            var effect = effectObject.AddComponent<VisualEffect>();

            effect.visualEffectAsset = visualEffect;
            effect.Play();

            Object.Destroy(effectObject, 5);
        }
    }

    public override void OnPick(PowerUpHolder holder)
    {

    }
}