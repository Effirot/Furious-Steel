
using CharacterSystem.DamageMath;
using UnityEngine;

namespace CharacterSystem.Effects
{
    [System.Serializable]
    public class TimeScaleEffect : LifetimeCharacterEffect
    {
        [SerializeField, Range(0, 1)]
        private float force = 1f;

        [SerializeField]
        private bool removeOnHit = true;

        public TimeScaleEffect() : this(0.3f, 1) { }
        public TimeScaleEffect(float time, float force) : base(time)
        {
            this.force = Mathf.Clamp(force, 0f, 1f);
        }

        public override void Start()
        {
            effectsHolder.character.LocalTimeScale -= force;

            effectsHolder.character.onDamageRecieved += OnDamageRecieved_Event;
        }
        public override void Remove()
        {
            effectsHolder.character.LocalTimeScale += force;

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

            var timeScaleEffectEffect = effect as TimeScaleEffect;
            var newForce = Mathf.Min(timeScaleEffectEffect.force, this.force);
            var deltaValue = this.force - newForce;

            effectsHolder.character.PhysicTimeScale -= deltaValue;

            force = newForce;
        }
    }
}