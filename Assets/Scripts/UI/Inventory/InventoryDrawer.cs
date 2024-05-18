

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Effiry.Items;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class InventoryDrawer : MonoBehaviour
{
    public static string inventoryBufferPath => Application.persistentDataPath + "/inventory.json";

    public static ItemPack LocalInventoryInstance = new() { 
        items = new Item[] {
            new Sword(), new LongSword(), new Axe(), new Buckler(), new Bone(), 
            new Mace(), new Spear(), new Shield(), null, new Plate(), 
            new HeavySword(), new Rapier(), new Bow(), new Bag(), new LeatherJacket(),
            
            new VoidMagic(), new ExplodeMagic(), null, null, new SteelScrap(), 
            new CurseFlameMagic(), null, null, null, new SteelScrap(), 
            new ThunderMagic(), null, null, null, new SteelScrap()
        }
    };
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void OnLoad()
    {
        Application.quitting += OnQuitting;

    } 
    private static void OnQuitting()
    {       
        Debug.Log($"Inventory was succesfully saved to \"{inventoryBufferPath}\"!");
    }

    [SerializeField]
    private GameObject inventorySlotPrefab;

    private List<InventorySlot> slot_intances = new();

    public void Clear()
    {
        while(slot_intances.Any())
        {
            if (!slot_intances[0].IsUnityNull())
            {
                Destroy(slot_intances[0].gameObject);
            }

            slot_intances.RemoveAt(0);
        }
    }
    public async void Refresh()
    {
        Clear();
     
        if (File.Exists(inventoryBufferPath))
        {
            var bytes = await File.ReadAllBytesAsync(inventoryBufferPath);
            LocalInventoryInstance.LoadFromString(Encoding.UTF8.GetString(bytes));
            
            Debug.Log($"Inventory was succesfully loaded from \"{inventoryBufferPath}\"!");
        }

        for (int i = 0; i < LocalInventoryInstance.MaxSize; i++)
        {
            CreateSlot(i);
        }
    }

    private void CreateSlot(int Index)
    {
        var slotObject = Instantiate(inventorySlotPrefab, transform);
        slotObject.SetActive(true);

        var slot = slotObject.GetComponent<InventorySlot>();
        slot.index = Index;
        slot.Item = Index <  LocalInventoryInstance.items.Length ? LocalInventoryInstance.items[Index] : null;

        slot_intances.Add(slot);
    }

    private void OnEnable()
    {   
        Refresh();
    }
    private async void OnDisable()
    {
        var bytes = Encoding.UTF8.GetBytes(LocalInventoryInstance.SaveToString());
        await File.WriteAllBytesAsync(inventoryBufferPath, bytes);

        Clear();
    }
}