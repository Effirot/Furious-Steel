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

public struct Damage
{
    public float Value;

    public override string ToString()
    {
        return $"Damage: {Value}";
    }
}