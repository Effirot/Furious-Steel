#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

using MessagePack;

namespace Effiry.Items
{
    [MessagePackObject]
    public class Item
    {
        public enum Quality : byte
        {
            Common,

            Improved,
            Obsessed,
            
            Rare,
        }


        [Key(0)] public ReactiveValue<string> Name = "Nameless";
        [Key(1)] public ReactiveValue<string> Description = "";

        [Key(2)] public ReactiveValue<Quality> Rarity = Quality.Common;

        [Key(3)] public ReactiveValue<string[]> Args = Array.Empty<string>();

        [Key(4)] public DateTime creationTime = DateTime.Now;
        [Key(5)] public DateTime lastModificationTime = DateTime.Now;

        
        public Item()
        {
            creationTime = DateTime.Now;
            lastModificationTime = DateTime.Now;

            Name.OnValueChanged += delegate { lastModificationTime = DateTime.Now; };
            Description.OnValueChanged += delegate { lastModificationTime = DateTime.Now; };
            Rarity.OnValueChanged += delegate { lastModificationTime = DateTime.Now; };
            Args.OnValueChanged += delegate { lastModificationTime = DateTime.Now; };
        }
    }


    [MessagePackObject]
    public class SAS : Item
    {
        [Key(6)]
        public int U = 1;
    }
}
