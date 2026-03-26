
using System.Collections.Generic;

using UnityEngine;

using MoreMountains.Feedbacks;

using GridFactory.Core;
using GridFactory.Grid;
using GridFactory.Conveyor;
using GridFactory.Directions;
using GridFactory.Inventory;

namespace GridFactory.Meta
{
    public abstract class MetaMachineBase : MonoBehaviour, IItemEndpoint
    {
        private static TickManager TM => TickManager.Instance;
        protected static EnergyManager EM => EnergyManager.Instance;
        protected static MetaGridManager MGM => MetaGridManager.Instance;
        protected static InventoryManager IM => InventoryManager.Instance;

        public string saveId;

        [SerializeField] protected int baseTicksPerProcess = 4;

        [Header("I/O")]
        [SerializeField] protected bool hasInput = true;
        [SerializeField] protected bool hasOutput = true;
        [SerializeField] protected Direction inputDirection = Direction.Left;
        [SerializeField] protected Direction outputDirection = Direction.Right;

        [Header("Visuals")]
        [SerializeField] protected Transform arrowsParent;
        [SerializeField] protected GameObject inputArrowPrefab;    // Prefab für EINEN Input-Pfeil
        [SerializeField] protected GameObject outputArrowPrefab;   // Prefab für EINEN Output-Pfeil
        [SerializeField] protected MMF_Player inputEffect;
        [SerializeField] protected MMF_Player outputEffect;
        [SerializeField] protected MMF_Player burnItemEffect;
        [SerializeField] protected MMF_Player cantOutputEffect;
        [SerializeField] protected Transform progressBar;
        [SerializeField] private Transform conveyorMachineVisualTransform;

        [Header("Energy")]
        [SerializeField] protected bool requiresEnergy = false;
        [SerializeField, Min(0f)] private float upkeepPerSecond = 0f;
        //[SerializeField] private bool useCachedUpkeep = false;

        protected readonly List<GameObject> _spawnedInputArrows = new List<GameObject>();
        protected readonly List<GameObject> _spawnedOutputArrows = new List<GameObject>();

        protected MetaCell _cell;
        protected Direction _baseFacing = Direction.Right;
        protected Direction _baseOutputSide = Direction.Right;
        protected Direction _facing = Direction.Right;
        private Item _currentItem;
        private MetaResearchCenter rsRef = null;

        protected int _calculatedTicksPerProcess = 1;
        protected bool _isProcessing = false;
        // protected float upkeepPerTickCached = 0f;
        protected float _processProgress = 0f;
        protected float _processingTickCounter;

        Direction IItemEndpoint.InputDirection => inputDirection;
        Direction IItemEndpoint.OutputDirection => outputDirection;

        public Item CurrentItem
        {
            get => _currentItem;
            set => _currentItem = value;
        }

        public Direction Facing
        {
            get => _facing;
        }

        public float DemandPerTick
        {
            get
            {
                if (!requiresEnergy)
                    return 0f;

                float tickInterval = Mathf.Max(0.0001f, TM.TickInterval);
                return Mathf.Max(0f, upkeepPerSecond) * tickInterval;
            }
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

        /*
        public void SetCurrentItemByType(ItemType type)
        {
            _currentItem = new Item(type);
        }
        */

        private void HandleTick() => TickInternal();
        protected virtual void OnProcessingTick(float deltaProgress01) { }
        protected abstract bool CanStartProcess();
        protected abstract void StartProcess();
        protected abstract bool FinishProcess();
        protected abstract void RotateAdditionalInputs(Direction dir);

        protected virtual void OnEnable()
        {
            if (SaveLoadContext.IsLoading)
                return;

            ActivateAfterLoadOrPlace();
        }

        protected virtual void OnDisable()
        {
            if (SaveLoadContext.IsLoading)
                return;

            TM.OnTick -= HandleTick;
        }

        void Start()
        {
            //_cell = MGM.GetCellFromWorld(transform.position);
        }

        public void ActivateAfterLoadOrPlace(bool reset = true)
        {
            RecalculateTicksPerProcess();

            TM.OnTick += HandleTick;

            if (this is MetaResearchCenter reseachCenter)
                rsRef = reseachCenter;

            if (reset)
                ResetSimulation();
        }

        private void TickInternal()
        {
            RecalculateTicksPerProcess();

            if (rsRef != null)
                HandleResearchTick();
            else
                HandleSimpleTick();
        }

        public void Init(MetaCell cell)
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

            if (this is not MetaBlueprintModule)
                CurrentItem = null;
            _processingTickCounter = 0;
            _processProgress = 0f;

            UpdateProcessVisual(0f);
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
                float ratio = 1f;
                if (requiresEnergy)
                    ratio = EM.PowerRatio;

                float before = _processingTickCounter;
                _processingTickCounter += Mathf.Clamp(ratio, 0f, 1f);

                float deltaProgress01 = (_processingTickCounter - before) / (float)_calculatedTicksPerProcess;
                if (deltaProgress01 > 0f)
                    OnProcessingTick(deltaProgress01);

                _processProgress = Mathf.Clamp01(_processingTickCounter / (float)_calculatedTicksPerProcess);
                UpdateProcessVisual(_processProgress);

                if (_processingTickCounter >= _calculatedTicksPerProcess)
                {
                    if (FinishProcess())
                    {
                        _isProcessing = false;
                        _processingTickCounter = 0f;
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

        private void HandleResearchTick()
        {
            if (!rsRef.ProcessingResearch)
            {
                if (rsRef.CanProcessResearch())
                {
                    StartProcess();

                    rsRef.ProcessingResearch = true;
                    _processingTickCounter = 0;
                    _processProgress = 0f;

                    UpdateProcessVisual(0f);
                }
                else
                {
                    rsRef.TryPullIntoProcessing();
                }
            }
            else
            {
                float ratio = 1f;
                if (requiresEnergy)
                    ratio = EM.PowerRatio;

                float before = _processingTickCounter;
                _processingTickCounter += Mathf.Clamp(ratio, 0f, 1f);

                float deltaProgress01 = (_processingTickCounter - before) / (float)_calculatedTicksPerProcess;
                if (deltaProgress01 > 0f)
                    OnProcessingTick(deltaProgress01);

                _processProgress = Mathf.Clamp01(_processingTickCounter / (float)_calculatedTicksPerProcess);
                UpdateProcessVisual(_processProgress);

                if (_processingTickCounter >= _calculatedTicksPerProcess)
                {
                    if (FinishProcess())
                    {
                        rsRef.ProcessingResearch = false;
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

        protected void RecalculateTicksPerProcess()
        {
            _calculatedTicksPerProcess = Mathf.Max(1, Mathf.RoundToInt(baseTicksPerProcess));
        }

        protected bool CanOutput(Direction dir, bool requestByCrossing = false)
        {
            if (!hasOutput || _cell == null)
                return false;

            MetaCell targetCell = GetAdjacentCell(dir);
            if (targetCell == null)
                return false;

            if (targetCell.Machine is MetaCrossing crossing)
            {
                return crossing.HasFreeLane(dir);
            }
            else if (targetCell.Machine is MetaResearchCenter mwrb && requestByCrossing)
            {
                if (!mwrb.ProcessingResearch)
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

            MetaCell targetCell = GetAdjacentCell(dir);
            if (targetCell == null)
                return false;

            if (targetCell.Machine is MetaCrossing crossing)
            {
                return crossing.TryPushItemToLane(dir, currentItem);
            }

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

        protected bool IsAllowedEndpointForMachine(IItemEndpoint endpoint)
        {
            if (endpoint == null)
                return false;
            if (endpoint is MetaConveyorBase)
                return true;
            if (endpoint is MetaMachineBase machine)
                return machine.GetMetaKind() == MetaKind.Crossing;
            return false;
        }

        protected MetaCell GetAdjacentCell(Direction dir)
        {
            if (_cell == null)
                return null;

            Vector2Int offset = DirectionUtils.DirectionToOffset(dir);
            return MGM.GetCell(_cell.Position + offset);
        }

        protected virtual void UpdateProcessVisual(float normalized)
        {
            if (progressBar)
            {
                normalized = Mathf.Clamp01(normalized);
                var scale = progressBar.localScale;
                scale.x = normalized;
                progressBar.localScale = scale;
            }
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
        }

        protected void SetArrowsIO()
        {
            foreach (var go in _spawnedInputArrows)
                if (go != null) Destroy(go);
            _spawnedInputArrows.Clear();

            foreach (var go in _spawnedOutputArrows)
                if (go != null) Destroy(go);
            _spawnedOutputArrows.Clear();

            Transform parent = arrowsParent != null ? arrowsParent : transform;
            if (inputArrowPrefab != null)
            {
                var arrow = Instantiate(inputArrowPrefab, parent);

                arrow.transform.localPosition = DirectionUtils.DirectionToOffsetVector3(inputDirection) * 0.5f;

                arrow.transform.localRotation = Quaternion.Euler(
                    0, 0,
                    DirectionUtils.DirToAngle(DirectionUtils.Opposite(inputDirection))
                );

                _spawnedInputArrows.Add(arrow);
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

                _spawnedOutputArrows.Add(arrow);
            }
        }

        public void PauseEffects()
        {
            if (outputEffect != null)
                outputEffect.transform.gameObject.SetActive(false);
        }

        public void ResumeEffects()
        {
            if (outputEffect != null)
                outputEffect.transform.gameObject.SetActive(true);
        }

        public virtual MetaKind GetMetaKind()
        {
            if (this is MetaResourceNode) return MetaKind.Resource;
            if (this is MetaBlueprintModule) return MetaKind.Blueprint;
            if (this is MetaMarket) return MetaKind.Market;
            if (this is MetaPowerPlant) return MetaKind.PowerPlant;
            if (this is MetaSplitter) return MetaKind.Splitter;
            if (this is MetaMerger) return MetaKind.Merger;
            if (this is MetaCrossing) return MetaKind.Crossing;
            return MetaKind.None;
        }
    }
}