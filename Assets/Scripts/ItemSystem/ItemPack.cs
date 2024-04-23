#nullable enable

using System.Collections.Generic;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Effiry.Items
{
    public sealed class ItemPack
    {
        private static readonly JsonSerializerSettings JsonSerializSettings = new JsonSerializerSettings() { 
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
        };
        
        public int MaxSize = 0;
        public Item?[] items = System.Array.Empty<Item>();

        public ItemPack() { }

        public void LoadFromString(string Json)
        {
            var pack = JsonConvert.DeserializeObject<ItemPack>(Json, JsonSerializSettings);
            
            if (pack is not null)
            {
                items = pack.items;
                MaxSize = pack.MaxSize;
            }
        }
        public string SaveToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented, JsonSerializSettings);
        }
    }
}