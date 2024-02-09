using System.Collections;
using System.Collections.Generic;
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public class ExplodePowerUp : PowerUp
{
    private static VisualEffectAsset visualEffect = Resources.Load<VisualEffectAsset>("Effects/Explode");
    
    public override GameObject prefab => throw new System.NotImplementedException();

    public override void Activate(PowerUpHolder holder)
    {
        if (holder.IsServer)
        {
            foreach (var collider in Physics.OverlapSphere(holder.transform.position, 4))
            {
                if (collider.gameObject == holder.gameObject)
                    continue;

                if (collider.TryGetComponent<IDamagable>(out var damagable))
                {
                    damagable.Hit(new Damage()
                    {
                        Value = 60,
                        Stunlock = 1,
                        PushForce = 400,
                        Sender = holder.Character
                    });
                }
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