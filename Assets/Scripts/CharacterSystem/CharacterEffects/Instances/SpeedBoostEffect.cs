

using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class SpeedBoostEffect : CharacterEffect
{
    public override bool Existance => time > 0;

    [SerializeField, Range(0, 120)]
    private float time = 0;

    public SpeedBoostEffect() { }
    public SpeedBoostEffect(float Time)
    {
       time = Time;
    }

    public override void Start()
    {
        effectsHolder.character.Speed += 5;

        effectsHolder.AddGlowing(this, Color.blue * 3, 1);
    }
    public override void Remove()
    {
        effectsHolder.character.Speed -= 5;
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
        time = ((SpeedBoostEffect)effect).time;
    }

    public override string ToString()
    {
        return base.ToString() + " - " + time;
    }
}