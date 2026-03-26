using UnityEngine;

namespace GridFactory.Core
{
    [CreateAssetMenu(menuName = "GridFactory/ItemDefinition")]
    public class ItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public ItemType type;
        public int value;
        public bool producesEnergy;
        public float energyAmount;
    }
}
