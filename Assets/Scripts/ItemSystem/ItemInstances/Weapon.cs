

namespace Effiry.Items
{
    public abstract class Weapon : Item 
    {
        protected Weapon() {  }

        public Material[] craftedMaterials = new Material[0];
    }
}