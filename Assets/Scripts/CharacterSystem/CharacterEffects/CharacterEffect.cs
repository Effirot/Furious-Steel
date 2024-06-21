using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CharacterSystem.Attacks;
using Mirror;
using UnityEngine;

namespace CharacterSystem.Effects
{
    [System.Serializable]
    public abstract class CharacterEffect
    {
        public static Type[] AllCharacterEffectTypes { get; private set; } = GetAllTypes();

        private static Type[] GetAllTypes()
        {
            var types = typeof(CharacterEffect).Assembly.GetTypes();

            var array = from type in types
                where type.IsSubclassOf(typeof(CharacterEffect)) && !type.IsAbstract
                select type;

            return array.ToArray();
        }

        public int indexOfEffect => Array.IndexOf(AllCharacterEffectTypes, this.GetType());

        public bool isServer => effectsHolder.isServer;
        public bool isClient => effectsHolder.isClient;

        public CharacterEffectsHolder effectsHolder { get; internal set; }
        public IDamageSource effectsSource { 
            get {
                if (NetworkServer.spawned.Count > effectSourceID && effectSourceID != 0)
                {
                    return NetworkServer.spawned[effectSourceID].GetComponent<IDamageSource>();
                }

                return null;
            }
            internal set {
                effectSourceID = value?.gameObject?.GetComponent<NetworkIdentity>()?.netId ?? 0;
            } 
        }

        [HideInInspector]
        public uint effectSourceID = 0;

        public abstract bool Existance { get; }

        public bool IsValid { get; internal set; } = false;

        public CharacterEffect() { }

        public virtual void Start() { }
        public virtual void Update() { }
        public virtual void Remove() { }
        public virtual void AddDublicate(CharacterEffect effect) { }

        public override string ToString()
        {
            return GetType().Name;
        }

        public CharacterEffect Clone()
        {
            return this.MemberwiseClone() as CharacterEffect;
        }
    }

    public static class CustomCharacterEffectSerialization 
    {
        public static void WriteCharacterEffect(this NetworkWriter writer, CharacterEffect value)
        {
            if (value == null)
                return;

            writer.Write(value.indexOfEffect);
            writer.Write(value.effectSourceID);

            foreach(var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField))
            {
                writer.Write(field.GetValue(value));
            }
        }

        public static CharacterEffect ReadCharacterEffect(this NetworkReader reader)
        {
            var index = reader.Read<int>();
            var newEffectType = CharacterEffect.AllCharacterEffectTypes[index];
            var newEffectInstance = Activator.CreateInstance(newEffectType) as CharacterEffect;

            newEffectInstance.effectSourceID = reader.Read<uint>();

            foreach(var field in newEffectType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField))
            {
                field.SetValue(newEffectInstance, reader.Read<object>());
            }

            return new BurnEffect();
        }
    }
}