
using UnityEngine;

namespace CharacterSystem.Effects
{
    [System.Serializable]
    public class TimeScaleEffect : CharacterEffect
    {
        public override bool Existance => time > 0;

        [SerializeField, Range(0, 120)]
        private float time = 0.3f;

        [SerializeField, Range(0, 1)]
        private float force = 1f;

        public TimeScaleEffect() { }
        public TimeScaleEffect(float Time, float Force)
        {
            time = Time;
            force = Mathf.Clamp(Force, 0f, 1f);
        }

        public override void Start()
        {
            effectsHolder.character.LocalTimeScale -= force;
        }
        public override void Remove()
        {
            effectsHolder.character.LocalTimeScale += force;
        }
        public override void Update()
        {
            time -= Time.fixedDeltaTime;
        }

        public override void AddDublicate(CharacterEffect effect)
        {
            var timeStopEffect = effect as TimeScaleEffect;
            
            time = timeStopEffect.time;
        }

        public override string ToString()
        {
            return base.ToString() + " - " + time;
        }
    }
}