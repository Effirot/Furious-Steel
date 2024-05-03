
using CharacterSystem.DamageMath;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class BurnEffect : CharacterEffect
{
    public override bool Existance => time > 0;

    [SerializeField, Range(0, 120)]
    private float time = 0;
    [SerializeField, Range(0, 120)]
    private float damage = 10;

    public BurnEffect() { }
    public BurnEffect(float Time, float Damage)
    {
       time = Time;
    }

    public override void Start()
    {
        
    }
    public override void Update()
    {
        Damage.Deliver(effectsHolder.character, new Damage(damage * Time.fixedDeltaTime, effectsSource, 0, Vector3.zero, Damage.Type.Effect));
    }
    public override void Remove()
    {
        
    }
}