using UnityEngine;

using GridFactory.Core;
using GridFactory.Grid;
using GridFactory.Directions;

namespace GridFactory.Machines
{
    public class PortMarker : MachineBase
    {
        [Header("Port Settings")]
        public PortKind portKind;
        public ItemType portItemType = ItemType.Wood;

        protected override void RotateAdditionalInputs(Direction dir) { }
        protected override void StartProcess() { return; }

        public override void ResetSimulation()
        {
            base.ResetSimulation();
            CurrentItem = null;
        }

        protected override bool CanStartProcess()
        {
            if (portKind == PortKind.Input)
                return true;
            if (portKind == PortKind.Output)
            {
                if (CurrentItem == null && CanPullFromDirection(inputDirection))
                    return TryConsumeFromSingleInput();

            }
            return false;
        }

        protected override bool FinishProcess()
        {
            if (portKind == PortKind.Input)
            {
                var item = new Item(portItemType);
                if (CanOutput(outputDirection))
                    return OutputToNeighbor(outputDirection, item);
            }
            else if (portKind == PortKind.Output)
            {
                if (TutorialGridFactoryController.Instance)
                    TutorialGridFactoryController.Instance.OutputPortReceivedItem(CurrentItem.type);
                CurrentItem = null;
                return true;
            }

            return false;
        }

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
                IItemEndpoint endpoint = inCell.ItemEndpoint;
                if (!IsAllowedEndpointForMachine(endpoint) || endpoint.CurrentItem == null)
                    return false;

                Vector2Int expectedDownstream =
                    inCell.Position + DirectionUtils.DirectionToOffset(endpoint.OutputDirection);

                return expectedDownstream == _cell.Position;
            }
        }

        private bool TryConsumeFromSingleInput()
        {
            GridCell inCell = GetAdjacentCell(inputDirection);
            if (inCell == null)
                return false;

            if (inCell.Machine is Crossing crossing)
            {
                if (crossing.TryPullItemFromLane(inputDirection, out var crossIncomingItem))
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
                if (!IsAllowedEndpointForMachine(endpoint) || endpoint.CurrentItem == null)
                    return false;

                Vector2Int expectedDownstream =
                    inCell.Position + DirectionUtils.DirectionToOffset(endpoint.OutputDirection);

                if (expectedDownstream != _cell.Position)
                    return false;

                CurrentItem = endpoint.CurrentItem;
                endpoint.CurrentItem = null;

                portItemType = CurrentItem.type;
                return true;
            }
            return false;
        }
    }
}
