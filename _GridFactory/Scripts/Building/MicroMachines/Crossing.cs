using System.Collections.Generic;
using System.Collections;
using UnityEngine;

using GridFactory.Core;
using GridFactory.Grid;
using GridFactory.Directions;
using GridFactory.Conveyor;
using GridFactory.Inventory;

namespace GridFactory.Machines
{
    public class Crossing : MachineBase
    {
        private struct Lane
        {
            public Direction inputDir;   // Richtung IN das Crossing (Nachbar sitzt auf inputDir)
            public Direction outputDir;  // Richtung AUS dem Crossing (Nachbar sitzt auf outputDir)
            public Item item;            // Slot (1 Item)
        }

        [Header("Crossing Inputs (2 Lanes)")]
        [SerializeField] private List<Direction> extraInputDirections = new List<Direction>();
        [SerializeField] private SpriteRenderer itemVisualLaneA;
        [SerializeField] private SpriteRenderer itemVisualLaneB;

        public List<Direction> AllExtraInpuDirections => extraInputDirections;

        private Lane _laneA; // primary
        private Lane _laneB; // extra
        private bool hasPulledToA = false;
        private bool hasPulledToB = false;


        private Item LaneAItem // Set for visual update
        {
            get => _laneA.item;
            set
            {
                _laneA.item = value;

                StopAllCoroutines();
                StartCoroutine(UpdateItemVisual());
            }
        }

        private Item LaneBItem // Set for visual update
        {
            get => _laneB.item;
            set
            {
                _laneB.item = value;

                StopAllCoroutines();
                StartCoroutine(UpdateItemVisual());
            }
        }

        protected override void StartProcess() { return; }

        protected override void OnEnable()
        {
            base.OnEnable();
            RebuildLanes();
        }

        public override void ResetSimulation()
        {
            base.ResetSimulation();
            LaneAItem = null;
            LaneBItem = null;
        }

        protected override bool CanStartProcess()
        {
            if (_cell == null)
                return false;

            if (_laneB.item != null && CanOutput(_laneB.outputDir, true))
                return true;

            if (_laneB.item == null && CanPullFromConveyor(_laneB.inputDir))
                return true;

            if (_laneA.item != null && CanOutput(_laneA.outputDir, true))
                return true;

            if (_laneA.item == null && CanPullFromConveyor(_laneA.inputDir))
                return true;

            return false;
        }

        protected override bool FinishProcess()
        {
            hasPulledToA = false;
            hasPulledToB = false;

            if (_laneA.item == null)
            {
                TryToPullFromConveyor(_laneA.inputDir, out var item);
                if (item != null)
                {
                    LaneAItem = item;
                    hasPulledToA = true;
                }
            }

            if (_laneB.item == null)
            {
                TryToPullFromConveyor(_laneB.inputDir, out var item);
                if (item != null)
                {
                    LaneBItem = item;
                    hasPulledToB = true;
                }
            }

            if (!hasPulledToA && CanOutput(_laneA.outputDir, true) && TryToPushToConveyor(_laneA.outputDir, _laneA.item))
                LaneAItem = null;

            if (!hasPulledToB && CanOutput(_laneB.outputDir, true) && TryToPushToConveyor(_laneB.outputDir, _laneB.item))
                LaneBItem = null;

            return true;
        }

        public bool HasInputInDirection(Direction neighborDirFromCrossing)
        {
            Direction incoming = DirectionUtils.Opposite(neighborDirFromCrossing);
            if (incoming == _laneA.inputDir || incoming == _laneB.inputDir)
                return true;
            return false;
        }

        public bool HasFreeLane(Direction neighborDirFromCrossing)
        {
            Direction incoming = DirectionUtils.Opposite(neighborDirFromCrossing);

            if (incoming == _laneA.inputDir && _laneA.item == null)
                return true;

            if (incoming == _laneB.inputDir && _laneB.item == null)
                return true;

            return false;
        }

        public bool HasItemOnLane(Direction neighborDirFromCrossing)
        {
            Direction incoming = DirectionUtils.Opposite(neighborDirFromCrossing);

            if (incoming == _laneA.outputDir && _laneA.item != null)
                return true;

            if (incoming == _laneB.outputDir && _laneB.item != null)
                return true;

            return false;
        }

        public bool TryPullItemFromLane(Direction neighborDirFromCrossing, out Item item)
        {
            item = null;
            Direction incoming = DirectionUtils.Opposite(neighborDirFromCrossing);

            if (incoming == _laneA.outputDir && _laneA.item != null)
            {
                item = _laneA.item;
                LaneAItem = null;
                return true;
            }

            if (incoming == _laneB.outputDir && _laneB.item != null)
            {
                item = _laneB.item;
                LaneBItem = null;
                return true;
            }
            return false;
        }

        public bool TryPushItemToLane(Direction neighborDirFromCrossing, Item outputItem)
        {
            Direction incoming = DirectionUtils.Opposite(neighborDirFromCrossing);

            if (incoming == _laneA.inputDir && _laneA.item == null)
            {
                LaneAItem = outputItem;
                return true;
            }

            if (incoming == _laneB.inputDir && _laneB.item == null)
            {
                LaneBItem = outputItem;
                return true;
            }

            return false;
        }

        private bool CanPullFromConveyor(Direction incomingDir)
        {
            GridCell targetCell = GetAdjacentCell(incomingDir);
            if (targetCell == null)
                return false;

            if (targetCell.Machine is Crossing crossing)
            {
                if (crossing.HasItemOnLane(incomingDir))
                    return true;
                return false;
            }
            else
            {
                IItemEndpoint ep = targetCell.ItemEndpoint;
                if (ep is ConveyorBase && ep.CurrentItem != null && ep.CurrentItem.type != ItemType.None)
                    return true;
                return false;
            }
        }

        private bool TryToPushToConveyor(Direction incomingDir, Item item)
        {
            GridCell targetCell = GetAdjacentCell(incomingDir);
            if (targetCell == null)
                return false;

            if (targetCell.Machine is Crossing crossing)
            {
                if (crossing.TryToPushToConveyor(incomingDir, item))
                    return true;
                return false;
            }
            else
            {
                IItemEndpoint endpoint = targetCell.ItemEndpoint;

                if (!IsAllowedEndpointForMachine(endpoint))
                    return false;

                Vector2Int expectedUpstreamPos =
                    targetCell.Position + DirectionUtils.DirectionToOffset(endpoint.InputDirection);

                if (expectedUpstreamPos == _cell.Position && endpoint.CurrentItem == null)
                {
                    endpoint.CurrentItem = item;
                    return true;
                }
                return false;
            }
        }


        private bool TryToPullFromConveyor(Direction incomingDir, out Item item)
        {
            item = null;

            GridCell targetCell = GetAdjacentCell(incomingDir);
            if (targetCell == null) return false;

            if (targetCell.Machine is Crossing crossing)
            {
                crossing.TryPullItemFromLane(incomingDir, out var pullItem);
                if (pullItem != null)
                {
                    item = pullItem;
                    return true;
                }
                return false;
            }
            else
            {
                IItemEndpoint ep = targetCell.ItemEndpoint;

                if (ep is ConveyorBase && ep.CurrentItem != null && ep.CurrentItem.type != ItemType.None)
                {
                    item = ep.CurrentItem;
                    ep.CurrentItem = null;
                    return true;
                }
                return false;
            }
        }

        protected override void RotateAdditionalInputs(Direction primaryInputDirection)
        {
            inputDirection = primaryInputDirection;
            Direction rotated = Direction.Left;

            switch (primaryInputDirection)
            {
                case Direction.Up: rotated = Direction.Left; break;
                case Direction.Left: rotated = Direction.Down; break;
                case Direction.Down: rotated = Direction.Right; break;
                case Direction.Right: rotated = Direction.Up; break;
            }

            extraInputDirections[0] = rotated;
            outputDirection = DirectionUtils.Opposite(inputDirection);
            RebuildLanes();
        }

        private void RebuildLanes()
        {
            _laneA.inputDir = inputDirection;
            _laneA.outputDir = DirectionUtils.Opposite(inputDirection);

            _laneB.inputDir = extraInputDirections[0];
            _laneB.outputDir = DirectionUtils.Opposite(_laneB.inputDir);

            outputDirection = _laneA.outputDir;
        }

        private IEnumerator UpdateItemVisual()
        {
            if (itemVisualLaneA)
            {
                if (_laneA.item == null)
                {
                    if (_cell != null)
                    {
                        GridCell iCell = GM.GetCell(_cell.Position + DirectionUtils.DirectionToOffset(outputDirection));
                        if (iCell.Machine)
                            yield return new WaitForSeconds(0.05f);
                    }
                    itemVisualLaneA.enabled = false;
                }
                else
                {
                    itemVisualLaneA.enabled = true;
                    itemVisualLaneA.sprite = IM.GetItemSprite(_laneA.item.type);
                }
            }
            if (itemVisualLaneB)
            {
                if (_laneB.item == null)
                {
                    if (_cell != null)
                    {
                        GridCell iCell = GM.GetCell(_cell.Position + DirectionUtils.DirectionToOffset(outputDirection));

                        if (iCell.Machine)
                            yield return new WaitForSeconds(0.05f);

                    }
                    itemVisualLaneB.enabled = false;
                }
                else
                {
                    itemVisualLaneB.enabled = true;
                    itemVisualLaneB.sprite = IM.GetItemSprite(_laneB.item.type);
                }
            }
        }

    }
}
