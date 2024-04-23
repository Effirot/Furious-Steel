

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Effiry.Items;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IItemSlot
{
    [SerializeField]
    private Image image;

    [SerializeField]
    private Sprite loadingSprite;

    [SerializeField]
    private Sprite invalidLoadingSprite;

    [SerializeField]
    public int index;

    public Item Item
    {
        get => _item;
        set {
            onItemChanged.Invoke(_item = value);
            
            LoadIcon();
        }
    }

    public event Action<Item> onItemChanged = delegate { };

    private Item _item;

    private RectTransform dragObject = null;

    private async void LoadIcon()
    {
        if (Item == null)
        {
            image.enabled = false;
            
            return;
        }

        var asyncRequest = Resources.LoadAsync($"Items/Icons/{Item.GetType().Name}");
        
        image.enabled = true;
        image.sprite = loadingSprite;
        
        await asyncRequest;

        if (asyncRequest.asset is Texture2D)
        {
            image.sprite = ConvertToSprite(asyncRequest.asset as Texture2D);

            return;
        }

        if (asyncRequest.asset is GameObject)
        {
            image.enabled = false;
            Instantiate(asyncRequest.asset, transform);

            return;
        }

        image.sprite = invalidLoadingSprite;
    }

    private Sprite ConvertToSprite(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Item == null) return;

        var root = eventData.pointerPressRaycast.module.gameObject.transform;

        dragObject = Instantiate(image.gameObject, eventData.position, quaternion.identity, transform).transform as RectTransform;
        dragObject.SetParent(root, true);
        dragObject.SetAsLastSibling();
        
        image.enabled = false;
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (dragObject != null)
        {
            dragObject.position = eventData.position;
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragObject == null) return;
        
        Destroy(dragObject.gameObject);

        if (!eventData.pointerEnter.IsUnityNull() && eventData.pointerEnter.gameObject.TryGetComponent<IItemSlot>(out var newSlot))
        {
            var lastItem = newSlot.Item;

            newSlot.Item = this.Item;

            if (newSlot is InventorySlot)
            {
                var newInventorySlot = newSlot as InventorySlot;

                InventoryDrawer.LocalInventoryInstance.items[newInventorySlot.index] = this.Item;
                InventoryDrawer.LocalInventoryInstance.items[this.index] = lastItem;
                this.Item = lastItem;
            }
        }
        else
        {
            image.enabled = true;
        }
    }
}