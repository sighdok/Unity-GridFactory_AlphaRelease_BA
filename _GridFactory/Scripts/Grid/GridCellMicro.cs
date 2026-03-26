using UnityEngine;

using GridFactory.Core;
using GridFactory.Machines;
using GridFactory.Conveyor;

namespace GridFactory.Grid
{
    public class GridCell
    {
        public bool IsLocked;
        public Vector2Int Position;
        public MachineBase Machine;
        public ConveyorBase Conveyor;
        public LockType LockType;
        public SpriteRenderer Sprite;
        public bool IsEmpty => Machine == null && Conveyor == null;

        public GridCell(Vector2Int pos, bool isLocked = false, LockType lockType = LockType.NotLocked)
        {
            Position = pos;
            IsLocked = isLocked;
            LockType = lockType;
        }

        public IItemEndpoint ItemEndpoint
        {
            get
            {
                if (Machine is IItemEndpoint machineEndpoint)
                    return machineEndpoint;

                if (Conveyor is IItemEndpoint conveyorEndpoint)
                    return conveyorEndpoint;

                return null;
            }
        }
    }
}