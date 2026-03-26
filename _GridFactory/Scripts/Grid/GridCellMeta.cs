using UnityEngine;

using GridFactory.Core;
using GridFactory.Conveyor;
using GridFactory.Meta;

namespace GridFactory.Grid
{
    public class MetaCell
    {
        public Vector2Int Position;
        public MetaMachineBase Machine;
        public MetaConveyorBase Conveyor;
        //public bool IsLocked;
        public SpriteRenderer Sprite;
        public GameObject Tile;
        public bool IsEmpty => Machine == null && Conveyor == null;

        public MetaCell(Vector2Int pos, bool isLocked = false)
        {
            //IsLocked = isLocked;
            Position = pos;
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
