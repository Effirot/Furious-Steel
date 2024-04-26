
using Effiry.Items;
using UnityEngine;
using static RoomManager;

using static RoomManager.SpawnArguments;


public sealed class SpawnArgumentsEditor : MonoBehaviour
{
    public void SetCharacter(Item item)
    {
        SetCharacter(item != null ? Item.ToJsonString(item) : "");
    }
    public void SetCharacter(string Json)
    {
        SpawnArguments.Local.CharacterItemJson = Json;
    }

    public void SetWeapon(Item item)
    {
        SetWeapon(item != null ? Item.ToJsonString(item) : "");
    }
    public void SetWeapon(string Json)
    {
        SpawnArguments.Local.WeaponItemJson = Json;
    }

    public void SetTrinket(Item item)
    {
        SetTrinket(item != null ? Item.ToJsonString(item) : "");
    }
    public void SetTrinket(string Json)
    {
        SpawnArguments.Local.TrinketItemJson = Json;
    }
}