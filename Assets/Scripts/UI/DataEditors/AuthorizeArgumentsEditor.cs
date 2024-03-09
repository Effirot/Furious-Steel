
using UnityEngine;
using static RoomManager;

using static RoomManager.SpawnArguments;


public sealed class AuthorizeArgumentsEditor : MonoBehaviour
{
    public void SetName(string Name)
    {
        Authorizer.localAuthorizeArgs.Name = Name;
    }
}