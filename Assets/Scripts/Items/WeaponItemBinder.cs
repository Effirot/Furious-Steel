

using Effiry.Items;
using Unity.Netcode;
using UnityEngine;

public class WeaponItemBinder : NetworkBehaviour
{

    public Item item
    {
        get => _item;
        set
        {
            _item = value;
            
            if (IsServer)
            {
                OnItemChanged_ClientRpc(Item.ToJsonString(value));
            }
        }
    }

    private Item _item;
    
    private Renderer[] meshRenderers; 

    private void OnItemChanged_ClientRpc(string Json)
    {
        item = Item.FromJsonString(Json);
    }

    private void Awake()
    {
        meshRenderers = GetComponentsInChildren<Renderer>();
    }
}