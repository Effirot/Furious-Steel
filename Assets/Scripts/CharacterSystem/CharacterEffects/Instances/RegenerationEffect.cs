
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

namespace CharacterSystem.Effects
{
    [System.Serializable]
    public class RegenerationEffect : CharacterEffect
    {
        private static VisualEffectAsset visualEffectAsset;
        
        public override bool Existance => time > 0 && !Team.IsAlly(effectsSource, effectsHolder.character);

        [SerializeField, Range(0, 120)]
        public float time = 0;

        [SerializeField, Range(0, 20)]
        public float healthPerSecond = 5;

        [SerializeField, ColorUsageAttribute(false, true)]
        public Color color = new Color(0.09f, 0.7f, 0.15f) * 1.1f;

        private VisualEffect visualEffect;
        
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

            AddVisualEffect();
        }
        public override void Update()
        {
            time -= Time.fixedDeltaTime;

            effectsHolder.character.Heal(new Damage(healthPerSecond * Time.fixedDeltaTime, effectsSource, 0, Vector3.zero, Damage.Type.Effect));
        }
        public override void Remove()
        {
            RemoveVisualEffect();
        }


        private void AddVisualEffect()
        {
            visualEffectAsset ??= Resources.Load<VisualEffectAsset>("Effects/VFX/HealingEffect");

            var visualEffectObject = new GameObject("HealingEffect"); 
            visualEffectObject.transform.SetParent(effectsHolder.transform, false);

            visualEffect = visualEffectObject.AddComponent<VisualEffect>();
            visualEffect.visualEffectAsset = visualEffectAsset;
            visualEffect.SetSkinnedMeshRenderer("SkinnedMeshRenderer", effectsHolder.characterSkinnedMeshRenderer);
            visualEffect.SetVector4("Color", color * 2f);
            visualEffect.SendEvent("OnConstant");
        }
        private void RemoveVisualEffect()
        {
            if (visualEffect != null)
            {
                visualEffect.Stop();

                GameObject.Destroy(visualEffect.gameObject, 3);

                visualEffect = null;
            }
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