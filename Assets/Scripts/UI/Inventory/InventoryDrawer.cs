

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Effiry.Items;
using UnityEngine;
using UnityEngine.Events;

public class InventoryDrawer : MonoBehaviour
{
    public static readonly string inventoryBufferPath = Application.dataPath + "/inventory.json";

    public static ItemPack LocalInventoryInstance = new() { 
        items = new Item[] {
            new Sword(), new LongSword(), new HeavySword(),
            new Rapier(), new Axe(), new Mace(),
            new Spear(), null, null,
            null, null, null,
            null, null, null,
            
            null, null, null,
            null, null, null,
            new VoidMagic(), new CurseFlameMagic(), new ThunderMagic(),
            null, null, null,
            new SteelScrap(), new SteelScrap(), new SteelScrap()
        }, 
        MaxSize = 30 
    };
    

    public static UnityEvent OnInventoryUpdated = new();

    public static async Task UpdateLocalInventory()
    {
        // using (var request = UnityEngine.Networking.UnityWebRequest.Get("https://discussions.unity.com/t/how-do-i-get-into-the-data-returned-from-a-unitywebrequest/218510/2"))
        // {
        //     await request.SendWebRequest();

        //     LocalInventoryInstance.LoadFromString(request.downloadHandler.text);

        //     OnInventoryUpdated.Invoke();
        // }

    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void OnLoad()
    {
        Application.quitting += OnQuitting;

        if (File.Exists(inventoryBufferPath))
        {
            var bytes = File.ReadAllBytes(inventoryBufferPath);
            LocalInventoryInstance.LoadFromString(Encoding.UTF8.GetString(bytes));
            
            Debug.Log($"Inventory was succesfully loaded from \"{inventoryBufferPath}\"!");
        }
    } 
    private static void OnQuitting()
    {
        var bytes = Encoding.UTF8.GetBytes(LocalInventoryInstance.SaveToString());
        File.WriteAllBytes(inventoryBufferPath, bytes);
        
        Debug.Log($"Inventory was succesfully saved to \"{inventoryBufferPath}\"!");
    }

    [SerializeField]
    private GameObject inventorySlotPrefab;

    private List<InventorySlot> slot_intances = new();

    public void Clear()
    {
        while(slot_intances.Any())
        {
            Destroy(slot_intances[0]);
            slot_intances.RemoveAt(0);
        }
    }
    public void Refresh()
    {
        Clear();

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
        slot.Item = LocalInventoryInstance.items[Index];

        slot_intances.Add(slot);
    }

    private async void OnEnable()
    {   
        await UpdateLocalInventory();

        OnInventoryUpdated.AddListener(Refresh);

        Refresh();
    }
    private void OnDisable()
    {
        OnInventoryUpdated.RemoveListener(Refresh);

        Clear();
    }
}