

using Effiry.Items;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class SpecialSlot<T> : MonoBehaviour,
    IItemSlot<T>,
    IPointerClickHandler
        where T : Item
{
    [SerializeField]
    public bool AllowEmptyness = false;

    public T Item { get; set; }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (AllowEmptyness)
        {
            Item = null;
        }
    }
}