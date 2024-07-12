

using CharacterSystem.DamageMath;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

namespace CharacterSystem.Effects
{
    [System.Serializable]
    public class BurnEffect : LifetimeCharacterEffect
    {
        private static VisualEffectAsset visualEffectAsset;

        [SerializeField, Range(0, 20)]
        public float damagePerSecond = 5;

        [SerializeField, ColorUsageAttribute(false, true)]
        public Color color = new Color(0.74f, 0.1f, 0) * 4;

        private VisualEffect visualEffect;

        public BurnEffect() : this(1, 1) { }
        public BurnEffect(float time, float damagePerSecond) : base(time)
        {
            this.damagePerSecond = damagePerSecond;
        }

        public override void Start()
        {
            effectsHolder.AddGlowing(this, color, 1);

            AddVisualEffect();
        }
        public override void Update()
        {
            base.Update();
            
            var report = 
                Damage.Deliver(
                    effectsHolder.character, 
                    new Damage(
                        damagePerSecond * Time.fixedDeltaTime, 
                        effectsSource, 
                        0, 
                        Vector3.zero, 
                        Damage.Type.Effect));

            if (!effectsSource.IsUnityNull())
            {
                effectsSource.DamageDelivered(report);
            }
        }
        public override void Remove()
        {
            RemoveVisualEffect();
        }

        private void AddVisualEffect()
        {
            visualEffectAsset ??= Resources.Load<VisualEffectAsset>("Effects/VFX/FlameBurn");

            var visualEffectObject = new GameObject("FlameEffect"); 
            visualEffectObject.transform.SetParent(effectsHolder.transform, false);

            visualEffect = visualEffectObject.AddComponent<VisualEffect>();
            visualEffect.visualEffectAsset = visualEffectAsset;
            visualEffect.SetSkinnedMeshRenderer("SkinnedMeshRenderer", effectsHolder.characterSkinnedMeshRenderer);
            visualEffect.SetVector4("Color", color * 6.7f);
            visualEffect.Play();
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

            var burn = (BurnEffect)effect;

            damagePerSecond = Mathf.Max(damagePerSecond, burn.damagePerSecond);

            visualEffect.SetVector4("Color", burn.color * 6.7f);

            effectsHolder.EditGlowing(this, burn.color);
        }
    }
}