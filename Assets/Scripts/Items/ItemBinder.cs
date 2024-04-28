

using System.Text;
using Effiry.Items;
using Unity.Netcode;
using UnityEngine;

public class ItemBinder : NetworkBehaviour
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

    private void Awake()
    {
        meshRenderers = GetComponentsInChildren<Renderer>();
    }

    [ClientRpc]
    private void OnItemChanged_ClientRpc(string Json)
    {
        if (!IsServer)
        {
            item = Item.FromJsonString(Json);
        }
    }
}