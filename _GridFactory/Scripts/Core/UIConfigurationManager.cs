using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using GridFactory.Blueprints;
using GridFactory.Inventory;
using GridFactory.Machines;
using GridFactory.Meta;

namespace GridFactory.Core
{
    public class UIConfigurationManager : MonoBehaviour
    {
        public static UIConfigurationManager Instance { get; private set; }
        [SerializeField] private ConfigurationUI configUi;

        private static GameManager GM => GameManager.Instance;
        private static InventoryManager IM => InventoryManager.Instance;
        private static BlueprintManager BM => BlueprintManager.Instance;
        private static MetaBuildController MBC => MetaBuildController.Instance;

        private List<ItemDefinition> _availableItems;
        private PortMarker _selectedPort;
        private MetaResourceNode _selectedNode;
        private MachineWithRecipeBase _selectedMachine;
        private RecipeDefinition _selectedRecipe = null;
        private MetaBlueprintModule _selectedBlueprintModule;
        private BlueprintDefinition _selectedBlueprintDefinition;
        private ItemType _selectedItem = ItemType.None;

        public Action<ItemType> _onConfigurationChanged;
        public Action<RecipeDefinition> _onRecipeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            _availableItems = IM.AllItems;
        }

        public void SetMachineWithRecipeBase(MachineWithRecipeBase mwrb = null)
        {
            RemoveAll();

            _selectedMachine = mwrb;
            _selectedRecipe = _selectedMachine.CurrentRecipe;

            List<RecipeDefinition> availableRecipes = mwrb.AllRecipes;
            if (availableRecipes.Count > 0 && _selectedRecipe == null)
                _selectedRecipe = availableRecipes[0];

            int index = availableRecipes.IndexOf(mwrb.CurrentRecipe);
            if (index < 0) index = 0;

            configUi.SetupRecipeDropdown(_selectedMachine.AllRecipes, index, false, () =>
                {
                    int selectedIndex = configUi.LastSelectedIndex;
                    if (_selectedMachine == null || selectedIndex < 0 || selectedIndex >= availableRecipes.Count)
                        return;

                    _selectedRecipe = availableRecipes[selectedIndex];
                    configUi.UpdateInfoText(GetIngredientsForRecipe(_selectedRecipe));
                });
            configUi.OpenConfiguration("Select Recipe", () =>
                {
                    if (_selectedMachine && _selectedRecipe)
                    {
                        _selectedMachine.CurrentRecipe = _selectedRecipe;
                        _onRecipeChanged?.Invoke(_selectedRecipe);
                    }

                    GM.ResetSimulation();
                    Close();
                }, () =>
                {
                    Close();
                });

            if (_selectedRecipe != null)
                configUi.UpdateInfoText(GetIngredientsForRecipe(_selectedRecipe));
        }

        public void SetBlueprintModule(MetaBlueprintModule mbpm)
        {
            RemoveAll();

            _selectedBlueprintModule = mbpm;
            _selectedBlueprintDefinition = _selectedBlueprintModule.blueprint;

            List<BlueprintDefinition> availableBlueprints = BM.runtimeBlueprints;

            int index = availableBlueprints.IndexOf(_selectedBlueprintDefinition);
            if (index < 0) index = 0;

            configUi.SetupBlueprintDropdown(availableBlueprints, index, false,
                () =>
                {
                    int selectedIndex = configUi.LastSelectedIndex;
                    if (_selectedBlueprintModule == null || selectedIndex < 0 || selectedIndex >= availableBlueprints.Count)
                        return;

                    _selectedBlueprintDefinition = availableBlueprints[selectedIndex];
                    configUi.UpdateInfoText(GetIngredientsForBlueprint(_selectedBlueprintDefinition));
                });

            configUi.OpenConfiguration("Select Blueprint", () =>
                {
                    if (_selectedBlueprintModule && _selectedBlueprintDefinition)
                        _selectedBlueprintModule.UpdateBlueprint(_selectedBlueprintDefinition);

                    GM.ResetSimulation();
                    Close();

                }, () =>
                {
                    Close();
                });

            if (_selectedBlueprintDefinition != null)
                configUi.UpdateInfoText(GetIngredientsForBlueprint(_selectedBlueprintDefinition));
        }

        public void SetBlueprintBuild()
        {
            RemoveAll();

            List<BlueprintDefinition> availableBlueprints = BM.runtimeBlueprints;

            if (availableBlueprints.Count > 0)
                _selectedBlueprintDefinition = availableBlueprints[0];

            configUi.SetupBlueprintDropdown(availableBlueprints, 0, false,
                () =>
                {
                    int selectedIndex = configUi.LastSelectedIndex;
                    if (selectedIndex < 0 || selectedIndex >= availableBlueprints.Count)
                        return;

                    _selectedBlueprintDefinition = availableBlueprints[selectedIndex];
                    configUi.UpdateInfoText(GetIngredientsForBlueprint(_selectedBlueprintDefinition));
                });

            configUi.OpenConfiguration("Select Blueprint", () =>
                {
                    if (_selectedBlueprintDefinition)
                        MBC.SetSelectedBlueprint(_selectedBlueprintDefinition);

                    Close();
                }, () =>
                {
                    Close();
                });
        }

        public void SetPortmarker(PortMarker port = null)
        {
            RemoveAll();

            _selectedPort = port;
            ItemDefinition selectedItemType = IM.GetDefinition(port.portItemType);

            int index = _availableItems.IndexOf(selectedItemType);
            if (index < 0) index = 0;

            configUi.SetupItemDropdown(_availableItems, index, false,
                () =>
                {
                    int selectedIndex = configUi.LastSelectedIndex;
                    if (_selectedPort == null || selectedIndex < 0 || selectedIndex >= _availableItems.Count)
                        return;

                    _selectedItem = _availableItems[selectedIndex].type;
                });
            configUi.OpenConfiguration("Select Item", () =>
                {
                    if (_selectedPort && _selectedItem != ItemType.None)
                        _selectedPort.portItemType = _selectedItem;

                    GM.ResetSimulation();
                    Close();
                }, () =>
                {
                    Close();
                });
        }

        public void SetResourceNode(MetaResourceNode node = null)
        {
            RemoveAll();

            List<ItemDefinition> allowedItems = new List<ItemDefinition>();

            _selectedNode = node;
            IReadOnlyList<ItemType> allowedByNode = _selectedNode.AllowedOutputItems;
            ItemDefinition selectedItemType = IM.GetDefinition(node.ResourceItem);

            foreach (ItemDefinition def in _availableItems)
            {
                if (allowedByNode.Contains(def.type))
                    allowedItems.Add(def);
            }

            int index = allowedItems.IndexOf(selectedItemType);
            if (index < 0) index = 0;

            configUi.SetupItemDropdown(allowedItems, index, false,
                () =>
                {
                    int selectedIndex = configUi.LastSelectedIndex;
                    if (_selectedNode == null || selectedIndex < 0 || selectedIndex >= allowedItems.Count)
                        return;

                    _selectedItem = allowedItems[selectedIndex].type;
                });

            configUi.OpenConfiguration("Select Resource", () =>
                {
                    if (_selectedNode && _selectedItem != ItemType.None)
                    {
                        _selectedNode.ResourceItem = _selectedItem;
                        _onConfigurationChanged?.Invoke(_selectedItem);
                    }

                    GM.ResetSimulation();
                    Close();
                }, () =>
                {
                    Close();
                });
        }

        public void ForceDropdownSelection(int val)
        {
            configUi.SetDropdownValueProgrammatically(val);
        }

        public void Close()
        {
            RemoveAll();
            configUi.Close();
        }

        private void RemoveAll()
        {
            _selectedMachine = null;
            _selectedBlueprintModule = null;
            _selectedRecipe = null;
            _selectedBlueprintDefinition = null;
            _selectedPort = null;
            _selectedNode = null;
            _selectedItem = ItemType.None;
        }

        private string GetIngredientsForRecipe(RecipeDefinition recipe)
        {
            string myString = "Needed Items:\n";

            foreach (var entry in recipe.inputItems)
            {
                if (entry == null || entry.item == null || entry.amount <= 0)
                    continue;

                string itemName = entry.item.type.ToString();
                myString += $"{entry.amount}x {itemName} | ";
            }
            return myString;
        }

        private string GetIngredientsForBlueprint(BlueprintDefinition bp)
        {
            string myString = "Needed Items:\n";

            foreach (var entry in bp.inputPorts)
            {
                ItemDefinition item = IM.GetDefinition(entry.itemType);
                myString += $"{item.displayName} |";
            }

            return myString;
        }
    }
}