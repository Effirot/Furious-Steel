
using UnityEngine;

namespace CharacterSystem.Effects
{
    [System.Serializable]
    public class SpeedBoostEffect : CharacterEffect
    {
        public override bool Existance => time > 0;

        [SerializeField, Range(0, 120)]
        private float time = 0;

        [SerializeField, Range(0, 120)]
        private float force = 5;

        public SpeedBoostEffect() { }
        public SpeedBoostEffect(float Time, float Force)
        {
            time = Time;
            force = Force;
        }

        public override void Start()
        {
            effectsHolder.character.Speed += force;

            effectsHolder.AddGlowing(this, Color.blue * 3, 1);
        }
        public override void Remove()
        {
            effectsHolder.character.Speed -= force;
        }
        public override void Update()
        {
            if (isServer)
            {
                time -= Time.fixedDeltaTime;
            }
        }

        public override void AddDublicate(CharacterEffect effect)
        {
            var speedEffect = (SpeedBoostEffect)effect;
            

            var deltaForce = Mathf.Max(force, speedEffect.force);
            deltaForce -= force;

            force += deltaForce;
            effectsHolder.character.Speed += deltaForce;
            time = Mathf.Max(time, speedEffect.time);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + time;
        }
    }
}
