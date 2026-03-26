using System;

using UnityEngine;

namespace GridFactory.Grid
{
    public enum LockType
    {
        NotLocked = 0,
        Unlockable = 1,
        Permanent = 2,
        SoftLock = 3
    }

    [Serializable]
    public struct CellLockData
    {
        public int x;
        public int y;
        public LockType lockType;

        public CellLockData(int x, int y, LockType lockType)
        {
            this.x = x;
            this.y = y;
            this.lockType = lockType;
        }

        public Vector2Int Pos => new Vector2Int(x, y);
    }
}
