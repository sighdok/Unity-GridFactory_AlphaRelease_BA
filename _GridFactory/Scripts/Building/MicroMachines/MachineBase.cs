
using System.Collections.Generic;

using UnityEngine;

using MoreMountains.Feedbacks;

using GridFactory.Core;
using GridFactory.Grid;
using GridFactory.Directions;
using GridFactory.Conveyor;
using GridFactory.Inventory;

namespace GridFactory.Machines
{
    public abstract class MachineBase : MonoBehaviour, IItemEndpoint
    {
        private static TickManager TM => TickManager.Instance;
        protected static GridManager GM => GridManager.Instance;
        protected static InventoryManager IM => InventoryManager.Instance;

        [SerializeField] protected int ticksPerProcess = 4;

        [Header("I/O")]
        [SerializeField] protected bool hasInput = true;
        [SerializeField] protected bool hasOutput = true;
        [SerializeField] protected Direction inputDirection = Direction.Left;
        [SerializeField] protected Direction outputDirection = Direction.Right;

        [Header("Visuals")]
        [SerializeField] protected Transform arrowsParent;
        [SerializeField] protected GameObject inputArrowPrefab;    // Prefab für EINEN Input-Pfeil
        [SerializeField] protected GameObject outputArrowPrefab;   // Prefab für EINEN Output-Pfeil
        [SerializeField] private Transform conveyorMachineVisualTransform;
        [SerializeField] private Transform progressBar;

        [SerializeField] protected MMF_Player burnItemEffect;

        protected GridCell _cell;
        protected Direction _baseFacing = Direction.Right;
        protected Direction _baseOutputSide = Direction.Right;
        protected Direction _facing = Direction.Right;
        private Item _currentItem;
        private MachineWithRecipeBase _mwrbRef = null;

        protected int _calculatedTicksPerProcess = 1;
        protected float _processProgress = 0f;
        protected float _processingTickCounter;
        protected bool _isProcessing = false;

        Direction IItemEndpoint.InputDirection => inputDirection;
        Direction IItemEndpoint.OutputDirection => outputDirection;

        public int TicksPerProcess
        {
            get => _calculatedTicksPerProcess;
        }

        public Item CurrentItem
        {
            get => _currentItem;
            set => _currentItem = value;
        }

        public Direction Facing
        {
            get => _facing;
        }

        public Direction InputDirection // blueprint- calculations without endpoint
        {
            get => inputDirection;
        }

        public Direction OutputDirection // blueprint- calculations without endpoint
        {
            get => outputDirection;
        }

        public bool HasInput // blueprint- calculations without endpoint
        {
            get => hasInput;
        }

        public bool HasOutput // blueprint- calculations without endpoint
        {
            get => hasOutput;
        }

        public virtual List<Direction> AllInputDirections()
        {
            List<Direction> dirs = new List<Direction>
            {
                inputDirection
            };
            return dirs;
        }

        public virtual List<Direction> AllOutputDirections()
        {

            List<Direction> dirs = new List<Direction>
            {
                outputDirection
            };
            return dirs;
        }

        private void HandleTick() => TickInternal();
        protected abstract bool CanStartProcess();
        protected abstract void StartProcess();
        protected abstract bool FinishProcess();
        protected abstract void RotateAdditionalInputs(Direction dir);

        protected virtual void OnEnable()
        {
            //_cell = GM.GetCellFromWorld(transform.position);

            RecalculateTicksPerProcess();

            TM.OnTick += HandleTick;

            UpdateProcessVisual(0f);

            if (this is MachineWithRecipeBase recipeMachine)
                _mwrbRef = recipeMachine;
        }

        protected virtual void OnDisable()
        {
            TM.OnTick -= HandleTick;
        }

        public void Init(GridCell cell)
        {
            _cell = cell;
            RotateAdditionalInputs(inputDirection);
        }

        public void SetFacing(Direction facing, bool updateArrows = true)
        {
            _facing = facing;
            UpdateRotationAndIO();

            if (updateArrows)
                SetArrowsIO();
        }
        public virtual void ResetSimulation()
        {
            _isProcessing = false;
            CurrentItem = null;
            _processingTickCounter = 0;
            _processProgress = 0f;
            UpdateProcessVisual(0f);
        }

        private void TickInternal()
        {
            if (_calculatedTicksPerProcess <= 0)
                RecalculateTicksPerProcess();

            if (_mwrbRef != null)
                HandleRecipeTick();
            else
                HandleSimpleTick();
        }

        private void HandleSimpleTick()
        {
            if (!_isProcessing)
            {
                if (CanStartProcess())
                {
                    StartProcess();

                    _isProcessing = true;
                    _processingTickCounter = 0;
                    _processProgress = 0f;

                    UpdateProcessVisual(0f);
                }
            }
            else
            {
                _processingTickCounter++;
                _processProgress = Mathf.Clamp01(_processingTickCounter / (float)_calculatedTicksPerProcess);
                UpdateProcessVisual(_processProgress);

                if (_processingTickCounter >= _calculatedTicksPerProcess)
                {
                    if (FinishProcess())
                    {
                        _isProcessing = false;
                        _processingTickCounter = 0;
                        _processProgress = 0f;

                        UpdateProcessVisual(0f);
                    }
                    else
                    {
                        _processingTickCounter = _calculatedTicksPerProcess;
                        _processProgress = 1f;

                        UpdateProcessVisual(1f);
                    }
                }
            }
        }

        private void HandleRecipeTick()
        {
            if (!_mwrbRef.ProcessingRecipe)
            {
                if (_mwrbRef.CanProcessRecipe())
                {
                    StartProcess();

                    _mwrbRef.ProcessingRecipe = true;
                    _processingTickCounter = 0;
                    _processProgress = 0f;

                    UpdateProcessVisual(0f);
                }
                else
                {
                    _mwrbRef.TryPullIntoProcessing();
                }
            }
            else
            {
                _processingTickCounter++;
                _processProgress = Mathf.Clamp01(_processingTickCounter / (float)_calculatedTicksPerProcess);
                UpdateProcessVisual(_processProgress);

                if (_processingTickCounter >= _calculatedTicksPerProcess)
                {
                    if (FinishProcess())
                    {
                        _mwrbRef.ProcessingRecipe = false;
                        _processingTickCounter = 0;
                        _processProgress = 0f;

                        UpdateProcessVisual(0f);
                    }
                    else
                    {
                        _processingTickCounter = _calculatedTicksPerProcess;
                        _processProgress = 1f;

                        UpdateProcessVisual(1f);
                    }
                }
            }
        }

        public virtual void RecalculateTicksPerProcess()
        {
            _calculatedTicksPerProcess = Mathf.Max(1, Mathf.RoundToInt(ticksPerProcess));
        }


        protected bool CanOutput(Direction dir, bool requestByCrossing = false)
        {
            if (!hasOutput || _cell == null)
                return false;

            GridCell targetCell = GetAdjacentCell(dir);
            if (targetCell == null)
                return false;

            if (targetCell.Machine is Crossing crossing)
            {
                return crossing.HasFreeLane(dir);
            }
            else if (targetCell.Machine is MachineWithRecipeBase mwrb && requestByCrossing)
            {
                if (!mwrb.ProcessingRecipe)
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

                if (expectedUpstreamPos != _cell.Position)
                    return false;
                if (endpoint.CurrentItem != null && endpoint.CurrentItem.type != ItemType.None)
                    return false;

                return true;
            }
        }

        protected bool OutputToNeighbor(Direction dir, Item currentItem)
        {
            if (!hasOutput || _cell == null || currentItem == null)
                return false;

            GridCell targetCell = GetAdjacentCell(dir);
            if (targetCell == null)
                return false;
            if (targetCell.Machine is Crossing crossing)
            {
                return crossing.TryPushItemToLane(dir, currentItem);
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
                    endpoint.CurrentItem = currentItem;
                    return true;
                }
                return false;
            }
        }

        protected bool IsAllowedEndpointForMachine(IItemEndpoint endpoint)
        {
            if (endpoint == null)
                return false;

            if (endpoint is ConveyorBase)
                return true;

            if (endpoint is MachineBase machine)
                return machine.GetMachineKind() == MachineKind.Crossing;

            return false;
        }

        protected GridCell GetAdjacentCell(Direction dir)
        {
            if (_cell == null)
                return null;

            Vector2Int offset = DirectionUtils.DirectionToOffset(dir);
            return GM.GetCell(_cell.Position + offset);
        }

        protected virtual void UpdateProcessVisual(float normalized)
        {
            if (progressBar == null)
                return;

            normalized = Mathf.Clamp01(normalized);
            var scale = progressBar.localScale;
            scale.x = normalized;
            progressBar.localScale = scale;
        }

        private void UpdateRotationAndIO()
        {
            float angle = 0f;
            switch (_facing)
            {
                case Direction.Right: angle = 0f; break;
                case Direction.Up: angle = 90f; break;
                case Direction.Left: angle = 180f; break;
                case Direction.Down: angle = 270f; break;
            }

            if (conveyorMachineVisualTransform)
                conveyorMachineVisualTransform.rotation = Quaternion.Euler(0f, 0f, angle);

            switch (_facing)
            {
                case Direction.Right:
                    inputDirection = Direction.Left;
                    outputDirection = Direction.Right;
                    break;
                case Direction.Left:
                    inputDirection = Direction.Right;
                    outputDirection = Direction.Left;
                    break;
                case Direction.Up:
                    inputDirection = Direction.Down;
                    outputDirection = Direction.Up;
                    break;
                case Direction.Down:
                    inputDirection = Direction.Up;
                    outputDirection = Direction.Down;
                    break;
            }

            RotateAdditionalInputs(inputDirection);
            SetArrowsIO();
        }

        private void SetArrowsIO()
        {
            Transform parent = arrowsParent != null ? arrowsParent : transform;
            if (inputArrowPrefab != null)
            {
                var arrow = Instantiate(inputArrowPrefab, parent);

                arrow.transform.localPosition =
                    DirectionUtils.DirectionToOffsetVector3(inputDirection) * 0.5f;
                arrow.transform.localRotation = Quaternion.Euler(
                    0, 0,
                    DirectionUtils.DirToAngle(DirectionUtils.Opposite(inputDirection))
                );
            }
            if (outputArrowPrefab != null)
            {
                var arrow = Instantiate(outputArrowPrefab, parent);

                arrow.transform.localPosition =
                            DirectionUtils.DirectionToOffsetVector3(outputDirection) * 0.5f;
                arrow.transform.localRotation = Quaternion.Euler(
                    0, 0,
                    DirectionUtils.DirToAngle(outputDirection)
                );
            }
        }

        public virtual MachineKind GetMachineKind()
        {
            if (this is Sawmill) return MachineKind.Sawmill;
            if (this is Mason) return MachineKind.Mason;
            if (this is Oven) return MachineKind.Oven;
            if (this is Smelter) return MachineKind.Smelter;
            if (this is Splitter) return MachineKind.Splitter;
            if (this is Merger) return MachineKind.Merger;
            if (this is Crossing) return MachineKind.Crossing;
            if (this is PortMarker) return MachineKind.PortMarker;

            return MachineKind.None;
        }
    }
}