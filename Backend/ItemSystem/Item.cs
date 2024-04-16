#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Effiry.Items
{
    public abstract class Item
    {
        public enum Quality : byte
        {
            Common,

            Improved,
            Obsessed,
            
            Rare,
        }
 
        public string Name { 
            get => _name;
            set
            {
                LastModificationTime = DateTime.Now;
                _name = value;
            }
        }
        public string Description { 
            get => _description;
            set
            {
                LastModificationTime = DateTime.Now;
                _description = value;
            }
        }

        public Quality Rarity { 
            get => _rarity;
            set
            {
                LastModificationTime = DateTime.Now;
                _rarity = value;
            }
        }

        public string[] Args { 
            get => _args;
            set
            {
                LastModificationTime = DateTime.Now;
                _args = value;
            }
        }

        private string _name = "";
        private string _description = "";

        private Quality _rarity = Quality.Common;

        private string[] _args = Array.Empty<string>();

        public DateTime CreationTime { get; private set; } = DateTime.Now;
        public DateTime LastModificationTime { get; private set; } = DateTime.Now;

        public Item() { }

        [ItemCommand]
        public void Delete()
        {

        }
    }

    public class Sas : Item { } 
}
