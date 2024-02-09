using System;
using CharacterSystem.Objects;
using UnityEngine;
using UnityEngine.VFX;

namespace CharacterSystem.DamageMath
{
    public interface IDamagable 
    {
        Transform transform { get; }
        GameObject gameObject { get; }

        float health { get; set; }

        float stunlock { get; set; }

        VisualEffect OnHitEffect { get; }

        void Hit(Damage damage);
        void Heal(float value);
        void Push(Vector3 direction);

        void Kill();
    }

    [Serializable]
    public struct Damage
    {
        public float Value;

        [NonSerialized]
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
}