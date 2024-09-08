

using System;
using System.Linq;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

namespace CharacterSystem.Effects
{
    [System.Serializable]
    public class FlameBurnEffect : BurnEffect
    {
        private static GameObject explodeResource;

        [SerializeField]
        private Damage explodeDamage;

        [SerializeField, Range(0, 20)]
        private float explodeRadius = 5;
        [SerializeField, Range(0, 5)]
        private float explodePushForce = 2;

        public override bool Existance => base.Existance;

        public override void Start()
        {
            base.Start();

            if (effectsHolder.character is IDamagable)
            {
                var source = effectsHolder.character as IDamagable;

                source.onDamageRecieved += OnDamageRecieved;
            }
        }
        public override void Remove()
        {
            base.Remove();

            if (effectsHolder.character is IDamagable)
            {
                var source = effectsHolder.character as IDamagable;

                source.onDamageRecieved -= OnDamageRecieved;
            }
        }

        private void OnDamageRecieved(Damage damage)
        {
            if (damage.type != Damage.Type.Effect && damage.args.Contains(Damage.DamageArgument.TRIGGER) && Existance)
            {
                time = -1;
                
                explodeResource ??= Resources.Load<GameObject>("PowerUps/Prefabs/ExplodeEffect");
                
                GameObject.Destroy(GameObject.Instantiate(explodeResource, effectsHolder.transform.position, effectsHolder.transform.rotation), 5);

                foreach (var collider in Physics.OverlapSphere(effectsHolder.transform.position, explodeRadius))
                {
                    var newDamage = explodeDamage;
                    newDamage.pushDirection = collider.transform.position - effectsHolder.transform.position;
                    newDamage.pushDirection.Normalize();
                    newDamage.pushDirection *= explodePushForce;
                    newDamage.pushDirection.y = Mathf.Max(explodeDamage.pushDirection.y);

                    newDamage.senderID = damage.senderID;

                    var report = Damage.Deliver(collider.gameObject, newDamage);
                    newDamage.sender?.DamageDelivered(report);
                }
            }
        }
    }
}