
using UnityEngine;
using static RoomManager;

using static RoomManager.SpawnArguments;


public sealed class SpawnArgumentsEditor : MonoBehaviour
{
    public void SetCharacter(string Name)
    {
        SpawnArguments.This.CharacterName = Name;
    }

    public void SetWeapon(string Name)
    {
        SpawnArguments.This.WeaponName = Name;
    }

    public void SetColor(int index)
    {
        SpawnArguments.This.ColorScheme = (CharacterColorScheme)index;
    }
}