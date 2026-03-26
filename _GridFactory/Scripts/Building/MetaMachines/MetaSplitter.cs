using System.Collections.Generic;

using UnityEngine;

using GridFactory.Core;
using GridFactory.Directions;
using GridFactory.Grid;

namespace GridFactory.Meta
{
    public class MetaSplitter : MetaMachineBase
    {
        private int _nextOutputIndex = 0;

        protected override void RotateAdditionalInputs(Direction dir) { }

        public override List<Direction> AllOutputDirections()
        {
            List<Direction> dirs = DirectionUtils.AllDirectionsAsList();
            dirs.Remove(inputDirection);
            return dirs;
        }

        public override void ResetSimulation()
        {
            base.ResetSimulation();
            CurrentItem = null;
            _nextOutputIndex = 0;
        }

        protected override bool CanStartProcess()
        {
            if (!hasInput)
                return false;

            if (CurrentItem != null && CurrentItem.type != ItemType.None)
                return true;

            if (CanPullFromDirection(inputDirection))
                return true;

            return false;
        }

        protected override void StartProcess()
        {
            if (!hasInput || (CurrentItem != null && CurrentItem.type != ItemType.None))
                return;

            TryPullIntoProcessing(inputDirection);
        }

        protected override bool FinishProcess()
        {
            if (CurrentItem == null)
                return true;

            if (!hasOutput)
            {
                CurrentItem = null;
                return true;
            }

            List<Direction> outputs = new List<Direction>();
            foreach (var dir in DirectionUtils.AllDirections())
            {
                if (dir == inputDirection)
                    continue;

                MetaCell outCell = GetAdjacentCell(dir);
                if (outCell == null)
                    continue;

                if (outCell.Machine is MetaCrossing crossing)
                {
                    if (crossing.HasFreeLane(dir))
                        outputs.Add(dir);
                    continue;
                }

                IItemEndpoint endpoint = outCell.ItemEndpoint;
                if (!IsAllowedEndpointForMachine(endpoint))
                    continue;

                Vector2Int expectedUpstream =
                    outCell.Position + DirectionUtils.DirectionToOffset(endpoint.InputDirection);

                if (expectedUpstream != _cell.Position)
                    continue;

                outputs.Add(dir);
            }

            if (outputs.Count == 0)
                return false;

            int count = outputs.Count;
            for (int i = 0; i < count; i++)
            {
                int idx = (_nextOutputIndex + i) % count;
                var dir = outputs[idx];

                if (CanOutput(dir) && OutputToNeighbor(dir, CurrentItem))
                {
                    CurrentItem = null;
                    _nextOutputIndex = (idx + 1) % count;
                    return true;
                }
            }
            return false;
        }

        private bool CanPullFromDirection(Direction dir)
        {
            MetaCell inCell = GetAdjacentCell(dir);
            if (inCell == null)
                return false;

            if (inCell.Machine is MetaCrossing crossing)
            {
                if (crossing.HasItemOnLane(dir))
                    return true;
                return false;
            }
            else
            {
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
            MetaCell inCell = GetAdjacentCell(dir);
            if (inCell == null)
                return false;

            if (inCell.Machine is MetaCrossing crossing)
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
