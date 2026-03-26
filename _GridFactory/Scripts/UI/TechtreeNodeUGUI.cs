
using Esper.SkillWeb.UI.UGUI;
using GridFactory.Core;
using UnityEngine.EventSystems;

using UnityEngine;
using Esper.SkillWeb;
using GridFactory.Inventory;
using static Esper.SkillWeb.Skill;
using Unity.VisualScripting;
using System.Collections.Generic;
namespace GridFactory.Tech
{
    public class TechTreeNodeUGUI : SkillNodeUGUI
    {
        private static readonly List<TechTreeNodeUGUI> allNodeUis = new List<TechTreeNodeUGUI>();
        public static void RefreshAll()
        {
            if (allNodeUis.Count == 0)
                return;

            // PHASE A: Moves planen
            foreach (TechTreeNodeUGUI node in allNodeUis)
            {
                node.Refresh();
            }
        }

        public Color inresearchColor;
        public Color obtainedColor;
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

                if (HovercardUGUI.Instance && GameManager.Instance.CurrentControlScheme != "desktop")
                {
                    HovercardUGUI.Instance.Open(this);
                }

                if (skillNode.CanResearch())
                {
                    UIConfirmationManager.Instance.Show(
                    "Start research?",
                    () =>
                    {

                        Research();

                        TechTreeManager.Instance.ForceTechTreeUIClose();
                    });
                }
            }
            if (HovercardUGUI.Instance && HovercardUGUI.Instance.IsOpen && GameManager.Instance.CurrentControlScheme == "desktop")
            {
                HovercardUGUI.Instance.Close();
            }
        }

        public override void Refresh()
        {
            /*
                float size = 100;

                switch (skillNode.skill.size)
                {
                    case Skill.Size.Tiny:
                        size = SkillWeb.Settings.skillNodeSizes.tiny;
                        break;

                    case Skill.Size.Small:
                        size = SkillWeb.Settings.skillNodeSizes.small;
                        break;

                    case Skill.Size.Medium:
                        size = SkillWeb.Settings.skillNodeSizes.medium;
                        break;

                    case Skill.Size.Large:
                        size = SkillWeb.Settings.skillNodeSizes.large;
                        break;

                    case Skill.Size.Giant:
                        size = SkillWeb.Settings.skillNodeSizes.giant;
                        break;
                }

                rectTransform.sizeDelta = new Vector2(size, size);
    */


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
                /*
                if (TechTreeManager.Instance.IsNodeInResearch(skillNode))
                {

                    icon = skillNode.GetIconByStateString("locked");
                    iconColor = icon.color;
                }
*/

                iconImage.sprite = icon.icon;
                iconImage.color = iconColor;
                iconImage.enabled = icon.icon;

                if (levelText)
                {
                    levelText.text = $"{skillNode.Level}/{skillNode.MaxLevel}";
                }
            }
            else
            {

                iconImage.sprite = null;
                iconImage.color = Color.white;
                iconImage.enabled = false;

                if (levelText)
                {
                    levelText.text = "0/0";
                }
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

                if (HovercardUGUI.Instance && HovercardUGUI.Instance.Target == this)
                {
                    HovercardUGUI.Instance.Refresh();
                }

                if (hasPointerHover || WebViewSelectorUGUI.Instance?.focusedNode == this)
                {
                    UnhighlightPaths();
                }

                if (skillBackground)
                    skillBackground.color = obtainedColor;

            }

            return result;
        }

        public override bool Research()
        {

            skillNode.Research();

            onResearch.Invoke(this);

            if (HovercardUGUI.Instance && HovercardUGUI.Instance.Target == this)
            {
                HovercardUGUI.Instance.Refresh();
            }

            if (hasPointerHover || WebViewSelectorUGUI.Instance?.focusedNode == this)
            {
                UnhighlightPaths();
            }

            if (skillBackground)
                skillBackground.color = inresearchColor;



            return true;
        }
    }

}
