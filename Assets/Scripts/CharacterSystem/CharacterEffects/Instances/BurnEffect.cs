
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class BurnEffect : CharacterEffect
{
    public override bool Existance => time > 0;

    [SerializeField, Range(0, 120)]
    public float time = 0;

    [SerializeField, Range(0, 120)]
    public float damagePerSecond = 5;

    public BurnEffect() { }
    public BurnEffect(float Time, float damagePerSecond)
    {
       time = Time;
       this.damagePerSecond = damagePerSecond;
    }

    public override void Start()
    {
        
    }
    public override void Update()
    {
        time -= Time.fixedDeltaTime;

        Damage.Deliver(
            effectsHolder.character, 
            new Damage(
                damagePerSecond * Time.fixedDeltaTime, 
                effectsSource, 
                0, 
                Vector3.zero, 
                Damage.Type.Effect));
    }
    public override void Remove()
    {
        
    }

    public override void AddDublicate(CharacterEffect effect)
    {
        time = ((BurnEffect)effect).time;
        damagePerSecond += ((BurnEffect)effect).damagePerSecond;
    }
}