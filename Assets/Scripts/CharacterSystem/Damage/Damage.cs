using System;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Attacks;
using CharacterSystem.Effects;
using CharacterSystem.Objects;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

namespace CharacterSystem.DamageMath
{
    [Serializable]
    public struct Damage : IEquatable<Damage>
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

        public enum DamageArgument : byte
        {
            DETONATE_BURN,
            
            COLD,
            HEAT,

            WALL_HIT
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

                        report.isBlocked = target.Hit(ref damage);
                        report.isDelivered = true;

                        target.lastRecievedDamage = damage;
                    }
                }
                else
                {
                    report.isBlocked = target.Heal(ref damage);
                    report.isDelivered = true;
                }
 
                if (report.isDelivered)
                {
                    DeliverEffects();
                }

                report.isLethal = target.health <= 0;


            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            damageDeliveryPipeline?.Invoke(report);
                
            if (report.isLethal)
            {
                target.Kill(damage);
            }

            return report;

            bool DeliverEffects()
            {
                if (target.gameObject.TryGetComponent<CharacterEffectsHolder>(out var effectHolder) && damage.Effects != null)
                {
                    List<CharacterEffect> addedEffect = new();

                    foreach (var effect in damage.Effects)
                    {
                        if (effect == null)
                            continue;
                            
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
        
        [SerializeField]
        public DamageArgument[] args; 

        [NonSerialized]
        public uint senderID;
         
        public IAttackSource sender { 
            get {
                var dictionary = NetworkClient.spawned;

                if (dictionary.ContainsKey(senderID) && dictionary[senderID].TryGetComponent<IAttackSource>(out var component))
                {
                    return component;
                }

                return null;
            }
            set{
                if (value.IsUnityNull())
                {
                    senderID = uint.MinValue; 
                    return;
                }

                var gObj = value.gameObject;

                if (gObj.IsUnityNull())
                {
                    senderID = uint.MinValue; 
                    return;
                }

                if (gObj.TryGetComponent<NetworkIdentity>(out var netObj))
                    senderID = netObj.netId;
            } 
        }

        public Damage(float value, uint senderID, float stunlock, Vector3 pushDirection, Type type, params CharacterEffect[] effects)
        {
            this.value = value;
            this.senderID = senderID;
            this.stunlock = stunlock;
            this.pushDirection = pushDirection;
            this.type = type;
            this.Effects = effects;
            this.RechargeUltimate = true;   
            this.SelfDamageUltimate = false;   
            this.args = new DamageArgument[0];
        }
        public Damage(float value, IAttackSource sender, float stunlock, Vector3 pushDirection, Type type, params CharacterEffect[] effects)
        {
            senderID = 0;

            this.value = value;
            this.stunlock = stunlock;
            this.pushDirection = pushDirection;
            this.type = type;
            this.Effects = effects;
            this.RechargeUltimate = true;
            this.SelfDamageUltimate = false;   
            this.args = new DamageArgument[0];
            
            this.sender = sender;

        }
        public Damage(params CharacterEffect[] effects) : this (0, null, 0, Vector3.zero, Type.Effect, effects) { }

        public override string ToString()
        {
            return $"Sender: {sender?.gameObject?.name ?? "null"}\n Damage: {value}\n Type: {type}\n Stunlock: {stunlock}\n Push Force: {pushDirection}\n UltimateRecharged {RechargeUltimate}";
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
    public class DamageDeliveryReport : IDisposable, IEquatable<DamageDeliveryReport>
    {
        public Damage damage = new Damage();

        public bool isDelivered = false;
        public bool isBlocked = false;
        public bool isLethal = false;

        public DateTime time = DateTime.Now;

        public CharacterEffect[] RecievedEffects;
        
        public uint targetID;

        public IDamagable target {
            get {
                var dictionary = NetworkClient.spawned;

                if (dictionary.ContainsKey(targetID) && dictionary[targetID].TryGetComponent<IAttackSource>(out var component))
                {
                    return component;
                }

                return null;
            }
            set{
                if (!value.IsUnityNull() && value.gameObject.TryGetComponent<NetworkIdentity>(out var identity))
                {
                    targetID = identity.netId;
                }
                else
                {
                    targetID = uint.MinValue;
                }
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
                other.targetID == targetID;
        }

        public override string ToString()
        {
            return $"{damage}\nTarget: {target?.gameObject.ToSafeString() ?? "null"} ({targetID})\nIsDelivered: {isDelivered}\nIsBlocked: {isBlocked}\nIsLethal: {isLethal}\nTime: {time}\nEffects: {RecievedEffects}";
        }
    }
}