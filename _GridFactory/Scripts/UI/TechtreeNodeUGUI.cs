
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;

using static Esper.SkillWeb.Skill;
using Esper.SkillWeb.UI.UGUI;

using GridFactory.Core;
using GridFactory.Inventory;

namespace GridFactory.Tech
{
    public class TechTreeNodeUGUI : SkillNodeUGUI
    {
        private static GameManager GM => GameManager.Instance;
        private static TechTreeManager TTM => TechTreeManager.Instance;
        private static UIConfirmationManager UICONFIRMM => UIConfirmationManager.Instance;
        private static HovercardUGUI HCUI => HovercardUGUI.Instance;
        private static WebViewSelectorUGUI WVSUI => WebViewSelectorUGUI.Instance;

        private static readonly List<TechTreeNodeUGUI> allNodeUis = new List<TechTreeNodeUGUI>();
        [SerializeField] private Color inresearchColor;
        [SerializeField] private Color obtainedColor;

        public static void RefreshAll()
        {
            if (allNodeUis.Count == 0)
                return;

            foreach (TechTreeNodeUGUI node in allNodeUis)
                node.Refresh();
        }

        void OnEnable()
        {
            if (!allNodeUis.Contains(this))
                allNodeUis.Add(this);
        }

        private void OnDisable()
        {
            allNodeUis.Remove(this);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == pointerUpgradeButton)
            {
                if (GameManager.Instance.CurrentControlScheme != "desktop")
                    HCUI.Open(this);

                if (skillNode.CanResearch())
                {
                    UICONFIRMM.Show(
                    "Start research?",
                    () =>
                    {
                        Research();
                        TTM.ForceTechTreeUIClose();
                    });
                }
            }

            if (HCUI.IsOpen && GM.CurrentControlScheme == "desktop")
                HCUI.Close();
        }

        public override void Refresh()
        {
            if (skillNode != null)
            {
                SkillIcon icon = skillNode.GetIcon(true);
                Color iconColor = icon.color;
                var myDataset = skillNode.dataset as TechTreeDataset;

                if (myDataset.baseCost > 0)
                {
                    if (InventoryManager.Instance.Gold < myDataset.baseCost)
                    {
                        icon = skillNode.GetIconByStateString("locked");
                        iconColor = icon.color;
                    }
                }

                iconImage.sprite = icon.icon;
                iconImage.color = iconColor;
                iconImage.enabled = icon.icon;

                if (levelText)
                    levelText.text = $"{skillNode.Level}/{skillNode.MaxLevel}";
            }
            else
            {
                iconImage.sprite = null;
                iconImage.color = Color.white;
                iconImage.enabled = false;

                if (levelText)
                    levelText.text = "0/0";
            }

            if (skillBackground)
            {
                if (skillNode.IsInResearch)
                    skillBackground.color = inresearchColor;

                if (skillNode.IsObtained)
                    skillBackground.color = obtainedColor;

            }
        }

        public override bool TryUpgrade()
        {
            bool result = skillNode.TryUpgrade();
            if (result)
            {
                onUpgrade.Invoke(this);

                if (HCUI.Target == this)
                    HCUI.Refresh();

                if (hasPointerHover || WVSUI.focusedNode == this)
                    UnhighlightPaths();

                if (skillBackground)
                    skillBackground.color = obtainedColor;

            }

            return result;
        }

        public override bool Research()
        {
            skillNode.Research();
            onResearch.Invoke(this);

            if (HCUI.Target == this)
                HCUI.Refresh();

            if (hasPointerHover || WVSUI.focusedNode == this)
                UnhighlightPaths();

            if (skillBackground)
                skillBackground.color = inresearchColor;

            return true;
        }
    }

}
