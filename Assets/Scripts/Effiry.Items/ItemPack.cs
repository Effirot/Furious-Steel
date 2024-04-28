#nullable enable

using System.Collections.Generic;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Serialization;

namespace Effiry.Items
{
    public sealed class ItemPack
    {
        public static readonly JsonSerializerSettings JsonSerializSettings = new JsonSerializerSettings() { 
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
        };
        
        public int MaxSize => items.Length;
        public Item?[] items = System.Array.Empty<Item>();

        public ItemPack() { }

        public void LoadFromString(string Json)
        {
            var pack = JsonConvert.DeserializeObject<ItemPack>(Json, JsonSerializSettings);
            
            if (pack is not null)
            {
                items = pack.items;
            }
        }
        public string SaveToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented, JsonSerializSettings);
        }
    }
}