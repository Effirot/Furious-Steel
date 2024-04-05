using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

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

    public bool IsServer => effectsHolder.IsServer;
    public bool IsClient => effectsHolder.IsClient;
    public bool IsHost => effectsHolder.IsHost;

    public CharacterEffectsHolder effectsHolder { get; internal set; }
    
    public abstract bool Existance { get; }

    public CharacterEffect() { }

    public virtual void Start() { }
    public virtual void Update() { }
    public virtual void Remove() { }

    public override string ToString()
    {
        return GetType().Name;
    }
}