using System;
using CharacterSystem.Objects;
using UnityEngine;
using UnityEngine.VFX;

namespace CharacterSystem.DamageMath
{
    [Serializable]
    public struct Damage
    {
        public enum DamageType : byte
        {
            Physics,
            Balistic,
            Magical,
        }

        public static void Deliver(GameObject gameObject, Damage damage)
        {
            if (!gameObject.TryGetComponent<IDamagable>(out var target)) 
                return;

            bool isBlocked = false;

            if (damage.value > 0)
            {
                isBlocked = target.Hit(damage);
            }
            else
            {
                isBlocked = target.Heal(damage);
            }

            if (!isBlocked)
            {
                target.Push(damage.pushDirection);
                target.stunlock = Mathf.Max(target.stunlock, damage.stunlock);
            }
        }

        [SerializeField, Range(-300, 300)]
        public float value;

        [SerializeField]
        public DamageType type;

        [SerializeField, Range(0, 10)]
        public float stunlock;

        [SerializeField, Range(0, 1200)]
        public Vector3 pushDirection;

        [NonSerialized]
        public GameObject sender;


        public Damage(float value, GameObject sender, float stunlock, Vector3 pushDirection, DamageType type)
        {
            this.value = value;
            this.sender = sender;
            this.stunlock = stunlock;
            this.pushDirection = pushDirection;
            this.type = type;
        }

        public override string ToString()
        {
            return $"Damage: {value}\n Sender: {sender.name}\n Stunlock: {stunlock}\n Push Force: {pushDirection}";
        }

        public static Damage operator * (Damage damage, float multipliyer)
        {
            damage.value *= multipliyer;
            damage.stunlock *= multipliyer;
            damage.pushDirection *= multipliyer;

            return damage;
        }
        public static Damage operator / (Damage damage, float multipliyer)
        {
            damage.value /= multipliyer;
            damage.stunlock /= multipliyer;
            damage.pushDirection /= multipliyer;

            return damage;
        }
    }
}