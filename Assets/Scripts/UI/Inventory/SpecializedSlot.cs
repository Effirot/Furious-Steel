

using System;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Effiry.Items;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpecializedSlot : MonoBehaviour,
    IItemSlot,
    IPointerDownHandler
{
    public enum ItemCheckingType
    {
        WhenAll,
        WhenAny,
    }

    [Serializable]
    public struct ItemCheckCondition
    {
        [Serializable]
        public enum RequirementType
        {
            HasAttribute,
            IsSubclassOfType,
            HasArgument,
            Rarity,
        }

        public RequirementType Type;
        public string Value;
    
        public bool Check(Item item)
        {
            var value = Value;

            return Type switch
            {
                RequirementType.HasAttribute => item.GetType().GetCustomAttributes().Where(attribute => attribute.GetType().Name == value).Any(),
                RequirementType.IsSubclassOfType => item.GetType().IsSubclassOf(typeof(Item).Assembly.GetType(value)) || item.GetType().GetInterface(value) != null,
                RequirementType.HasArgument => Array.Exists(item.Args, arg => arg == value),
                RequirementType.Rarity => item.Rarity == Enum.Parse<Item.Quality>(value),
                _ => false,
            };
        }
    }

    [SerializeField]
    private Image image;

    [SerializeField]
    private Sprite loadingSprite;

    [SerializeField]
    private Sprite invalidLoadingSprite;
    
    [Space]
    [SerializeField]
    public bool AllowEmptyness = false;
    
    [SerializeField]
    public UnityEvent<Item> onItemChanged = new();

    [SerializeField]
    private ItemCheckingType checkingType = ItemCheckingType.WhenAll;
    [SerializeField]
    private ItemCheckCondition[] itemCheckConditions = new ItemCheckCondition[0];

    private string SlotName => "ItemSlot" + gameObject.name;

    public Item Item {
        get => _item; 
        set
        {
            if (value == null && AllowEmptyness)
            {
                onItemChanged.Invoke(null);
                _item = null;
            }
            else
            {
                if (CheckItem(value))
                {
                    onItemChanged.Invoke(value);
                    _item = value;
                }
            }
            
            SaveItem(_item);

            LoadIcon();
        } 
    }
    
    private Item _item;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (AllowEmptyness)
        {
            Item = null;
        }
    }

    private bool CheckItem(Item item)
    {
        if (item == null)
        {
            return false;
        }

        return checkingType switch
        {
            ItemCheckingType.WhenAll => itemCheckConditions.All(checker => checker.Check(item)),
            ItemCheckingType.WhenAny => itemCheckConditions.Any(checker => checker.Check(item)),
            
            _ => false
        };
    }


    private async void LoadIcon()
    {
        foreach(Transform item in image.transform)
        {
            Destroy(item.gameObject);
        }

        image.gameObject.SetActive(Item != null);
        
        if (Item == null)
            return;

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

    private void Start()
    {
        Item = LoadItem();
    }

    private Item LoadItem()
    {
        var loadedIndex = PlayerPrefs.GetInt(SlotName, -1);
            
        if (loadedIndex < 0 || loadedIndex >= InventoryDrawer.LocalInventoryInstance.items.Length)
        {
            if (AllowEmptyness)
            {
                return null;
            }
            else
            {
                return InventoryDrawer.LocalInventoryInstance.items.First(item => CheckItem(item));
            }
        }
        
        return InventoryDrawer.LocalInventoryInstance.items[loadedIndex];
    }
    private void SaveItem(Item item)
    {            
        PlayerPrefs.SetInt(SlotName, Array.IndexOf(InventoryDrawer.LocalInventoryInstance.items, item));
    }

    private Sprite ConvertToSprite(Texture2D texture)
    {
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        sprite.name = texture.name + "(SPRITE)";

        return sprite;
    }
}