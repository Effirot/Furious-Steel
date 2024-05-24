
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class BurnEffect : CharacterEffect
{
    public override bool Existance => time > 0;

    [SerializeField, Range(0, 120)]
    public float time = 0;

    [SerializeField, Range(0, 120)]
    public float damagePerSecond = 5;

    [SerializeField, ColorUsageAttribute(false, true)]
    public Color color = Color.yellow * 5;

    public BurnEffect() { }
    public BurnEffect(float Time, float damagePerSecond)
    {
       time = Time;
       this.damagePerSecond = damagePerSecond;
    }

    public override void Start()
    {
        effectsHolder.AddGlowing(this, color, 1);
    }
    public override void Update()
    {
        time -= Time.fixedDeltaTime;

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
        
    }

    public override void AddDublicate(CharacterEffect effect)
    {
        time = ((BurnEffect)effect).time;
        damagePerSecond = Mathf.Max(damagePerSecond, ((BurnEffect)effect).damagePerSecond);
    }
}