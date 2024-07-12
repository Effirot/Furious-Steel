

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

            if (effectsHolder.character is IDamageSource)
            {
                var source = effectsHolder.character as IDamageSource;

                source.onDamageDelivered += OnDamageRecieved;
            }
        }

        public override void Remove()
        {
            base.Remove();

            if (effectsHolder.character is IDamageSource)
            {
                var source = effectsHolder.character as IDamageSource;

                source.onDamageDelivered -= OnDamageRecieved;
            }
        }

        private async void OnDamageRecieved(DamageDeliveryReport damageDeliveryReport)
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