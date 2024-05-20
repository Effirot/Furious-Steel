using System;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Attacks;
using CharacterSystem.Objects;
using Unity.Netcode;
using Unity.VisualScripting;
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

            Effect = 32,
        }

        public delegate void OnDamageDeliveredDelegate(DamageDeliveryReport damage);
        
        public static event OnDamageDeliveredDelegate damageDeliveryPipeline;


        public static DamageDeliveryReport Deliver(GameObject gameObject, Damage damage)
        {
            if (!gameObject.TryGetComponent<IDamagable>(out var target)) 
            {
                if (gameObject.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.AddForce((damage.pushDirection + Vector3.up / 2) * 1000);
                }

                return new();
            }

            return Deliver(target, damage);
        }
        public static DamageDeliveryReport Deliver(Transform transform, Damage damage)
        {
            if (!transform.gameObject.TryGetComponent<IDamagable>(out var target)) 
            {
                if (transform.gameObject.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.AddForce((damage.pushDirection + Vector3.up / 2) * 1000);
                }

                return new();
            }

            return Deliver(target, damage);
        }
        public static DamageDeliveryReport Deliver(IDamagable target, Damage damage)
        {
            var report = new DamageDeliveryReport();
            report.target = target;
            report.damage = damage;

            if (target.IsUnityNull() || target == damage.sender)
                return report;


            if (ITeammate.IsAlly(target, damage.sender))
            {
                report.isDelivered = DeliverEffects();
                report.isDelivered = target.Heal(damage);

                if (report.isDelivered)
                {
                    damageDeliveryPipeline?.Invoke(report);
                }

                return report;
            }
            
            try {
                if (damage.value >= 0)
                {
                    report.isDelivered = true;
                    report.isBlocked = target.Hit(damage);
                    report.isLethal = target.health <= 0;

                    if (!report.isBlocked)
                    {
                        DeliverEffects();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            damageDeliveryPipeline?.Invoke(report);
            return report;

            bool DeliverEffects()
            {
                if (target.gameObject.TryGetComponent<CharacterEffectsHolder>(out var effectHolder) && damage.Effects != null)
                {
                    List<CharacterEffect> addedEffect = new();

                    foreach (var effect in damage.Effects)
                    {
                        var effectClone = effect.Clone();

                        if (effectHolder.AddEffect(effectClone))
                        {
                            addedEffect.Add(effectClone);
                        }
                    }

                    report.RecievedEffects = addedEffect.ToArray();
                    return report.RecievedEffects.Any();
                }

                return false;
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

        [SerializeField, SubclassSelector, SerializeReference]
        public CharacterEffect[] Effects;

        [NonSerialized]
        public ulong senderID;
         
        public IDamageSource sender { 
            get {
                if (senderID != ulong.MinValue)
                {
                    var dictionary = NetworkManager.Singleton.SpawnManager.SpawnedObjects;

                    if (dictionary.ContainsKey(senderID) && dictionary[senderID].TryGetComponent<IDamageSource>(out var component))
                    {
                        return component;
                    }
                }

                return null;
            }
            set{
                senderID = value?.gameObject?.GetComponent<NetworkObject>()?.NetworkObjectId ?? ulong.MinValue;
            } 
        }


        public Damage(float value, ulong senderID, float stunlock, Vector3 pushDirection, Type type, params CharacterEffect[] effects)
        {
            this.value = value;
            this.senderID = senderID;
            this.stunlock = stunlock;
            this.pushDirection = pushDirection;
            this.type = type;
            this.Effects = effects;
            this.RechargeUltimate = true;   
        }
        public Damage(float value, IDamageSource sender, float stunlock, Vector3 pushDirection, Type type, params CharacterEffect[] effects)
        {
            senderID = 0;

            this.value = value;
            this.stunlock = stunlock;
            this.pushDirection = pushDirection;
            this.type = type;
            this.Effects = effects;
            this.RechargeUltimate = true;
            
            this.sender = sender;
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

            serializer.SerializeValue(ref senderID);
        }

        public static Damage operator * (Damage damage, float multipliyer)
        {
            damage.value *= multipliyer;
            damage.stunlock *= multipliyer;
            // damage.pushDirection *= multipliyer;

            return damage;
        }
        public static Damage operator / (Damage damage, float multipliyer)
        {
            damage.value /= multipliyer;
            damage.stunlock /= multipliyer;
            // damage.pushDirection /= multipliyer;

            return damage;
        }
    }
    
    [Serializable]
    public class DamageDeliveryReport : 
        IDisposable, 
        INetworkSerializable
    {
        public Damage damage = new Damage();

        public bool isDelivered = false;
        public bool isBlocked = false;
        public bool isLethal = false;

        public IDamagable target = null;

        public DateTime time = DateTime.Now;

        public CharacterEffect[] RecievedEffects;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref damage);
            serializer.SerializeValue(ref isDelivered);
            serializer.SerializeValue(ref isBlocked);
            serializer.SerializeValue(ref isLethal);
            serializer.SerializeValue(ref time);

            var objectID = ulong.MinValue;
            if (!target.IsUnityNull() && target.gameObject.TryGetComponent<NetworkObject>(out var networkObject))
            {
                objectID = networkObject.NetworkObjectId;
            }

            serializer.SerializeValue(ref objectID);

            if (serializer.IsReader)
            {
                var dictionary = NetworkManager.Singleton.SpawnManager.SpawnedObjects;

                if (dictionary.ContainsKey(objectID))
                {
                    target = dictionary[objectID].GetComponent<IDamagable>();
                }
                else
                {
                    target = null;
                }

            }
        }
    }
}