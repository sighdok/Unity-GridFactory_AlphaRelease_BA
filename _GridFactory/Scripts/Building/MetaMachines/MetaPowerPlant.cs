using System.Collections.Generic;

using UnityEngine;

using GridFactory.Core;
using GridFactory.Directions;
using GridFactory.Grid;

using MoreMountains.Feedbacks;

namespace GridFactory.Meta
{
    public class MetaPowerPlant : MetaMachineBase
    {
        private Dictionary<ItemType, ItemDefinition> defByType;
        private float activeFuelEnergyTotal;
        private bool hasActiveFuel;

        protected override void RotateAdditionalInputs(Direction dir) { }

        private void Start()
        {
            var all = IM.AllItems;

            defByType = new Dictionary<ItemType, ItemDefinition>(all.Count);
            foreach (var d in all) defByType[d.type] = d;
        }

        public override void ResetSimulation()
        {
            base.ResetSimulation();
            CurrentItem = new Item(ItemType.None);
            activeFuelEnergyTotal = 0f;
            hasActiveFuel = false;
        }

        protected override void OnProcessingTick(float deltaProgress01)
        {
            if (!hasActiveFuel || activeFuelEnergyTotal <= 0f)
                return;

            float produced = activeFuelEnergyTotal * deltaProgress01;
            EM.AddEnergyFloat(produced);
        }

        protected override bool CanStartProcess()
        {
            if (!hasInput || !CanPullFromDirection(inputDirection))
                return false;

            TryConsumeFromSingleInput();

            if (CurrentItem == null || CurrentItem.type == ItemType.None)
                return false;

            if (defByType.TryGetValue(CurrentItem.type, out var def))
            {
                if (def.producesEnergy == false)
                {
                    CurrentItem = new Item(ItemType.None);
                    if (burnItemEffect)
                        burnItemEffect.PlayFeedbacks();

                    return false;
                }
            }

            return true;
        }

        protected override void StartProcess()
        {
            defByType.TryGetValue(CurrentItem.type, out var def);
            activeFuelEnergyTotal = def.energyAmount; // Brennwert
            hasActiveFuel = true;

            if (TutorialGridFactoryController.Instance)
                TutorialGridFactoryController.Instance.FuelBurned(CurrentItem.type);
        }

        protected override bool FinishProcess()
        {
            if (!hasActiveFuel)
                return false;

            if (outputEffect)
            {
                var floatingText = outputEffect.GetFeedbackOfType<MMF_FloatingText>();
                if (floatingText != null)
                    floatingText.Value = "+" + activeFuelEnergyTotal;
                outputEffect.PlayFeedbacks();
            }

            CurrentItem = new Item(ItemType.None);
            activeFuelEnergyTotal = 0f;
            hasActiveFuel = false;
            return true;
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

        private bool TryConsumeFromSingleInput()
        {
            MetaCell inCell = GetAdjacentCell(inputDirection);
            if (inCell == null)
                return false;

            if (inCell.Machine is MetaCrossing crossing)
            {
                if (crossing.TryPullItemFromLane(inputDirection, out var crossIncomingItem))
                {
                    if (crossIncomingItem != null)
                    {
                        Item item = crossIncomingItem;
                        CurrentItem = item;
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

                if (endpoint.CurrentItem == null)
                    return false;

                Vector2Int expectedDownstream =
                    inCell.Position + DirectionUtils.DirectionToOffset(endpoint.OutputDirection);

                if (expectedDownstream != _cell.Position)
                    return false;

                CurrentItem = endpoint.CurrentItem;
                endpoint.CurrentItem = null;


                return true;
            }
            return false;
        }
    }
}
