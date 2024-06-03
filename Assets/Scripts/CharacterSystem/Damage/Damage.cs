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
    public struct Damage : INetworkSerializable,
        IEquatable<Damage>
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
                return new();
            }

            return Deliver(target, damage);
        }
        public static DamageDeliveryReport Deliver(Transform transform, Damage damage)
        {
            if (!transform.gameObject.TryGetComponent<IDamagable>(out var target)) 
            {
                return new();
            }

            return Deliver(target, damage);
        }
        public static DamageDeliveryReport Deliver(IDamagable target, Damage damage)
        {
            var report = new DamageDeliveryReport();
            report.target = target;
            report.damage = damage;

            if (target.IsUnityNull())
                return report;
                
            if (target.health <= 0)
                return report;
            
            try {
                if (damage.value >= 0)
                {
                    if (damage.sender != target || damage.SelfDamageUltimate)
                    {
                        if (damage.sender.IsUnityNull() && !target.lastRecievedDamage.sender.IsUnityNull())
                        {
                            damage.sender = target.lastRecievedDamage.sender;
                        }

                        report.isBlocked = target.Hit(damage);
                        report.isDelivered = true;

                        target.lastRecievedDamage = damage;
                    }
                }
                else
                {
                    report.isBlocked = target.Heal(damage);
                    report.isDelivered = true;
                }
 
                if (!report.isBlocked && report.isDelivered)
                {
                    DeliverEffects();
                }

                report.isLethal = target.health <= 0;

                if (report.isLethal)
                {
                    target.Kill();
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
                        effectClone.effectsSource = damage.sender;

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

        [SerializeField]
        public bool SelfDamageUltimate;

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
                
                if (value.IsUnityNull())
                {
                    senderID = ulong.MinValue; 
                    return;
                }

                var gObj = value.gameObject;

                if (gObj.IsUnityNull())
                {
                    senderID = ulong.MinValue; 
                    return;
                }

                if (gObj.TryGetComponent<NetworkObject>(out var netObj))
                    senderID = netObj.NetworkObjectId;
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
            this.SelfDamageUltimate = false;   
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
            this.SelfDamageUltimate = false;   
            
            this.sender = sender;
        }
        public Damage(params CharacterEffect[] effects) : this (0, null, 0, Vector3.zero, Type.Effect, effects) { }

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
        public bool Equals(Damage other)
        {
            return other.value == value &&
                other.type == type &&
                other.stunlock == stunlock &&
                other.pushDirection == pushDirection &&
                other.RechargeUltimate == RechargeUltimate &&

                other.senderID == senderID;
        }

        public static bool operator == (Damage damage1, Damage damage2)
        {
            return damage1.Equals(damage2);
        }
        public static bool operator != (Damage damage1, Damage damage2)
        {
            return !damage1.Equals(damage2);
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

        public override bool Equals(object o)
        {
            if (o is Damage)
            {
                return (Damage)o == this;
            }
            
            return false;
        }
        public override readonly int GetHashCode() => 1;
    }
    
    [Serializable]
    public class DamageDeliveryReport : 
        IDisposable, 
        INetworkSerializable,
        IEquatable<DamageDeliveryReport>
    {
        public Damage damage = new Damage();

        public bool isDelivered = false;
        public bool isBlocked = false;
        public bool isLethal = false;

        public DateTime time = DateTime.Now;

        public CharacterEffect[] RecievedEffects;
        
        public ulong TargetID;

        public IDamagable target {
            get {
                if (TargetID != ulong.MinValue)
                {
                    var dictionary = NetworkManager.Singleton.SpawnManager.SpawnedObjects;

                    if (dictionary.ContainsKey(TargetID) && dictionary[TargetID].TryGetComponent<IDamageSource>(out var component))
                    {
                        return component;
                    }
                }

                return null;
            }
            set{
                TargetID = value?.gameObject?.GetComponent<NetworkObject>()?.NetworkObjectId ?? ulong.MinValue;
            } 
        }


        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public bool Equals(DamageDeliveryReport other)
        {
            return other.damage == damage &&
                other.isDelivered == isDelivered &&
                other.isBlocked == isBlocked &&
                other.isLethal == isLethal &&
                other.time == time &&
                other.TargetID == TargetID;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref damage);
            serializer.SerializeValue(ref isDelivered);
            serializer.SerializeValue(ref isBlocked);
            serializer.SerializeValue(ref isLethal);
            serializer.SerializeValue(ref time);
            serializer.SerializeValue(ref TargetID);
        }
    }
}