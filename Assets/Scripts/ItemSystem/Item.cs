#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Effiry.Items
{
    public class Item
    {
        public enum Quality : byte
        {
            Common,

            Improved,
            Obsessed,
            
            Rare,
        }

        
        public ReactiveValue<string> Name = "Nameless";
        public ReactiveValue<string> Description = "";

        public ReactiveValue<Quality> Rarity = Quality.Common;

        public ReactiveValue<string[]> Args = Array.Empty<string>();

        public DateTime creationTime = DateTime.Now;
        public DateTime lastModificationTime = DateTime.Now;

        
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

    public class SAS : Item
    {
        public int U = 1;
    }
}
