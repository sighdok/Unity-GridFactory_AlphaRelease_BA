using System;

namespace GridFactory.Core
{
    [Serializable]
    public class Item
    {
        public ItemType type;

        public Item(ItemType type = ItemType.None)
        {
            this.type = type;
        }
    }

    public enum ItemType
    {
        None,
        Wood,
        Coal,
        Charcoal,
        Beam,
        Plank,
        Stone,
        Stonebrick,
        Stonetile,
        IronOre,
        IronBar,
        TinOre,
        TinBar,
        BronzeAlloy
    }
}