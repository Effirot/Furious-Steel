using System;
using CharacterSystem.Attacks;
using CharacterSystem.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace CharacterSystem.DamageMath
{
    [Serializable]
    public struct Damage : INetworkSerializable
    {
        public enum Type : byte
        {
            Physics = 1,
            Balistic = 2,
            Magical = 4,

            Unblockable = 8,
            Parrying = 16,
        }

        public static DamageDeliveryReport Deliver(GameObject gameObject, Damage damage)
        {
            if (!gameObject.TryGetComponent<IDamagable>(out var target)) 
                return new();

            return Deliver(target, damage);
        }
        public static DamageDeliveryReport Deliver(IDamagable target, Damage damage)
        {
            var report = new DamageDeliveryReport();

            if (target == null || target == damage.sender)
                return report;

            if (ITeammate.IsAlly(target, damage.sender))
                return report;

            report.target = target;
            report.damage = damage;
            report.isDelivered = true;
            
            if (damage.value >= 0)
            {
                report.isBlocked = target.Hit(damage);
                report.isLethal = target.health <= 0;
            }         
            else
            {
                report.isBlocked = target.Heal(damage);
            }

            damage.sender?.DamageDelivered(report);
            

            return report;
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
            return $"Sender: {sender?.gameObject?.name ?? "null"}\n Damage: {value}\n Type: {type}\n Stunlock: {stunlock}\n Push Force: {pushDirection}\n UltimateRecharged {RechargeUltimate}";
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref value);
            serializer.SerializeValue(ref type);
            serializer.SerializeValue(ref stunlock);
            serializer.SerializeValue(ref pushDirection);
            serializer.SerializeValue(ref RechargeUltimate);

            var objectID = sender?.gameObject?.GetComponent<NetworkObject>()?.NetworkObjectId ?? ulong.MinValue;
            serializer.SerializeValue(ref objectID);

            if (serializer.IsReader)
            {
                if (objectID != ulong.MinValue)
                {
                    var dictionary = NetworkManager.Singleton.SpawnManager.SpawnedObjects;

                    if (dictionary.ContainsKey(objectID) && dictionary[objectID].TryGetComponent<IDamageSource>(out var component))
                    {
                        sender = component;
                    }
                }
            }
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