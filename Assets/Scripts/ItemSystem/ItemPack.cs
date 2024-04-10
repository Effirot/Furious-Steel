#nullable enable

using System;
using MessagePack;
using MessagePack.Formatters;

namespace Effiry.Items
{
    [MessagePackObject]
    public sealed class ItemPack
    {
        [UnityEngine.RuntimeInitializeOnLoadMethod]
        public static void OnLoad()
        {
            var item1 = new ItemPack(
                new Item(), 
                new SAS(), 
                new Item());
                
            var bytes = MessagePackSerializer.Serialize(item1);
           
            UnityEngine.Debug.Log(MessagePackSerializer.ConvertToJson(bytes)); 
        }

        [Key(0), MessagePackFormatter(typeof(TypelessFormatter))]
        public Item?[] items = Array.Empty<Item>();

        public ItemPack(params Item?[] items)
        {
            this.items = items;
        }
    }
}