

using Unity.Netcode;
using UnityEngine;

public class SpeedBoostEffect : CharacterEffect
{
    public override bool Existance => time > 0;

    private float time = 0;

    public SpeedBoostEffect() { }
    public SpeedBoostEffect(float Time)
    {
       time = Time;
    }

    public override void Start()
    {
        effectsHolder.character.Speed += 10;
    }
    public override void Remove()
    {
        effectsHolder.character.Speed -= 10;
    }
    public override void Update()
    {
        time -= Time.fixedDeltaTime;
    }

    public override void NetworkSerialize<T>(BufferSerializer<T> serializer)
    {
        
    }

    public override string ToString()
    {
        return base.ToString() + " - " + time;
    }
}