using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Objects;
using UnityEngine;

public abstract class PowerUp
{
    public static PowerUp[] AllPowerUps = GetAllTypes();

    public static int GetTypeID<T>() where T : PowerUp
    {
        return Array.IndexOf(AllPowerUps, typeof(T));
    }

    private static PowerUp[] GetAllTypes()
    {
        var types = typeof(PowerUp).Assembly.GetTypes();

        var array = from type in types
            where type.IsSubclassOf(typeof(PowerUp)) && !type.IsAbstract
            select Activator.CreateInstance(type) as PowerUp;

        return array.ToArray();
    }

    protected PowerUp() { }

    public abstract GameObject prefab  { get; }

    public abstract void Activate(NetworkCharacter character);
}
