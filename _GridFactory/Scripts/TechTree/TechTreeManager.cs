using System;
using System.Collections.Generic;

using UnityEngine;

using Esper.SkillWeb;
using Esper.SkillWeb.DataManagement;
using Esper.SkillWeb.Graph;
using Esper.SkillWeb.UI.UGUI;

using GridFactory.Core;
using GridFactory.Inventory;
using GridFactory.Meta;

namespace GridFactory.Tech
{
    public class TechTreeManager : MonoBehaviour
    {
        public static TechTreeManager Instance { get; private set; }

        private static InventoryManager IM => InventoryManager.Instance;
        private static UIConfirmationManager UICONFIRMM => UIConfirmationManager.Instance;
        private static UIPanelManager UIPM => UIPanelManager.Instance;

        WebViewUGUI skillWeb;

        [SerializeField] private string graphName = "Forschung";
        [SerializeField] private GameObject techTreeRoot;
        [SerializeField] private GlobalEffectDisplay effectDisplay;

        [SerializeField] GameObject buildMarketButton;
        [SerializeField] GameObject buildBlueprintButton;
        [SerializeField] GameObject buildSmelterButton;
        [SerializeField] GameObject buildSplitterButton;
        [SerializeField] GameObject buildSplitterMetaButton;
        [SerializeField] GameObject buildMergerButton;
        [SerializeField] GameObject buildMergerMetaButton;

        private Dictionary<SkillNode, int> _unlockedSkills = new Dictionary<SkillNode, int>();
        private MetaResearchCenter _selectedCenter;

        public Action<string> _onResearchSelected;
        public Action<string> _onResearchCompleted;

        public List<SkillNode> currentInResearch = new List<SkillNode>();

        public MetaResearchCenter SelectedCenter
        {
            get => _selectedCenter;
            set
            {
                _selectedCenter = value;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            SkillNodeUGUI.onResearch.AddListener(StartResearch);
            SetCustomUpgradeMethod();

            skillWeb = WebViewUGUI.Find("Forschung");
        }

        public SavableWeb GetSaveFile()
        {
            return skillWeb.web.ToSavable();
        }

        public void CloseOnResearchCenterDeletion(MetaResearchCenter mrs)
        {
            if (mrs.CurrentResearch != null && currentInResearch.Contains(mrs.CurrentResearch))
            {
                mrs.CurrentResearch.StopResearch();
                currentInResearch.Remove(mrs.CurrentResearch);
                mrs.CurrentResearch = null;
            }

            if (_selectedCenter == mrs)
                ForceTechTreeUIClose();
        }

        public void LoadTech(SavableWeb saveFile = null)
        {
            LockEverything();

            if (saveFile != null)
            {
                skillWeb.Load(saveFile.ToWeb());
                foreach (var node in skillWeb.web.GetObtainedSkillNodes())
                {
                    var myDataset = node.dataset as TechTreeDataset;
                    ApplyUnlockEffects(myDataset.internalId);
                }
            }
            else
            {
                var webGraph = SkillWeb.GetWebGraph(graphName);
                var web = new Web(webGraph);

                skillWeb.Load(web);
            }
        }

        private void StartResearch(SkillNodeUGUI node)
        {
            var myDataset = node.skillNode.dataset as TechTreeDataset;
            if (myDataset.baseCost == 0 || IM.TrySpendGold(myDataset.baseCost * (node.skillNode.Level + 1)))
            {
                _selectedCenter.SelectResearch(node.skillNode);
                if (!currentInResearch.Contains(node.skillNode))
                    currentInResearch.Add(node.skillNode);

                _onResearchSelected?.Invoke(myDataset.internalId);
            }
        }

        public bool IsNodeInResearch(SkillNode node)
        {
            if (currentInResearch.Contains(node))
                return true;
            return false;
        }

        public void UnlockSkill(SkillNode node)
        {
            node.TryUpgrade();
            if (node.dataset is not TechTreeDataset myDataset)
                return;

            if (_unlockedSkills.ContainsKey(node))
                _unlockedSkills[node] = node.Level;
            else
                _unlockedSkills.Add(node, node.Level);

            ApplyUnlockEffects(myDataset.internalId);

            effectDisplay.ShowBox("Research finished.", "Unlocked: " + myDataset.GetName());
            _onResearchCompleted?.Invoke(myDataset.internalId);

            if (currentInResearch.Contains(node))
                currentInResearch.Remove(node);
        }

        public void SetCustomUpgradeMethod()
        {
            SkillNode.canUpgrade = skillNode =>
            {
                if (IsNodeInResearch(skillNode))
                {
                    UICONFIRMM.Show(
                        "Node already in research.",
                        () => { },
                        () => { }, "", true
                    );
                    return false;
                }
                if (_selectedCenter.CurrentResearch != null)
                {
                    UICONFIRMM.Show(
                        "Center has unfinished research.",
                        () => { },
                        () => { }, "", true
                    );
                    return false;
                }

                var myDataset = skillNode.dataset as TechTreeDataset;
                if (IM.Gold >= myDataset.baseCost * (skillNode.Level + 1))
                    return true;

                return false;
            };
        }

        public void ForceTechTreeUIClose()
        {
            UIPM.ToggleTechTreeUI(null, true);
        }

        private void LockEverything()
        {
            if (buildMarketButton)
                buildMarketButton.SetActive(false);
            if (buildBlueprintButton)
                buildBlueprintButton.SetActive(false);
            if (buildSmelterButton)
                buildSmelterButton.SetActive(false);
            if (buildSplitterButton)
                buildSplitterButton.SetActive(false);
            if (buildSplitterMetaButton)
                buildSplitterMetaButton.SetActive(false);
            if (buildMergerButton)
                buildMergerButton.SetActive(false);
            if (buildMergerMetaButton)
                buildMergerMetaButton.SetActive(false);
        }

        private void ApplyUnlockEffects(string internalId)
        {
            switch (internalId)
            {
                case "market":
                    buildMarketButton.SetActive(true);
                    break;
                case "blueprint":
                    buildBlueprintButton.SetActive(true);
                    break;
                case "smelter":
                    buildSmelterButton.SetActive(true);
                    break;
                case "splitter":
                    buildSplitterButton.SetActive(true);
                    buildSplitterMetaButton.SetActive(true);
                    break;
                case "merger":
                    buildMergerButton.SetActive(true);
                    buildMergerMetaButton.SetActive(true);
                    break;
                case "conveyor_speed":
                    Debug.Log("CONVEYOR SPEED RESEARCHED");
                    break;
            }
        }
    }
}