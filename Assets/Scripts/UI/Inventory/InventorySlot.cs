

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
        get => InventoryDrawer.LocalInventoryInstance.items[index];
        set {
            onItemChanged.Invoke(InventoryDrawer.LocalInventoryInstance.items[index] = value);

            LoadIcon();
        }
    }

    public event Action<Item> onItemChanged = delegate { };

    private RectTransform dragObject = null;


    private void Awake()
    {
        GetComponent<MaskableGraphic>().onCullStateChanged.AddListener(OnCullStateChanged);
    }

    private void OnCullStateChanged(bool cullState)
    {
        Debug.Log("A");
        image.gameObject.SetActive(cullState && Item != null);
    }

    private async void LoadIcon()
    {
        foreach(Transform item in image.transform)
        {
            Destroy(item.gameObject);
        }

        // GetComponent<MaskableGraphic>().raycastTarget = Item != null;
        
        if (Item == null)
        {
            image.gameObject.SetActive(false);

            return;
        }


        var asyncRequest = Resources.LoadAsync($"Items/Icons/{Item.GetType().Name}");
        
        image.gameObject.SetActive(true);
        image.sprite = loadingSprite;
        
        await asyncRequest;

        if (asyncRequest.asset is Texture2D)
        {
            image.enabled = true;
            image.gameObject.SetActive(true);
            
            image.sprite = ConvertToSprite(asyncRequest.asset as Texture2D);

            return;
        }

        if (asyncRequest.asset is GameObject)
        {
            image.enabled = false;
            image.gameObject.SetActive(true);

            Instantiate(asyncRequest.asset, image.transform);

            return;
        }

        image.enabled = true;
        image.gameObject.SetActive(true);
        image.sprite = invalidLoadingSprite;
    }

    private Sprite ConvertToSprite(Texture2D texture)
    {
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        sprite.name = texture.name + "(SPRITE)";

        return sprite;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Item == null) return;

        var root = eventData.pointerPressRaycast.module.gameObject.transform;

        dragObject = Instantiate(image.gameObject, eventData.position, quaternion.identity, transform).transform as RectTransform;
        dragObject.SetParent(root, true);
        dragObject.SetAsLastSibling();
        
        image.gameObject.SetActive(false);
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (dragObject != null)
        {
            dragObject.position = eventData.pointerCurrentRaycast.worldPosition;
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

                this.Item = lastItem;
            }
        }
        
        image.gameObject.SetActive(true);      
    }
}