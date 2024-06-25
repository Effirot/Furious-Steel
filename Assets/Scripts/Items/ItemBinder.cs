

using System.Text;
using Effiry.Items;
using Mirror;
using UnityEngine;

public class ItemBinder : MonoBehaviour
{
    public Item item
    {
        get => _item;
        set
        {
            _item = value;
            
            // if (isServer)
            // {
            //     OnItemChanged_Command(Item.ToJsonString(value));
            // }
        }
    }

    private Item _item;
    
    private Renderer[] meshRenderers;  

    private void Awake()
    {
        meshRenderers = GetComponentsInChildren<Renderer>();
    }

}