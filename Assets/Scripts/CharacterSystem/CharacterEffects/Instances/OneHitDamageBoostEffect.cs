

using System;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using Cysharp.Threading.Tasks;

namespace CharacterSystem.Effects
{
    [Serializable]
    public class OneHitDamageBoostEffect : DamageBoostEffect
    {
        public override bool Existance => base.Existance;

        public override void Start()
        {
            base.Start();

            if (effectsHolder.character is IAttackSource)
            {
                var source = effectsHolder.character as IAttackSource;

                source.onDamageDelivered += OnDamageDelivered;
            }
        }

        public override void Remove()
        {
            base.Remove();

            if (effectsHolder.character is IAttackSource)
            {
                var source = effectsHolder.character as IAttackSource;

                source.onDamageDelivered -= OnDamageDelivered;
            }
        }

        private async void OnDamageDelivered(DamageDeliveryReport damageDeliveryReport)
        {
            if (damageDeliveryReport.damage.type is not Damage.Type.Effect && Existance)
            {
                await UniTask.WaitForFixedUpdate();

                if (this != null && Existance)
                {
                    time = -1;
                }
            }
        }
    }
}