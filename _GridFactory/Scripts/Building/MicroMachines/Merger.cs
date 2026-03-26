using System.Collections.Generic;

using UnityEngine;

using GridFactory.Core;
using GridFactory.Grid;
using GridFactory.Directions;

namespace GridFactory.Machines
{
    public class Merger : MachineBase
    {
        private int _nextInputIndex = 0;

        protected override void RotateAdditionalInputs(Direction dir) { }


        public override List<Direction> AllInputDirections()
        {
            List<Direction> dirs = DirectionUtils.AllDirectionsAsList();
            dirs.Remove(outputDirection);
            return dirs;
        }

        public override void ResetSimulation()
        {
            base.ResetSimulation();
            CurrentItem = null;
            _nextInputIndex = 0;
        }

        protected override bool CanStartProcess()
        {
            if (!hasInput || (CurrentItem != null && CurrentItem.type != ItemType.None))
                return false;

            Direction[] dirs = DirectionUtils.AllDirections();
            int attempts = dirs.Length;

            for (int i = 0; i < attempts; i++)
            {
                int idx = (_nextInputIndex + i) % attempts;
                var dir = dirs[idx];

                if (CanPullFromDirection(dir))
                    return true;
            }

            return false;
        }

        protected override void StartProcess()
        {
            if (!hasInput || (CurrentItem != null && CurrentItem.type != ItemType.None))
                return;

            Direction[] dirs = DirectionUtils.AllDirections();
            int attempts = dirs.Length;

            for (int i = 0; i < attempts; i++)
            {
                int idx = (_nextInputIndex + i) % attempts;
                var dir = dirs[idx];

                if (TryPullIntoProcessing(dir))
                {
                    _nextInputIndex = (idx + 1) % attempts;
                    break;
                }
            }
        }

        protected override bool FinishProcess()
        {
            if (CurrentItem == null || CurrentItem.type == ItemType.None)
                return true;

            if (!hasOutput)
            {
                CurrentItem = null;
                return true;
            }

            if (CanOutput(outputDirection))
            {
                if (OutputToNeighbor(outputDirection, CurrentItem))
                {
                    CurrentItem = null;
                    return true;
                }
            }

            return false;
        }

        // =============================================================
        //  Pull Helpers
        // =============================================================

        private bool CanPullFromDirection(Direction dir)
        {
            GridCell inCell = GetAdjacentCell(dir);
            if (inCell == null)
                return false;

            if (inCell.Machine is Crossing crossing)
            {
                if (crossing.HasItemOnLane(dir))
                    return true;
                return false;
            }
            else
            {

                // Standard: IItemEndpoint
                IItemEndpoint endpoint = inCell.ItemEndpoint;
                if (!IsAllowedEndpointForMachine(endpoint) || endpoint.CurrentItem == null)
                    return false;

                Vector2Int expectedDownstream =
                    inCell.Position + DirectionUtils.DirectionToOffset(endpoint.OutputDirection);

                return expectedDownstream == _cell.Position;
            }
        }

        private bool TryPullIntoProcessing(Direction dir)
        {
            GridCell inCell = GetAdjacentCell(dir);
            if (inCell == null)
                return false;

            if (inCell.Machine is Crossing crossing)
            {
                if (crossing.TryPullItemFromLane(dir, out var crossIncomingItem))
                {
                    if (crossIncomingItem != null)
                    {
                        CurrentItem = crossIncomingItem;
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                IItemEndpoint endpoint = inCell.ItemEndpoint;
                if (!IsAllowedEndpointForMachine(endpoint))
                    return false;

                Vector2Int expectedDownstream =
                    inCell.Position + DirectionUtils.DirectionToOffset(endpoint.OutputDirection);

                if (expectedDownstream != _cell.Position || endpoint.CurrentItem == null)
                    return false;

                CurrentItem = endpoint.CurrentItem;
                endpoint.CurrentItem = null;
                return true;
            }
            return false;
        }
    }
}
