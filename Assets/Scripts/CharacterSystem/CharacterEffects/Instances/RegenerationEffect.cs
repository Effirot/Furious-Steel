
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

namespace CharacterSystem.Effects
{
    [System.Serializable]
    public class RegenerationEffect : LifetimeCharacterEffect
    {
        private static VisualEffectAsset visualEffectAsset;
        
        [SerializeField, Range(0, 20)]
        public float healthPerSecond = 5;

        [SerializeField, ColorUsageAttribute(false, true)]
        public Color color = new Color(0.09f, 0.7f, 0.15f) * 1.1f;

        private VisualEffect visualEffect;
        
        public RegenerationEffect() : this(1, 1) { }
        public RegenerationEffect(float time, float healthPerSecond) : base(time)
        {
            this.healthPerSecond = healthPerSecond;
        }

        public override void Start()
        {
            effectsHolder.AddGlowing(this, color, 5f);
            
            var heal = new Damage(20, effectsSource, 0, Vector3.zero, Damage.Type.Effect);
            effectsHolder.character.Heal(ref heal);

            AddVisualEffect();
        }
        public override void Update()
        {
            base.Update();

            var heal = new Damage(healthPerSecond * Time.fixedDeltaTime, effectsSource, 0, Vector3.zero, Damage.Type.Effect);

            effectsHolder.character.Heal(ref heal);
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
            visualEffect.SetVector4("Color", color);
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
            base.AddDublicate(effect);

            var burn = (RegenerationEffect)effect;

            healthPerSecond = Mathf.Max(healthPerSecond, burn.healthPerSecond);

            effectsHolder.EditGlowing(this, burn.color);
        }
    }
}