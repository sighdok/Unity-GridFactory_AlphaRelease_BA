using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using GridFactory.Core;
using GridFactory.Grid;
using GridFactory.Blueprints;
using GridFactory.Directions;
using Unity.Android.Gradle;
using Unity.VisualScripting;

namespace GridFactory.Meta
{
    public class MetaBlueprintModule : MetaMachineBase, IEnergyConsumer
    {
        protected static BlueprintManager BM => BlueprintManager.Instance;

        public BlueprintDefinition blueprint;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer itemVisual;

        private readonly List<Direction> _inputSidesMeta = new List<Direction>();
        private readonly List<Direction> _inputSidesMetaBase = new();
        private readonly Dictionary<ItemType, int> _requiredAmounts = new Dictionary<ItemType, int>();
        private readonly Dictionary<ItemType, int> _bufferedAmounts = new Dictionary<ItemType, int>();
        private bool _hasPendingOutput;

        private int _rotationSteps;

        protected override void RotateAdditionalInputs(Direction dir) { }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        public void UpdateBlueprint(BlueprintDefinition bp)
        {
            blueprint = bp;

            BuildRecipeRequirementsFromBlueprint();
            BuildMetaInputOutputSides();
            SetOutputItem();

            hasInput = _inputSidesMeta.Count > 0;
            hasOutput = blueprint.hasOutputPort;
            baseTicksPerProcess = (int)blueprint.ticksPerProcess;
            _bufferedAmounts.Clear();
            RecalculateTicksPerProcess();

            //upkeepPerTickCached = BM.UpkeepForBlueprint(blueprint);
            UpdateArrowPositions();
        }

        public void InitWithBlueprint(BlueprintDefinition bp)
        {
            blueprint = bp;

            BuildRecipeRequirementsFromBlueprint();
            BuildMetaInputOutputSides();
            SetOutputItem();

            hasInput = _inputSidesMeta.Count > 0;
            hasOutput = blueprint.hasOutputPort;
            baseTicksPerProcess = (int)blueprint.ticksPerProcess;

            RecalculateTicksPerProcess();

            _bufferedAmounts.Clear();
            //upkeepPerTickCached = BM.UpkeepForBlueprint(blueprint);
            UpdateArrowPositions();
        }

        public override void ResetSimulation()
        {
            base.ResetSimulation();
            if (requiresEnergy)
                EM.UnregisterConsumer(this);
            _hasPendingOutput = false;
            _bufferedAmounts.Clear();
        }

        public void ResetBlueprint()
        {
            _baseOutputSide = Direction.Right;

            _inputSidesMeta.Clear();
            _requiredAmounts.Clear();

            _bufferedAmounts.Clear();
            _inputSidesMetaBase.Clear();

            foreach (var go in _spawnedInputArrows)
                if (go != null) Destroy(go);
            _spawnedInputArrows.Clear();

            // Existierende Output-Pfeile löschen
            foreach (var go in _spawnedOutputArrows)
                if (go != null) Destroy(go);
            _spawnedOutputArrows.Clear();

            baseTicksPerProcess = 9999;
            blueprint = null;
            CurrentItem = new Item(ItemType.None);

            SetOutputItem();
            ResetSimulation();
        }

        protected override bool CanStartProcess()
        {
            if (!hasInput || !hasOutput || _hasPendingOutput)
                return false;

            TryCollectFromAllInputSides();

            if (!HasFullBatch())
                return false;

            ConsumeOneBatch();
            _hasPendingOutput = true;
            return true;
        }

        protected override void StartProcess()
        {
            if (requiresEnergy)
                EM.RegisterConsumer(this);
            return;
        }

        protected override bool FinishProcess()
        {
            if (!_hasPendingOutput)
                return true;

            Item item = new Item(CurrentItem.type);

            if (OutputToNeighbor(outputDirection, item))
            {

                _hasPendingOutput = false;
                if (outputEffect)
                    outputEffect.PlayFeedbacks();
                if (requiresEnergy)
                    EM.UnregisterConsumer(this);
                return true;
            }

            return false;
        }

        private void TryCollectFromAllInputSides()
        {
            if (_inputSidesMeta.Count == 0)
                return;


            foreach (var dir in _inputSidesMeta)
            {

                MetaCell inCell = GetAdjacentCell(dir);
                if (inCell == null)
                    continue;

                bool itemNotRequired = false;
                Item item = null;
                if (inCell.Machine is MetaCrossing crossing)
                {
                    if (crossing.HasItemOnLane(dir))
                    {
                        crossing.TryPullItemFromLane(dir, out var crossIncomingItem);
                        if (crossIncomingItem != null)
                        {
                            item = crossIncomingItem;


                            if (!_requiredAmounts.ContainsKey(item.type))
                                itemNotRequired = true;

                            bool inputSideCheck = false;
                            foreach (var port in blueprint.inputPorts)
                            {
                                var originalFacing = port.facing;
                                Direction rotatedPortFacing = DirectionUtils.RotateCW(port.facing, _rotationSteps);

                                if (rotatedPortFacing == DirectionUtils.Opposite(dir) && port.itemType == item.type)
                                    inputSideCheck = true;
                            }
                            if (!inputSideCheck)
                                itemNotRequired = true;

                            if (itemNotRequired)
                            {
                                if (burnItemEffect)
                                    burnItemEffect.PlayFeedbacks();
                                continue;
                            }

                            if (!_bufferedAmounts.ContainsKey(item.type))
                                _bufferedAmounts[item.type] = 0;

                            _bufferedAmounts[item.type]++;
                        }
                    }
                }
                else
                {
                    var belt = inCell.Conveyor;
                    if (belt == null || belt.CurrentItem == null || belt.OutputDirection != DirectionUtils.Opposite(dir))
                        continue;

                    item = belt.CurrentItem;
                    belt.CurrentItem = null;

                    if (!_requiredAmounts.ContainsKey(item.type))
                        itemNotRequired = true;

                    bool inputSideCheck = false;
                    foreach (var port in blueprint.inputPorts)
                    {
                        var originalFacing = port.facing;
                        Direction rotatedPortFacing = DirectionUtils.RotateCW(port.facing, _rotationSteps);

                        if (rotatedPortFacing == DirectionUtils.Opposite(dir) && port.itemType == item.type)
                            inputSideCheck = true;
                    }
                    if (!inputSideCheck)
                        itemNotRequired = true;

                    if (itemNotRequired)
                    {
                        if (burnItemEffect)
                            burnItemEffect.PlayFeedbacks();
                        continue;
                    }

                    if (!_bufferedAmounts.ContainsKey(item.type))
                        _bufferedAmounts[item.type] = 0;
                    _bufferedAmounts[item.type]++;

                    if (TutorialGridFactoryController.Instance)
                        TutorialGridFactoryController.Instance.BlueprintModuleReceivedItem(item.type);
                }
                if (item != null)
                    if (TutorialGridFactoryController.Instance)
                        TutorialGridFactoryController.Instance.BlueprintModuleReceivedItem(item.type);
            }
        }

        private bool HasFullBatch()
        {
            if (_requiredAmounts.Count == 0)
                return false;

            foreach (var kvp in _requiredAmounts)
            {
                var type = kvp.Key;
                int required = kvp.Value;

                int current;
                _bufferedAmounts.TryGetValue(type, out current);

                if (current < required)
                    return false;
            }

            return true;
        }

        private void ConsumeOneBatch()
        {
            foreach (var kvp in _requiredAmounts.ToList())
            {
                var type = kvp.Key;
                int required = kvp.Value;

                int current;
                _bufferedAmounts.TryGetValue(type, out current);

                current -= required;
                if (current < 0) current = 0;

                _bufferedAmounts[type] = current;
            }
        }

        private void BuildMetaInputOutputSides()
        {
            if (blueprint == null)
                return;

            _inputSidesMetaBase.Clear();
            _inputSidesMeta.Clear();
            _baseFacing = Direction.Right;

            if (blueprint.inputPorts != null)
            {
                foreach (var port in blueprint.inputPorts)
                {
                    var side = DirectionUtils.Opposite(port.facing);
                    if (!_inputSidesMetaBase.Contains(side))
                        _inputSidesMetaBase.Add(side);
                }
            }

            if (blueprint.hasOutputPort)
                _baseOutputSide = blueprint.outputPort.facing;


            DirectionUtils.IOOrientation.RotateIOByFacing(
                _baseFacing,
                _facing,
                _inputSidesMetaBase,
                _baseOutputSide,
                _inputSidesMeta,
                out outputDirection
            );

            _rotationSteps = DirectionUtils.RotationStepsCW(_baseFacing, _facing);
            Debug.Log("BP ROTATION STEPS ARE : " + _rotationSteps);

            if (_inputSidesMeta.Count > 0)
                inputDirection = _inputSidesMeta[0];
        }

        private void SetOutputItem()
        {
            ItemType outputType = ItemType.None;

            if (blueprint != null && blueprint.hasOutputPort)
                outputType = blueprint.outputPort.itemType;

            CurrentItem = new Item(outputType);

            if (CurrentItem.type != ItemType.None)
            {
                itemVisual.enabled = true;
                itemVisual.sprite = IM.GetItemSprite(CurrentItem.type);
            }
            else
            {
                itemVisual.enabled = false;
            }
        }

        private void BuildRecipeRequirementsFromBlueprint()
        {

            if (blueprint == null || blueprint.elements == null)
                return;

            _requiredAmounts.Clear();
            Dictionary<ItemType, int> consumed = new Dictionary<ItemType, int>();
            Dictionary<ItemType, int> produced = new Dictionary<ItemType, int>();

            foreach (var elem in blueprint.elements)
            {
                var recipe = elem.currentRecipe;
                if (recipe == null)
                    continue;

                if (recipe.inputItems != null)
                {
                    foreach (var ri in recipe.inputItems)
                    {
                        if (ri == null || ri.item == null)
                            continue;

                        var type = ri.item.type;
                        if (type == ItemType.None)
                            continue;

                        if (!consumed.ContainsKey(type))
                            consumed[type] = 0;

                        consumed[type] += Mathf.Max(1, ri.amount);
                    }
                }

                if (recipe.outputItems != null)
                {
                    foreach (var ro in recipe.outputItems)
                    {
                        if (ro == null || ro.item == null)
                            continue;

                        var type = ro.item.type;
                        if (type == ItemType.None)
                            continue;

                        if (!produced.ContainsKey(type))
                            produced[type] = 0;

                        produced[type] += Mathf.Max(1, ro.amount);
                    }
                }
            }

            HashSet<ItemType> allTypes = new HashSet<ItemType>(consumed.Keys);
            allTypes.UnionWith(produced.Keys);

            foreach (var type in allTypes)
            {
                int inAmount = consumed.TryGetValue(type, out var c) ? c : 0;
                int outAmount = produced.TryGetValue(type, out var p) ? p : 0;
                int net = inAmount - outAmount;

                if (net > 0)
                    _requiredAmounts[type] = net;
            }
        }

        private void UpdateArrowPositions()
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
