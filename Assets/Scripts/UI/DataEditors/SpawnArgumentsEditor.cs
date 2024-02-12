
using UnityEngine;
using static RoomManager;

using static RoomManager.SpawnArguments;


public sealed class SpawnArgumentsEditor : MonoBehaviour
{
    public void SetCharacter(int index)
    {
        SpawnArguments.This.CharacterIndex = index;
    }

    public void SetWeapon(int index)
    {
        SpawnArguments.This.WeaponIndex = index;
    }

    public void SetColor(int index)
    {
        SpawnArguments.This.ColorScheme = (CharacterColorScheme)index;
    }
}