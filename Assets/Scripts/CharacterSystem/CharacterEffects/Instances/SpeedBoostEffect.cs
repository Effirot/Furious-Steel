
using UnityEngine;
using UnityEngine.VFX;

namespace CharacterSystem.Effects
{
    [System.Serializable]
    public class SpeedBoostEffect : CharacterEffect
    {
        private static VisualEffectAsset visualEffectAsset;
        
        public override bool Existance => time > 0;

        [SerializeField, Range(0, 120)]
        private float time = 0;

        [SerializeField, Range(0, 120)]
        private float force = 5;

        [SerializeField, ColorUsageAttribute(false, true)]
        private Color color = Color.blue * 3;

        private VisualEffect visualEffect;

        public SpeedBoostEffect() { }
        public SpeedBoostEffect(float Time, float Force)
        {
            time = Time;
            force = Force;
        }

        public override void Start()
        {
            effectsHolder.character.Speed += force;

            effectsHolder.AddGlowing(this, color, 1);
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


        private void AddVisualEffect()
        {
            visualEffectAsset ??= Resources.Load<VisualEffectAsset>("Effects/VFX/FlameBurn");

            var visualEffectObject = new GameObject("FlameEffect"); 
            visualEffectObject.transform.SetParent(effectsHolder.transform, false);

            visualEffect = visualEffectObject.AddComponent<VisualEffect>();
            visualEffect.visualEffectAsset = visualEffectAsset;
            visualEffect.SetSkinnedMeshRenderer("SkinnedMeshRenderer", effectsHolder.characterSkinnedMeshRenderer);
            visualEffect.SetVector4("Color", color);
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


        public override string ToString()
        {
            return base.ToString() + " - " + time;
        }
    }
}
