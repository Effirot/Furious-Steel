
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.VisualScripting;
using UnityEngine;

namespace CharacterSystem.Effects
{
    [System.Serializable]
    public class RegenerationEffect : CharacterEffect
    {
        public override bool Existance => time > 0 && !Team.IsAlly(effectsSource, effectsHolder.character);

        [SerializeField, Range(0, 120)]
        public float time = 0;

        [SerializeField, Range(0, 20)]
        public float healthPerSecond = 5;

        [SerializeField, ColorUsageAttribute(false, true)]
        public Color color = new Color(0.09f, 0.7f, 0.15f) * 1.1f;

        public RegenerationEffect() { }
        public RegenerationEffect(float Time, float healthPerSecond)
        {
        time = Time;
        this.healthPerSecond = healthPerSecond;
        }

        public override void Start()
        {
            effectsHolder.AddGlowing(this, color, 5f);
            effectsHolder.character.Heal(new Damage(20, effectsSource, 0, Vector3.zero, Damage.Type.Effect));
        }
        public override void Update()
        {
            time -= Time.fixedDeltaTime;

            effectsHolder.character.Heal(new Damage(healthPerSecond * Time.fixedDeltaTime, effectsSource, 0, Vector3.zero, Damage.Type.Effect));
        }
        public override void Remove()
        {
            
        }

        public override void AddDublicate(CharacterEffect effect)
        {
            var burn = (RegenerationEffect)effect;

            time = burn.time;
            healthPerSecond = Mathf.Max(healthPerSecond, burn.healthPerSecond);

            effectsHolder.EditGlowing(this, burn.color);
        }
    }
}