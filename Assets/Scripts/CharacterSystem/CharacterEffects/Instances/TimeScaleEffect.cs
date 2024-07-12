
using UnityEngine;

namespace CharacterSystem.Effects
{
    [System.Serializable]
    public class TimeScaleEffect : LifetimeCharacterEffect
    {
        [SerializeField, Range(0, 1)]
        private float force = 1f;

        public TimeScaleEffect() : this(0.3f, 1) { }
        public TimeScaleEffect(float time, float force) : base(time)
        {
            this.force = Mathf.Clamp(force, 0f, 1f);
        }

        public override void Start()
        {
            effectsHolder.character.LocalTimeScale -= force;
        }
        public override void Remove()
        {
            effectsHolder.character.LocalTimeScale += force;
        }

        public override void AddDublicate(CharacterEffect effect)
        {
            base.AddDublicate(effect);

            var timeStopEffect = effect as TimeScaleEffect;
        }
    }
}