#nullable enable

using System.Text.Json;
using System.Collections.Generic;
using Codice.Client.Common;
using System;

namespace Effiry.Items
{
    
    public sealed class ItemPack
    {
        [UnityEngine.RuntimeInitializeOnLoadMethod]
        public static void OnLoad()
        {
            
        }

        public Item?[] items = Array.Empty<Item>();

        public ItemPack(params Item?[] items)
        {
            this.items = items;
        }
    }
}