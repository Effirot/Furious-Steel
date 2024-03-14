using System;
using CharacterSystem.Attacks;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace CharacterSystem.DamageMath
{
    [Serializable]
    public struct Damage
    {
        public enum Type : byte
        {
            Physics = 1,
            Balistic = 2,
            Magical = 4,

            Unblockable = 8,
            Parrying = 16,
        }

        public static void Deliver(GameObject gameObject, Damage damage)
        {
            if (!gameObject.TryGetComponent<IDamagable>(out var target)) 
                return;

            Deliver(target, damage);
        }
        public static void Deliver(IDamagable target, Damage damage)
        {
            if (target == null)
                return;

            if (ITeammate.IsAlly(target, damage.sender))
                return;

            var report = new DamageDeliveryReport();

            report.target = target;
            report.damage = damage;
            report.isDelivered = true;
            
            if (damage.value > 0)
            {
                report.isBlocked = target.Hit(damage);
                report.isLethal = target.health <= 0;
            }
            
            if (damage.value < 0)
            {
                report.isBlocked = target.Heal(damage);
            }

            if (!report.isBlocked)
            {
                target.Push(damage.pushDirection);
                target.stunlock = Mathf.Max(target.stunlock, damage.stunlock);
            }

            if (damage.sender != null)
            {
                using (report)
                {
                    damage.sender?.DamageDelivered(report);
                }
            }
        }

        [SerializeField, Range(-50, 300)]
        public float value;

        [SerializeField]
        public Type type;

        [SerializeField, Range(0, 10)]
        public float stunlock;

        [SerializeField]
        public Vector3 pushDirection;

        [SerializeField]
        public bool RechargeUltimate;

        [NonSerialized]
        public IDamageSource sender;


        public Damage(float value, IDamageSource sender, float stunlock, Vector3 pushDirection, Type type)
        {
            this.value = value;
            this.sender = sender;
            this.stunlock = stunlock;
            this.pushDirection = pushDirection;
            this.type = type;
            this.RechargeUltimate = true;
        }

        public override string ToString()
        {
            return $"Sender: {sender.gameObject.name}\n Damage: {value}\n Type: {type}\n Stunlock: {stunlock}\n Push Force: {pushDirection}\n UltimateRecharged {RechargeUltimate}";
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

    public class DamageDeliveryReport : IDisposable
    {
        public Damage damage = new Damage();

        public bool isDelivered = false;
        public bool isBlocked = false;
        public bool isLethal = false;

        public IDamagable target = null;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}