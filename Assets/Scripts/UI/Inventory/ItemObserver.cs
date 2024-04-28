

using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks.Triggers;
using Effiry.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ItemObserver : MonoBehaviour
{
    [SerializeField]
    private bool FollowSlot = false;

    [Space]
    [SerializeField]
    private TMP_Text NameField;
    [SerializeField]
    private TMP_Text DescriptionField;
    [SerializeField]
    private TMP_Text ArgumentsField;


    private List<RaycastResult> eventDataRaycastResult = new();
    private GameObject target;

    public Item SelectedItem 
    {
        get => _selectedItem;
        set {
            _selectedItem = value;
            
            foreach(Transform item in transform)
            {
                item.gameObject.SetActive(value != null);
            }

            if (value != null)
            {
                NameField?.SetText(value.GetType().Name + (value.Name.Any() ? "(" + value.Name + ")" : ""));
                DescriptionField?.SetText("");
                ArgumentsField?.SetText(string.Join("\n", value.Args.Select(arg => " - " + arg)));
            }
            else
            {
                NameField?.SetText("");
                DescriptionField?.SetText("");
                ArgumentsField?.SetText("");
            }

        }
    }
    private Item _selectedItem;

    private void Awake()
    {
        SelectedItem = null;
    }

#if !UNITY_SERVER || UNITY_EDITOR
    private void FixedUpdate()
    {
        PointerEventData eventData = new(EventSystem.current);
        eventData.position = Mouse.current.position.value;

        EventSystem.current.RaycastAll(eventData, eventDataRaycastResult);
        

        if (eventDataRaycastResult.Any() && eventDataRaycastResult.First().gameObject.TryGetComponent<IItemSlot>(out var component))
        {
            target = eventDataRaycastResult.First().gameObject;
            SelectedItem = component.Item;
        }
        else
        {
            target = null;
            SelectedItem = null;
        }

        if (FollowSlot)
        {
            if (target != null)
            {
                transform.position = Vector2.Lerp(transform.position, target.transform.position, 0.1f);
            }
        }
    }
#endif
}