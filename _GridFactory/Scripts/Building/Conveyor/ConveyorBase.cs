using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GridFactory.Core;
using GridFactory.Grid;
using GridFactory.Machines;
using GridFactory.Inventory;
using GridFactory.Directions;

namespace GridFactory.Conveyor
{
    public class ConveyorBase : MonoBehaviour, IItemEndpoint
    {
        private static readonly List<ConveyorBase> s_allConveyors = new List<ConveyorBase>();

        private readonly struct ConveyorMove
        {
            public readonly ConveyorBase from;
            public readonly ConveyorBase to;
            public readonly Item item;

            public ConveyorMove(ConveyorBase from, ConveyorBase to, Item item)
            {
                this.from = from;
                this.to = to;
                this.item = item;
            }
        }

        public static void TickAllConveyors()
        {
            if (s_allConveyors.Count == 0)
                return;

            var plannedMoves = new List<ConveyorMove>(s_allConveyors.Count);
            var usedTargets = new HashSet<ConveyorBase>();

            foreach (var conv in s_allConveyors)
            {
                if (conv._cell == null)
                    continue;

                if (conv.CurrentItem == null)
                    continue;

                conv._stepCounter++;
                int stepsNeeded = Mathf.Max(1, conv.ticksPerStep);
                if (conv._stepCounter < stepsNeeded)
                    continue; // noch nicht dran

                conv._stepCounter = 0;

                Vector2Int nextPos = conv._cell.Position + DirectionUtils.DirectionToOffset(conv.OutputDirection);
                GridCell nextCell = conv.GrM.GetCell(nextPos);
                if (nextCell == null || nextCell.Conveyor == null)
                    continue;

                var nextConv = nextCell.Conveyor;

                Vector2Int expectedUpstreamPos =
                    nextCell.Position + DirectionUtils.DirectionToOffset(nextConv.InputDirection);

                if (expectedUpstreamPos != conv._cell.Position)
                    continue;

                if (nextConv.CurrentItem != null && nextConv.CurrentItem.type != ItemType.None)
                    continue;

                if (usedTargets.Contains(nextConv))
                    continue;

                plannedMoves.Add(new ConveyorMove(conv, nextConv, conv.CurrentItem));
                usedTargets.Add(nextConv);
            }

            foreach (var move in plannedMoves)
            {
                move.from.CurrentItem = null;
                move.to.CurrentItem = move.item;
            }
        }

        private static InventoryManager IM => InventoryManager.Instance;

        private GridManager GrM => GridManager.Instance;
        private GridCell _cell;
        private Direction _outputDirection;
        private Direction _inputDirection;
        private Item _currentItem;
        private int _stepCounter = 0;
        private Vector3 _baseScale = Vector3.one;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer itemVisual;
        [SerializeField] private SpriteRenderer beltRenderer;
        [SerializeField] private Sprite straightSprite;
        [SerializeField] private Sprite cornerSprite;
        [SerializeField] private Transform conveyorVisualTransform;

        [Header("Timing")]
        public int ticksPerStep = 1;

        Direction IItemEndpoint.InputDirection => _inputDirection;
        Direction IItemEndpoint.OutputDirection => _outputDirection;
        Item IItemEndpoint.CurrentItem
        {
            get => _currentItem;
            set
            {
                //Debug.Log("ARGE : CURRENT ITEM SET BY INTERFACE ITEMENDPOINT ITEM");
                _currentItem = value;
                StopAllCoroutines();
                StartCoroutine(UpdateItemVisual());
            }
        }

        // ARGE- ToDo: Dopplung?
        public Item CurrentItem
        {

            get => _currentItem;
            set
            {
                //Debug.Log("ARGE : CURRENT ITEM SET BY PULIC ITEM");
                _currentItem = value;
                StopAllCoroutines();
                StartCoroutine(UpdateItemVisual());
            }
        }

        public Direction InputDirection
        {
            get => _inputDirection;
            set => _inputDirection = value;
        }

        public Direction OutputDirection
        {
            get => _outputDirection;
            set => _outputDirection = value;
        }

        private void OnEnable()
        {
            if (!s_allConveyors.Contains(this))
                s_allConveyors.Add(this);

            StartCoroutine(UpdateItemVisual());
        }

        private void OnDisable()
        {
            s_allConveyors.Remove(this);
        }

        public void Init(GridCell cell)
        {
            _cell = cell;
        }

        public void SetOutputDirection(Direction dir)
        {
            OutputDirection = dir;
            UpdateShapeAndRotation();
        }

        public void SetInputDirection(Direction dir, bool updateShape = false)
        {
            InputDirection = dir;
            if (updateShape)
                UpdateShapeAndRotation();
        }

        private IEnumerator UpdateItemVisual()
        {
            if (itemVisual && _cell != null)
            {
                if (CurrentItem == null)
                {
                    GridCell iCell = GrM.GetCell(
                        _cell.Position + DirectionUtils.DirectionToOffset(OutputDirection)
                    );

                    if (iCell.Machine)
                        yield return new WaitForSeconds(0.05f);

                    itemVisual.enabled = false;
                }
                else
                {
                    itemVisual.enabled = true;
                    itemVisual.sprite = IM.GetItemSprite(CurrentItem.type);
                }
            }
        }

        public void UpdateShapeAndRotation()
        {
            if (beltRenderer == null || _cell == null)
                return;

            float angle = 0f;
            switch (OutputDirection)
            {
                case Direction.Right: angle = 0f; break;
                case Direction.Up: angle = 90f; break;
                case Direction.Left: angle = 180f; break;
                case Direction.Down: angle = 270f; break;
            }

            beltRenderer.sprite = straightSprite;
            if (conveyorVisualTransform == null)
                conveyorVisualTransform = transform;

            conveyorVisualTransform.rotation = Quaternion.Euler(0f, 0f, angle);
            conveyorVisualTransform.localScale = _baseScale;

            var lPos = _cell.Position + DirectionUtils.DirectionToOffset(DirectionUtils.GetLeft(OutputDirection));
            var rPos = _cell.Position + DirectionUtils.DirectionToOffset(DirectionUtils.GetRight(OutputDirection));
            var fPos = _cell.Position + DirectionUtils.DirectionToOffset(OutputDirection);
            var bPos = _cell.Position + DirectionUtils.DirectionToOffset(DirectionUtils.Opposite(OutputDirection));

            GridCell fCell = GrM.GetCell(fPos);
            GridCell bCell = GrM.GetCell(bPos);
            GridCell lCell = GrM.GetCell(lPos);
            GridCell rCell = GrM.GetCell(rPos);

            bool leftCorner = false;
            bool rightCorner = false;

            bool hasLeftCon = lCell != null && lCell.Conveyor != null;
            bool hasRightCon = rCell != null && rCell.Conveyor != null;

            bool hasLeftMachine = lCell != null && lCell.Machine != null;
            bool hasRightMachine = rCell != null && rCell.Machine != null;

            if (hasLeftCon)
            {
                var cellInLeftNeighborOutputDirection =
                    lCell.Position + DirectionUtils.DirectionToOffset(lCell.Conveyor.OutputDirection);

                if (lCell.Conveyor.OutputDirection != DirectionUtils.Opposite(OutputDirection) &&
                    cellInLeftNeighborOutputDirection == _cell.Position)
                {
                    leftCorner = true;
                }
            }

            if (hasRightCon)
            {
                var cellInRightNeighborOutputDirection =
                    rCell.Position + DirectionUtils.DirectionToOffset(rCell.Conveyor.OutputDirection);

                if (rCell.Conveyor.OutputDirection != DirectionUtils.Opposite(OutputDirection) &&
                    cellInRightNeighborOutputDirection == _cell.Position)
                {
                    rightCorner = true;
                }
            }

            if (hasLeftMachine)
            {
                var machine = lCell.Machine;
                MachineKind machineKind = machine.GetMachineKind();

                if (machineKind != MachineKind.Splitter && machineKind != MachineKind.Crossing)
                {
                    if (lCell.Machine.OutputDirection != DirectionUtils.Opposite(OutputDirection) &&
                        lCell.Machine.InputDirection == DirectionUtils.Opposite(DirectionUtils.GetRight(OutputDirection)))
                    {
                        leftCorner = true;
                    }
                }
                else
                {
                    if (machineKind == MachineKind.Splitter)
                    {
                        foreach (Direction dir in DirectionUtils.AllDirections())
                        {
                            if (dir != lCell.Machine.InputDirection &&
                                dir == DirectionUtils.Opposite(DirectionUtils.GetLeft(OutputDirection)) &&
                                dir != OutputDirection)
                            {
                                leftCorner = true;
                            }
                        }
                    }
                    else if (machineKind == MachineKind.Crossing)
                    {
                        Crossing cross = lCell.Machine as Crossing;
                        if (cross != null && cross.AllExtraInpuDirections.Count > 0)
                        {
                            foreach (Direction dir in DirectionUtils.AllDirections())
                            {
                                if (dir != cross.InputDirection &&
                                    dir != cross.AllExtraInpuDirections[0] &&
                                    dir == DirectionUtils.Opposite(DirectionUtils.GetLeft(OutputDirection)) &&
                                    dir != OutputDirection)
                                {
                                    leftCorner = true;
                                }
                            }
                        }
                    }
                }
            }

            if (hasRightMachine)
            {
                var machine = rCell.Machine;
                MachineKind machineKind = machine.GetMachineKind();
                if (machineKind != MachineKind.Splitter && machineKind != MachineKind.Crossing)
                {
                    if (rCell.Machine.OutputDirection != DirectionUtils.Opposite(OutputDirection) &&
                        rCell.Machine.InputDirection == DirectionUtils.Opposite(DirectionUtils.GetLeft(OutputDirection)))
                    {
                        rightCorner = true;
                    }
                }
                else
                {
                    if (machineKind == MachineKind.Splitter)
                    {
                        foreach (Direction dir in DirectionUtils.AllDirections())
                        {
                            if (dir != machine.InputDirection &&
                                dir == DirectionUtils.Opposite(DirectionUtils.GetRight(OutputDirection)) &&
                                dir != OutputDirection)
                            {
                                rightCorner = true;
                            }
                        }
                    }
                    else if (machineKind == MachineKind.Crossing)
                    {
                        Crossing cross = rCell.Machine as Crossing;
                        foreach (Direction dir in DirectionUtils.AllDirections())
                        {
                            if (dir != cross.InputDirection &&
                                dir != cross.AllExtraInpuDirections[0] &&
                                dir == DirectionUtils.Opposite(DirectionUtils.GetRight(OutputDirection)) &&
                                dir != OutputDirection)
                            {
                                rightCorner = true;
                            }
                        }
                    }
                }
            }

            bool isCorner = leftCorner || rightCorner;

            if (bCell != null && bCell.Conveyor != null &&
                bCell.Conveyor.OutputDirection == DirectionUtils.Opposite(OutputDirection) &&
                bCell.Conveyor.InputDirection == DirectionUtils.Opposite(bCell.Conveyor.OutputDirection))
            {
                isCorner = false;
            }

            if (bCell != null && bCell.Machine &&
                bCell.Machine.OutputDirection == DirectionUtils.Opposite(OutputDirection))
            {
                isCorner = false;
            }

            if (fCell != null &&
                ((fCell.Conveyor != null &&
                  fCell.Conveyor.OutputDirection == DirectionUtils.Opposite(OutputDirection)) ||
                 (fCell.Machine &&
                  fCell.Machine.OutputDirection == DirectionUtils.Opposite(OutputDirection))))
            {
                isCorner = false;
            }

            if (isCorner)
            {
                CornerVisual vis = DirectionUtils.GetCornerVisual(OutputDirection, InputDirection);

                beltRenderer.sprite = cornerSprite;
                if (conveyorVisualTransform == null)
                    conveyorVisualTransform = transform;
                conveyorVisualTransform.rotation = Quaternion.Euler(0f, 0f, vis.angle);
                conveyorVisualTransform.localScale = new Vector3(
                    _baseScale.x * (vis.flipX ? -1f : 1f),
                    _baseScale.y * (vis.flipY ? -1f : 1f),
                    _baseScale.z
                );
            }
        }
    }
}
