

using UnityEngine;

namespace CharacterSystem.Effects
{
    public abstract class LifetimeCharacterEffect : CharacterEffect
    {
        public override bool Existance => time > 0;
        
        [SerializeField, Range(0, 120)]
        public float time = 0;

        public LifetimeCharacterEffect() : this(1) { }
        public LifetimeCharacterEffect(float time)
        {
            this.time = time;
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
            var lifetimeCharacterEffect = (LifetimeCharacterEffect)effect;

            time = Mathf.Max(time, lifetimeCharacterEffect.time);
        }

        public override string ToString()
        {
            return base.ToString() + " - " + time;
        }
    }
} 
