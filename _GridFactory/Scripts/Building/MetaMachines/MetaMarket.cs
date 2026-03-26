using System.Collections.Generic;
using UnityEngine;

using MoreMountains.Feedbacks;

using GridFactory.Core;
using GridFactory.Directions;
using GridFactory.Grid;

namespace GridFactory.Meta
{
    public class MetaMarket : MetaMachineBase, IEnergyConsumer
    {
        private List<ItemDefinition> itemDefinitions = new List<ItemDefinition>();

        protected override void RotateAdditionalInputs(Direction dir) { }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (requiresEnergy)
                EM.UnregisterConsumer(this);
        }

        private void Start()
        {
            itemDefinitions = IM.AllItems;
        }

        public override void ResetSimulation()
        {
            base.ResetSimulation();

            CurrentItem = new Item(ItemType.None);
            if (requiresEnergy)
                EM.UnregisterConsumer(this);
        }

        protected override bool CanStartProcess()
        {
            if (!hasInput)
                return false;
            if ((CurrentItem != null && CurrentItem.type != ItemType.None) || CanPullFromDirection(inputDirection))
                return true;

            return false;
        }

        protected override void StartProcess()
        {
            if (requiresEnergy)
                EM.RegisterConsumer(this);

            return;
        }

        protected override bool FinishProcess()
        {
            if (CurrentItem.type == ItemType.None)
                TryConsumeFromSingleInput();

            if (CurrentItem.type != ItemType.None)
            {
                foreach (ItemDefinition definition in itemDefinitions)
                {
                    if (definition.type == CurrentItem.type)
                    {
                        IM.AddItem(definition.type);
                        IM.AddGold(definition.value);
                        if (outputEffect)
                        {
                            MMF_FloatingText floatingText = outputEffect.GetFeedbackOfType<MMF_FloatingText>();
                            floatingText.Value = "+" + definition.value;
                            outputEffect.PlayFeedbacks();
                        }

                        if (TutorialGridFactoryController.Instance)
                            TutorialGridFactoryController.Instance.ItemSold(CurrentItem.type);
                    }
                }
                if (requiresEnergy)
                    EM.UnregisterConsumer(this);

                CurrentItem = new Item(ItemType.None);
            }
            else
            {
                return false;
            }

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

                Item item = endpoint.CurrentItem;
                endpoint.CurrentItem = null;

                CurrentItem = item;
                return true;
            }
            return false;
        }
    }
}
