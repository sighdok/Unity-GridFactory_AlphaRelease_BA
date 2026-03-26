using UnityEngine;

namespace GridFactory.Core
{
    [CreateAssetMenu(menuName = "GridFactory/RecipeDefinition")]
    public class RecipeDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public RecipeItem[] inputItems;
        public RecipeItem[] outputItems;
        public float machineProcessingTimeMultiplikator = 1;
    }
}