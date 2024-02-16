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
                    value = 60,
                    stunlock = 1,
                    pushDirection = Vector3.up * 2 + VectorToTarget,
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

            holder.StartCoroutine(RemoveGameObject(effectObject));
        }
        IEnumerator RemoveGameObject(GameObject gameObject)
        {
            yield return new WaitForSeconds(5);

            if (gameObject != null)
            {
                GameObject.Destroy(gameObject);
            }
        }
    }

    public override void OnPick(PowerUpHolder holder)
    {

    }
}