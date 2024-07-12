using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem.Attacks;
using CharacterSystem.DamageMath;
using UnityEngine;
using UnityEngine.VFX;

namespace CharacterSystem.Effects
{
    [Serializable]
    public class DamageBoostEffect : LifetimeCharacterEffect
    {
        private static VisualEffectAsset visualEffectAsset;

        public override bool Existance => base.Existance && effectsHolder.character is IDamageSource;

        [SerializeField, Range(1, 5)]
        private float force = 2f; 

        [SerializeField, ColorUsageAttribute(false, true)]
        public Color color = new Color(1f, 0.1f, 0.1f) * 30f;
        
        private VisualEffect visualEffect;

        public DamageBoostEffect() : this(1, 1) { }
        public DamageBoostEffect(float time, float force) : base(time)
        {
            this.force = force;
        }

        public override void Start() 
        { 
            AddVisualEffect();

            if (effectsHolder.character is IDamageSource)
            {
                var source = effectsHolder.character as IDamageSource;

                source.DamageMultipliyer += force - 1;
            }

            effectsHolder.AddGlowing(this, color, 7f);
        }
        public override void Remove() 
        {
            RemoveVisualEffect();

            if (effectsHolder.character is IDamageSource)
            {
                var source = effectsHolder.character as IDamageSource;

                source.DamageMultipliyer -= force - 1;
            }
        }

        private void AddVisualEffect()
        {
            visualEffectAsset ??= Resources.Load<VisualEffectAsset>("Effects/VFX/DamageBoostEffect");

            var visualEffectObject = new GameObject("DamageBoostEffect"); 
            visualEffectObject.transform.SetParent(effectsHolder.transform, false);
            visualEffectObject.transform.localScale = Vector3.one / 2f;
            visualEffectObject.transform.position += Vector3.up * 1.5f;

            visualEffect = visualEffectObject.AddComponent<VisualEffect>();
            visualEffect.visualEffectAsset = visualEffectAsset;
            visualEffect.SetVector4("Color", color);
            visualEffect.Play();
        }
        private void RemoveVisualEffect()
        {
            if (visualEffect != null)
            {
                visualEffect.Stop();
                visualEffect.SendEvent("OnBurst");

                GameObject.Destroy(visualEffect.gameObject, 3);

                visualEffect = null;
            }
        }

        public override void AddDublicate(CharacterEffect effect) 
        { 
            base.AddDublicate(effect);
        }
    }
}
