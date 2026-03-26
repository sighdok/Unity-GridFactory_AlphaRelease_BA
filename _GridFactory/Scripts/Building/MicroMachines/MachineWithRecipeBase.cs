using System.Collections.Generic;

using UnityEngine;

using GridFactory.Core;
using GridFactory.Grid;
using GridFactory.Directions;

namespace GridFactory.Machines
{


    /// <summary>
    /// Basisklasse für Maschinen mit Rezept-Logik.
    /// - Konsumiert Input-Items (Conveyor ODER Crossing, Pull ODER Push)
    /// - Trackt Rezept-Fortschritt
    /// - Erzeugt KEINEN Output selbst (das machen Unterklassen wie Press)
    /// </summary>
    public abstract class MachineWithRecipeBase : MachineBase, IItemEndpoint
    {
        [Header("Recipes")]
        [SerializeField] protected List<RecipeDefinition> recipeList = new List<RecipeDefinition>();
        [Header("Additional Inputs")]
        [SerializeField] private List<Direction> extraInputDirections = new List<Direction>();

        private RecipeDefinition _currentRecipe;
        private List<RecipeItemCounter> _completedItemArrival = new List<RecipeItemCounter>();
        private readonly List<GameObject> _spawnedInputArrows = new List<GameObject>();
        private readonly List<GameObject> _spawnedOutputArrows = new List<GameObject>();
        private readonly List<Direction> _inputSidesMeta = new List<Direction>();
        private readonly List<Direction> _inputSidesMetaBase = new();
        private bool _processingRecipe = false;
        private bool _hasPendingOutput = false;

        Item IItemEndpoint.CurrentItem
        {
            get => null;
            set => base.CurrentItem = value;
        }

        public List<RecipeDefinition> AllRecipes
        {
            get => recipeList;
        }

        public bool ProcessingRecipe
        {
            get => _processingRecipe;
            set => _processingRecipe = value;
        }

        public override List<Direction> AllInputDirections()
        {
            List<Direction> dirs = new List<Direction> {
                inputDirection
            };
            foreach (var dir in extraInputDirections)
                dirs.Add(dir);
            return dirs;
        }

        public RecipeDefinition CurrentRecipe
        {
            get => _currentRecipe;
            set
            {
                _currentRecipe = value;
                ResetCompletedItemArrival();
                RecalculateTicksPerProcess();
            }
        }


        protected override void OnEnable()
        {
            base.OnEnable();
            if (recipeList.Count > 0 && _currentRecipe == null)
                _currentRecipe = recipeList[0];

            ResetCompletedItemArrival();
        }

        protected override void RotateAdditionalInputs(Direction primaryInputDirection)
        {
            if (extraInputDirections.Count == 0)
                return;

            switch (primaryInputDirection)
            {
                case Direction.Right:
                    extraInputDirections[0] = Direction.Down;
                    if (extraInputDirections.Count > 1)
                        extraInputDirections[1] = Direction.Up;
                    break;

                case Direction.Left:
                    extraInputDirections[0] = Direction.Up;
                    if (extraInputDirections.Count > 1)
                        extraInputDirections[1] = Direction.Down;
                    break;

                case Direction.Up:
                    extraInputDirections[0] = Direction.Right;
                    if (extraInputDirections.Count > 1)
                        extraInputDirections[1] = Direction.Left;
                    break;

                case Direction.Down:
                    extraInputDirections[0] = Direction.Left;
                    if (extraInputDirections.Count > 1)
                        extraInputDirections[1] = Direction.Right;
                    break;
            }
        }

        public override void ResetSimulation()
        {
            base.ResetSimulation();
            ResetCompletedItemArrival();
            _hasPendingOutput = false;
            _processingRecipe = false;
        }

        public override void RecalculateTicksPerProcess()
        {
            if (_currentRecipe != null)
                _calculatedTicksPerProcess = Mathf.Max(1, Mathf.RoundToInt(ticksPerProcess * _currentRecipe.machineProcessingTimeMultiplikator));
            else
                _calculatedTicksPerProcess = Mathf.Max(1, Mathf.RoundToInt(ticksPerProcess));
        }

        protected void ResetCompletedItemArrival()
        {
            _completedItemArrival.Clear();

            if (_currentRecipe == null)
                return;

            foreach (RecipeItem inputItem in _currentRecipe.inputItems)
                _completedItemArrival.Add(new RecipeItemCounter(inputItem.item, inputItem.amount));
        }


        public bool CanProcessRecipe()
        {
            if (_completedItemArrival.Count == 0)
                return false;

            foreach (RecipeItemCounter counter in _completedItemArrival)
                if (!counter.completed)
                    return false;

            return true;
        }

        protected override void StartProcess()
        {
            _hasPendingOutput = _currentRecipe != null;
        }

        protected override bool FinishProcess()
        {
            if (!_hasPendingOutput && CurrentItem == null)
                return true;

            if (_hasPendingOutput)
            {
                if (_currentRecipe == null || _currentRecipe.outputItems == null)
                {
                    _hasPendingOutput = false;
                    ResetCompletedItemArrival();
                    return true;
                }

                if (CurrentItem == null)
                    CurrentItem = new Item(_currentRecipe.outputItems[0].item.type);

                _hasPendingOutput = false;
            }

            if (CurrentItem != null)
            {
                if (CanOutput(outputDirection) && OutputToNeighbor(outputDirection, CurrentItem))
                {
                    CurrentItem = null;
                    ResetCompletedItemArrival();
                    return true;
                }

                return false;
            }

            ResetCompletedItemArrival();
            return true;
        }


        protected bool AllowIncomingItem(Item incomingItem)
        {
            if (_currentRecipe == null || incomingItem == null)
                return false;

            foreach (RecipeItem possibleItem in _currentRecipe.inputItems)
                if (possibleItem.item.type == incomingItem.type)
                    return true;

            return false;
        }

        protected bool TryToStoreIncomingItem(Item item)
        {
            foreach (RecipeItemCounter counter in _completedItemArrival)
            {
                if (counter.item.type != item.type)
                    continue;

                if (counter.current < counter.target)
                {
                    counter.current++;
                    if (counter.current >= counter.target)
                        counter.completed = true;
                }

                return true;
            }
            return false;
        }

        public void TryPullIntoProcessing()
        {
            if (_currentRecipe == null)
                return;

            foreach (var dir in AllInputDirections())
            {
                GridCell inCell = GetAdjacentCell(dir);
                if (inCell == null)
                    continue;

                if (inCell.Machine is Crossing crossing)
                {
                    if (crossing.HasItemOnLane(dir))
                    {
                        crossing.TryPullItemFromLane(dir, out var crossIncomingItem);
                        if (crossIncomingItem != null)
                        {
                            if (AllowIncomingItem(crossIncomingItem))
                                TryToStoreIncomingItem(crossIncomingItem);
                            else
                                if (burnItemEffect)
                                    burnItemEffect.PlayFeedbacks();
                        }
                    }
                }
                else
                {
                    IItemEndpoint endpoint = inCell.ItemEndpoint;
                    if (endpoint == null || endpoint.CurrentItem == null)
                        continue;

                    if (!IsAllowedEndpointForMachine(endpoint))
                        continue;

                    Vector2Int expectedDownstream =
                        inCell.Position + DirectionUtils.DirectionToOffset(endpoint.OutputDirection);

                    if (expectedDownstream != _cell.Position)
                        continue;

                    Item incomingItem = endpoint.CurrentItem;
                    endpoint.CurrentItem = null;

                    if (AllowIncomingItem(incomingItem))
                        TryToStoreIncomingItem(incomingItem);
                    else
                        if (burnItemEffect)
                            burnItemEffect.PlayFeedbacks();
                }
            }
        }

        public void BuildInputOutputSides()
        {
            _baseOutputSide = _facing;

            _inputSidesMetaBase.Clear();
            _inputSidesMeta.Clear();

            if (!_inputSidesMetaBase.Contains(inputDirection))
                _inputSidesMetaBase.Add(inputDirection);

            foreach (var dir in extraInputDirections)
                if (!_inputSidesMetaBase.Contains(dir))
                    _inputSidesMetaBase.Add(dir);

            DirectionUtils.IOOrientation.RotateIOByFacing(
                _baseOutputSide,
                _facing,
                _inputSidesMetaBase,
                _baseOutputSide,
                _inputSidesMeta,
                out outputDirection
            );

            if (_inputSidesMeta.Count > 0)
                inputDirection = _inputSidesMeta[0];
        }

        public void UpdateArrowPositions()
        {
            foreach (var go in _spawnedInputArrows)
                if (go != null) Destroy(go);
            _spawnedInputArrows.Clear();

            foreach (var go in _spawnedOutputArrows)
                if (go != null) Destroy(go);
            _spawnedOutputArrows.Clear();

            Transform parent = arrowsParent != null ? arrowsParent : transform;

            if (inputArrowPrefab != null && _inputSidesMeta.Count > 0)
            {
                foreach (var dir in _inputSidesMeta)
                {
                    var arrow = Instantiate(inputArrowPrefab, parent);

                    arrow.transform.localPosition =
                        DirectionUtils.DirectionToOffsetVector3(dir) * 0.5f;
                    arrow.transform.localRotation = Quaternion.Euler(
                        0, 0,
                        DirectionUtils.DirToAngle(DirectionUtils.Opposite(dir))
                    );

                    _spawnedInputArrows.Add(arrow);
                }
            }

            if (outputArrowPrefab != null)
            {
                var dir = outputDirection;
                var arrow = Instantiate(outputArrowPrefab, parent);

                arrow.transform.localPosition =
                    DirectionUtils.DirectionToOffsetVector3(dir) * 0.5f;
                arrow.transform.localRotation = Quaternion.Euler(
                    0, 0,
                    DirectionUtils.DirToAngle(dir)
                );

                _spawnedOutputArrows.Add(arrow);
            }
        }
    }
}
