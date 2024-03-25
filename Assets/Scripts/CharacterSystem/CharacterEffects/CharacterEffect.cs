using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public abstract class CharacterEffect : INetworkSerializable
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

    public CharacterEffectsHolder effectsHolder { get; internal set; }
    
    public abstract bool Existance { get; }

    public CharacterEffect() { }

    public abstract void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter;

    public virtual void Start() { }
    public virtual void Update() { }
    public virtual void Remove() { }
}