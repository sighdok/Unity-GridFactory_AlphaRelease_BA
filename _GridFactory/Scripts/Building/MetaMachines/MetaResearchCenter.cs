using System.Collections.Generic;

using UnityEngine;

using Esper.SkillWeb.Graph;

using GridFactory.Core;
using GridFactory.Grid;
using GridFactory.Directions;
using GridFactory.Tech;

namespace GridFactory.Meta
{
    public class MetaResearchCenter : MetaMachineBase, IEnergyConsumer
    {
        protected static TechTreeManager TTM => TechTreeManager.Instance;

        [SerializeField] private SpriteRenderer researchVisual;

        private List<ResearchItemCounter> _completedItemArrival = new List<ResearchItemCounter>();
        private SkillNode _currentResearch;
        private TechTreeDataset _researchDataset;
        private bool _processingResearch = false;
        private bool _hasPendingOutput = false;

        public bool ProcessingResearch
        {
            get => _processingResearch;
            set => _processingResearch = value;
        }

        public SkillNode CurrentResearch
        {
            get => _currentResearch;
            set
            {
                _currentResearch = value;
                if (value != null)
                {
                    _researchDataset = _currentResearch.dataset as TechTreeDataset;
                    researchVisual.sprite = _currentResearch.GetIconByStateString("unlocked").icon;
                }
                else
                {
                    researchVisual.sprite = null;
                    _researchDataset = null;
                }
                ResetCompletedItemArrival();
            }
        }

        protected override void RotateAdditionalInputs(Direction dir) { }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetCompletedItemArrival();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (requiresEnergy)
                EM.UnregisterConsumer(this);
        }

        protected override bool CanStartProcess()
        {
            if (_currentResearch != null)
                return true;
            else
                return false;
        }

        public void SelectResearch(SkillNode node)
        {
            CurrentResearch = node;
            baseTicksPerProcess = _researchDataset.ticksForProcess;
        }

        public override void ResetSimulation()
        {
            base.ResetSimulation();
            ResetCompletedItemArrival();
            _hasPendingOutput = false;
            _processingResearch = false;
            baseTicksPerProcess = 0;
            if (requiresEnergy)
                EM.UnregisterConsumer(this);
        }

        protected void ResetCompletedItemArrival()
        {
            _completedItemArrival.Clear();
            if (_currentResearch == null)
                return;

            foreach (ResearchItem inputItem in _researchDataset.inputItems)
                _completedItemArrival.Add(new ResearchItemCounter(inputItem.item, inputItem.amount));
        }

        protected bool AllowIncomingItem(Item incomingItem)
        {
            if (_currentResearch == null || incomingItem == null)
                return false;

            foreach (ResearchItem possibleItem in _researchDataset.inputItems)

                if (possibleItem.item.type == incomingItem.type)
                    return true;

            return false;
        }

        protected bool TryToStoreIncomingItem(Item item)
        {
            foreach (ResearchItemCounter counter in _completedItemArrival)
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

        public bool CanProcessResearch()
        {
            if (_currentResearch == null)
                return false;

            foreach (ResearchItemCounter counter in _completedItemArrival)
                if (!counter.completed)
                    return false;

            if (_completedItemArrival.Count == 0)
                return true;

            return true;
        }

        protected override void StartProcess()
        {
            _hasPendingOutput = true;
            if (requiresEnergy)
                EM.RegisterConsumer(this);
        }

        protected override bool FinishProcess()
        {
            if (!_hasPendingOutput)
                return false;

            TTM.UnlockSkill(_currentResearch);
            _hasPendingOutput = false;
            _currentResearch.StopResearch();
            _currentResearch = null;
            researchVisual.sprite = null;

            if (requiresEnergy)
                EM.UnregisterConsumer(this);

            return true;
        }

        public void TryPullIntoProcessing()
        {
            if (_currentResearch == null)
                return;

            MetaCell inCell = GetAdjacentCell(inputDirection);
            if (inCell == null)
                return;

            if (inCell.Machine is MetaCrossing crossing)
            {
                if (crossing.HasItemOnLane(inputDirection))
                {
                    crossing.TryPullItemFromLane(inputDirection, out var crossIncomingItem);
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

                if (endpoint == null || endpoint.CurrentItem == null || !IsAllowedEndpointForMachine(endpoint))
                    return;

                Vector2Int expectedDownstream =
                    inCell.Position + DirectionUtils.DirectionToOffset(endpoint.OutputDirection);

                if (expectedDownstream != _cell.Position)
                    return;

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
}
