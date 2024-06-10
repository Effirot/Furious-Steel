

using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class SpeedBoostEffect : CharacterEffect
{
    public override bool Existance => time > 0;

    [SerializeField, Range(0, 120)]
    private float time = 0;

    [SerializeField, Range(0, 120)]
    private float force = 5;

    public SpeedBoostEffect() { }
    public SpeedBoostEffect(float Time, float Force)
    {
        time = Time;
        force = Force;
    }

    public override void Start()
    {
        effectsHolder.character.CurrentSpeed += force;

        effectsHolder.AddGlowing(this, Color.blue * 3, 1);
    }
    public override void Remove()
    {
        effectsHolder.character.CurrentSpeed -= force;
    }
    public override void Update()
    {
        if (IsServer)
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
        effectsHolder.character.CurrentSpeed += deltaForce;
        time = Mathf.Max(time, speedEffect.time);
    }

    public override string ToString()
    {
        return base.ToString() + " - " + time;
    }
}