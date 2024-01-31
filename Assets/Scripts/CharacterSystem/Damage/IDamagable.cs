using System;
using UnityEngine;
using UnityEngine.VFX;

public interface IDamagable 
{
    Transform transform { get; }

    float Health { get; set; }

    float Stunlock { get; set; }

    VisualEffect OnHitEffect { get; }

    void SendDamage(Damage damage);
}

[Serializable]
public struct Damage
{
    public float Value;

    public NetworkCharacter Sender;

    public float Stunlock;

    [Range(0, 1200)]
    public float PushForce;

    public Damage(float value, NetworkCharacter sender, float stunlock, float pushForce)
    {
        Value = value;
        Sender = sender;
        Stunlock = stunlock;
        PushForce = pushForce;
    }

    public override string ToString()
    {
        return $"Damage: {Value}\n Sender: {Sender.name}\n Stunlock: {Stunlock}\n Push Force: {PushForce}";
    }

    public static Damage operator * (Damage damage, float multipliyer)
    {
        damage.Value *= multipliyer;
        damage.Stunlock *= multipliyer;
        damage.PushForce *= multipliyer;

        return damage;
    }
}