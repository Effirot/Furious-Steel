using UnityEngine;

namespace CharacterSystem.Effects
{
    [System.Serializable]
    public class SlownessEffect : LifetimeCharacterEffect
    {
        [SerializeField, Range(0, 10)]
        private float force = 5;

        public SlownessEffect() : this(1, 1) { }
        public SlownessEffect(float time, float force) : base(time)
        {
            this.force = force;
        }

        public override void Start()
        {
            effectsHolder.character.Speed -= force;
        }
        public override void Remove()
        {
            effectsHolder.character.Speed += force;
        }
    }
}