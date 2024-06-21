using UnityEngine;

namespace CharacterSystem.Effects
{
    [System.Serializable]
    public class SlownessEffect : CharacterEffect
    {
        public override bool Existance => time > 0;

        [SerializeField, Range(0, 120)]
        private float time = 0;

        [SerializeField, Range(0, 10)]
        private float force = 5;

        public SlownessEffect() { }
        public SlownessEffect(float Time, float force)
        {
        time = Time;
        }

        public override void Start()
        {
            effectsHolder.character.Speed -= force;
        }
        public override void Remove()
        {
            effectsHolder.character.Speed += force;
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
            var slownessEffect = (SlownessEffect)effect;

            time = Mathf.Max(time, slownessEffect.time);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + time;
        }
}
}