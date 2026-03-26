using UnityEngine;

namespace GridFactory.Grid
{
    [CreateAssetMenu(menuName = "GridFactory/GridDefinition")]
    public class GridDefinition : ScriptableObject
    {
        [Header("Meta")]
        public string id = "grid_preset_001";
        public string displayName = "Preset Grid";

        [Header("Size")]
        public int width = 3;
        public int height = 3;
        public int price = 0;

        [Header("Lock Data")]
        public Vector2Int[] unlockableLockedCells;
        public Vector2Int[] lockedCells;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
                id = name;

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = name;

            if ((unlockableLockedCells == null || unlockableLockedCells.Length == 0) && (lockedCells != null && lockedCells.Length > 0))
                unlockableLockedCells = lockedCells;
        }
    }
}
