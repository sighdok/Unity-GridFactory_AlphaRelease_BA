using System;
using System.Collections.Generic;

using UnityEngine;

using GridFactory.Grid;

namespace GridFactory.Core
{
    [Serializable]
    public class GridDefinitionOwned
    {
        public string id;              // unique (preset id oder rnd_...)
        public string displayName;
        public int width;
        public int height;
        public bool isPreset;
        public int seed;               // nur für Random relevant (Preset = 0)
        public List<CellLockData> locks = new List<CellLockData>();

        public int TotalCells => width * height;
        public int LockedCount => locks?.Count ?? 0;
        public int FreeCells => TotalCells - LockedCount;

        public bool IsLocked(Vector2Int p)
        {
            if (locks == null) return false;
            for (int i = 0; i < locks.Count; i++)
            {
                if (locks[i].x == p.x && locks[i].y == p.y)
                    return true;
            }
            return false;
        }
    }
}
