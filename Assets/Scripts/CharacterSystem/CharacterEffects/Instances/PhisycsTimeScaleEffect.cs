
using CharacterSystem.DamageMath;
using UnityEngine;

namespace CharacterSystem.Effects
{
    [System.Serializable]
    public class PhisycsTimeScaleEffect : LifetimeCharacterEffect
    {
        [SerializeField, Range(0, 1)]
        private float force = 1f;

        public PhisycsTimeScaleEffect() : this(0.3f, 1) { }
        public PhisycsTimeScaleEffect(float time, float force) : base(time)
        {
            this.force = Mathf.Clamp(force, 0f, 1f);
        }

        public override void Start()
        {
            effectsHolder.character.PhysicTimeScale -= force;

            effectsHolder.character.onDamageRecieved += OnDamageRecieved_Event;
        }
        public override void Remove()
        {
            effectsHolder.character.PhysicTimeScale += force;

            effectsHolder.character.onDamageRecieved -= OnDamageRecieved_Event;
        }

        private void OnDamageRecieved_Event(Damage damage)
        {
            if (damage.type is not Damage.Type.Effect)
            {
                time = -1;
            }
        }
        
        public override void AddDublicate(CharacterEffect effect)
        {
            base.AddDublicate(effect);

            var phisycsTimeScaleEffectEffect = effect as PhisycsTimeScaleEffect;
            var newForce = Mathf.Min(phisycsTimeScaleEffectEffect.force, this.force);
            var deltaValue = this.force - newForce;

            effectsHolder.character.PhysicTimeScale -= deltaValue;

            force = newForce;
        }
    }
}