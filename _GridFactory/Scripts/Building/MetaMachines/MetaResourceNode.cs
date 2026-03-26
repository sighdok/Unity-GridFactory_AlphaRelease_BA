using System;
using System.Collections.Generic;

using UnityEngine;

using MoreMountains.Feedbacks;

using GridFactory.Core;
using GridFactory.Directions;

namespace GridFactory.Meta
{
    public class MetaResourceNode : MetaMachineBase, IEnergyConsumer
    {
        [Header("Allowed outputs (whitelist)")]
        [SerializeField] private List<ItemType> allowedOutputItems = new List<ItemType>();
        [SerializeField] private ItemType resourceItemType = ItemType.Stone;

        public IReadOnlyList<ItemType> AllowedOutputItems
        {
            get
            {
                if (allowedOutputItems == null || allowedOutputItems.Count == 0)
                    return (ItemType[])Enum.GetValues(typeof(ItemType));
                return allowedOutputItems;
            }
        }

        public ItemType ResourceItem
        {
            get => resourceItemType;
            set => resourceItemType = value;
        }


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

        public override void ResetSimulation()
        {
            base.ResetSimulation();
            CurrentItem = new Item(ItemType.None);
            if (requiresEnergy)
                EM.UnregisterConsumer(this);
        }

        protected override void StartProcess()
        {
            if (requiresEnergy)
                EM.RegisterConsumer(this);
            return;
        }

        protected override bool CanStartProcess()
        {
            return true;
        }

        protected override bool FinishProcess()
        {
            var item = new Item(resourceItemType);
            if (CanOutput(outputDirection))
            {
                if (OutputToNeighbor(outputDirection, item))
                {
                    if (outputEffect)
                    {
                        var floatingText = outputEffect.GetFeedbackOfType<MMF_FloatingSprite>();
                        if (floatingText != null)
                        {
                            floatingText.Value = IM.GetItemSprite(resourceItemType);
                        }

                        outputEffect.PlayFeedbacks();
                    }

                    if (requiresEnergy)
                        EM.UnregisterConsumer(this);
                    return true;
                }

                return false;
            }
            return false;
        }
    }
}