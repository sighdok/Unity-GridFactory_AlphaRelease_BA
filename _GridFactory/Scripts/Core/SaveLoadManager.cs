using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using TigerForge;
using Esper.SkillWeb.DataManagement;

using GridFactory.Inventory;
using GridFactory.Blueprints;
using GridFactory.Grid;
using GridFactory.Tech;

namespace GridFactory.Core
{
    public static class SaveLoadContext
    {
        public static bool IsLoading { get; private set; }
        public static void BeginLoad() => IsLoading = true;
        public static void EndLoad() => IsLoading = false;
    }

    public class SaveLoadManager : MonoBehaviour
    {
        public static SaveLoadManager Instance { get; private set; }

        private static GridShopManager GSM => GridShopManager.Instance;
        private static GridDefinitionManager GDM => GridDefinitionManager.Instance;
        private static BlueprintManager BPM => BlueprintManager.Instance;
        private static MetaGridManager MGM => MetaGridManager.Instance;
        private static InventoryManager IM => InventoryManager.Instance;
        private static EnergyManager EM => EnergyManager.Instance;
        private static TechTreeManager TTM => TechTreeManager.Instance;
        private static UIConfirmationManager UICONFIRMM => UIConfirmationManager.Instance;
        private static GoldRateTrackingManager GRTM => GoldRateTrackingManager.Instance;

        [Header("Grid Shop")]
        [SerializeField] private string gridFileName = "griddata";
        [SerializeField] private string gridLibraryKey = "GridLibrary";
        [SerializeField] private string gridShopKey = "GridShopState";

        [Header("Blueprints")]
        [SerializeField] private string blueprintFileName = "blueprints";
        [SerializeField] private string blueprintKey = "BlueprintLibrary";

        [Header("TechTree")]
        [SerializeField] private string techTreeFilename = "techtree";
        [SerializeField] private string techtreeKey = "TechTree";

        [Header("Inventory")]
        [SerializeField] private string inventoryFileName = "gamedata";
        [SerializeField] private string inventoryKey = "InventoryData";

        [Header("Meta")]
        [SerializeField] private string metaFileName = "metadata";
        [SerializeField] private string metaKey = "MetaData";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void ForceLoadGame()
        {
            LoadGame();
        }

        public void ForceSaveGame()
        {
            SaveGame();
        }

        public void TryToSaveGame()
        {
            UICONFIRMM.Show(
                "Save game?",
                () =>
                    SaveGame(),
                () => { }
            );
        }

        public void TryToLoadGame()
        {
            UICONFIRMM.Show(
                 "Load game?",
                 () =>
                 LoadGame(),
                 () => { }
             );
        }

        public void TryToDeleteSaveFile()
        {
            UICONFIRMM.Show(
                "CAUTION: Delete ALL?",
                () => DeleteSave(),
                () => { }
            );
        }

        private void DeleteSave()
        {
            var myFile = new EasyFileSave(metaFileName);
            myFile.Delete();
            myFile = new EasyFileSave(gridFileName);
            myFile.Delete();
            myFile = new EasyFileSave(blueprintFileName);
            myFile.Delete();
            myFile = new EasyFileSave(inventoryFileName);
            myFile.Delete();
            myFile = new EasyFileSave(techTreeFilename);
            myFile.Delete();
            EasyFileSave.DeleteAll();

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void SaveGame()
        {
            EasyFileSave efsTT = new EasyFileSave(techTreeFilename);
            EasyFileSave efsInv = new EasyFileSave(inventoryFileName);
            EasyFileSave efsMeta = new EasyFileSave(metaFileName);
            EasyFileSave efsGrid = new EasyFileSave(gridFileName);
            EasyFileSave efsBp = new EasyFileSave(blueprintFileName);

            var savableWeb = TTM.GetSaveFile();
            efsTT.AddSerialized(techtreeKey, savableWeb);

            InventorySaveData invData = BuildInventorySaveData();
            efsInv.AddSerialized(inventoryKey, invData);

            MetaGridStructureSaveData metaData = MGM.ToStructureSaveData();
            efsMeta.AddSerialized(metaKey, metaData);

            var gridLib = GDM.ToSaveData();
            var shopState = GSM.ToSaveData();
            efsGrid.AddSerialized(gridLibraryKey, gridLib);
            efsGrid.AddSerialized(gridShopKey, shopState);

            BlueprintLibrarySaveData lib = new BlueprintLibrarySaveData();
            foreach (var bp in BPM.runtimeBlueprints)
            {
                if (bp == null)
                    continue;
                lib.blueprints.Add(BlueprintSerialization.ToSaveData(bp));
            }
            efsBp.AddBinary(blueprintKey, lib);

            efsTT.Save();
            efsInv.Save();
            efsMeta.Save();
            efsGrid.Save();
            efsBp.Save();
        }

        private void LoadGame()
        {
            EM.ClearConsumersOnLoad();

            EasyFileSave efsBp = new EasyFileSave(blueprintFileName);
            EasyFileSave efsInv = new EasyFileSave(inventoryFileName);
            EasyFileSave efsMeta = new EasyFileSave(metaFileName);
            EasyFileSave efsTT = new EasyFileSave(techTreeFilename);
            EasyFileSave efsGrid = new EasyFileSave(gridFileName);

            if (efsInv.Load())
            {
                object obj = efsInv.GetDeserialized(inventoryKey, typeof(InventorySaveData));
                if (obj == null)
                {
                    //Debug.LogWarning("[SaveLoadManager] Keine Inventar-Daten im Save gefunden.");
                    return;
                }
                else
                {
                    InventorySaveData invData = (InventorySaveData)obj;
                    ApplyInventorySaveData(invData);
                }
            }

            if (efsBp.Load())
            {
                object obj = efsBp.GetBinary(blueprintKey);
                if (obj == null)
                {
                    //Debug.LogWarning("[SaveLoadManager] Keine Blueprint-Daten im Save gefunden.");
                    return;
                }
                else
                {
                    BPM.runtimeBlueprints.Clear();
                    var lib = obj as BlueprintLibrarySaveData;
                    foreach (var bpData in lib.blueprints)
                    {
                        var def = BlueprintSerialization.FromSaveData(bpData);
                        BPM.runtimeBlueprints.Add(def);
                    }
                }
            }

            if (efsMeta.Load())
            {
                object obj = efsMeta.GetDeserialized(metaKey, typeof(MetaGridStructureSaveData));
                if (obj == null)
                {
                    //Debug.LogWarning("[SaveLoadManager] Keine Meta-Daten im Save gefunden.");
                    return;
                }
                else
                {
                    MetaGridStructureSaveData metaData = (MetaGridStructureSaveData)obj;
                    MGM.ApplyStructureSaveData2Pass(metaData);
                }
            }

            if (efsGrid.Load())
            {
                object objLib = efsGrid.GetDeserialized(gridLibraryKey, typeof(GridLibrarySaveData));
                if (objLib is GridLibrarySaveData libT)
                    GDM.ApplySaveData(libT);
                else
                    GDM.InitDefaults();

                object objShop = efsGrid.GetDeserialized(gridShopKey, typeof(GridShopStateSaveData));
                if (objShop is GridShopStateSaveData shop)
                    GSM.ApplySaveData(shop);
                else
                    GSM.GenerateInitialOffers();
            }
            else
            {
                GDM.InitDefaults();
                GSM.GenerateInitialOffers();
            }

            SavableWeb skillData = null;
            if (efsTT.Load())
            {
                object techObj = efsTT.GetDeserialized(techtreeKey, typeof(SavableWeb));
                skillData = techObj as SavableWeb;
            }
            TTM.LoadTech(skillData);

            efsInv.Dispose();
            efsBp.Dispose();
            efsMeta.Dispose();
            efsTT.Dispose();
            efsGrid.Dispose();
        }

        private InventorySaveData BuildInventorySaveData()
        {
            var data = new InventorySaveData();
            data.gold = IM.Gold;
            data.energyFloat = EM.EnergyFloat;
            data.goldPerSecond = GRTM.GoldPerSecond;
            data.goldRateSumInWindow = GRTM.SumInWindow;

            Dictionary<ItemType, int> counts = IM.GetAllItemCountsCopy();
            foreach (var kvp in counts)
            {
                if (kvp.Key == ItemType.None)
                    continue;

                data.items.Add(new InventoryItemStackData
                {
                    type = kvp.Key,
                    count = kvp.Value
                });
            }

            return data;
        }

        private void ApplyInventorySaveData(InventorySaveData data)
        {
            IM.ClearInventory();
            IM.SetGoldDirect(data.gold);
            EM.LoadEnergyData(data.energyFloat);
            GRTM.SetGoldPerSecondDirect(data.goldPerSecond);
            GRTM.SumInWindow = data.goldRateSumInWindow;

            var newCounts = new Dictionary<ItemType, int>();
            foreach (var stack in data.items)
            {
                if (stack.type == ItemType.None)
                    continue;
                if (!newCounts.ContainsKey(stack.type))
                    newCounts[stack.type] = 0;

                newCounts[stack.type] += Mathf.Max(0, stack.count);
            }

            IM.SetAllItemCounts(newCounts);
        }

        public static class BlueprintSerialization
        {
            public static BlueprintSaveData ToSaveData(BlueprintDefinition def)
            {
                var data = new BlueprintSaveData
                {
                    id = def.id,
                    displayName = def.displayName,
                    baseGridId = def.baseGridId,
                    machineCount = def.machineCount,
                    beltCount = def.beltCount,
                    blueprintCells = def.blueprintCells,
                    sizeX = def.size.x,
                    sizeY = def.size.y,
                    ticksPerProcess = def.ticksPerProcess,
                    hasOutputPort = def.hasOutputPort
                };

                if (def.inputPorts != null)
                {
                    foreach (var port in def.inputPorts)
                    {
                        var p = new BlueprintPortSaveData
                        {
                            x = port.localPos.x,
                            y = port.localPos.y,
                            facing = port.facing,
                            itemType = port.itemType
                        };
                        data.inputPorts.Add(p);
                    }
                }

                if (def.hasOutputPort)
                {
                    data.outputPort = new BlueprintPortSaveData
                    {
                        x = def.outputPort.localPos.x,
                        y = def.outputPort.localPos.y,
                        facing = def.outputPort.facing,
                        itemType = def.outputPort.itemType
                    };
                }

                foreach (var elem in def.elements)
                {
                    var e = new BlueprintElementSaveData
                    {
                        elementType = elem.elementType,
                        machineKind = elem.machineKind,
                        portKind = elem.portKind,
                        recipeId = elem.currentRecipe != null ? elem.currentRecipe.id : null,
                        x = elem.localPos.x,
                        y = elem.localPos.y,
                        inputDirection = elem.inputDirection,
                        outputDirection = elem.outputDirection
                    };
                    data.elements.Add(e);
                }

                return data;
            }

            public static BlueprintDefinition FromSaveData(BlueprintSaveData data)
            {
                var def = ScriptableObject.CreateInstance<BlueprintDefinition>();

                def.size = new Vector2Int(data.sizeX, data.sizeY);
                def.id = data.id;
                def.displayName = data.displayName;
                def.baseGridId = data.baseGridId;
                def.machineCount = data.machineCount;
                def.beltCount = data.beltCount;
                def.blueprintCells = data.blueprintCells;
                def.ticksPerProcess = data.ticksPerProcess;
                def.inputPorts = new List<BlueprintPort>();
                if (data.inputPorts != null)
                {
                    foreach (var p in data.inputPorts)
                    {
                        var port = new BlueprintPort
                        {
                            localPos = new Vector2Int(p.x, p.y),
                            facing = p.facing,
                            itemType = p.itemType,
                            machineRef = null // wird später beim Platzieren im Grid gesetzt
                        };
                        def.inputPorts.Add(port);
                    }
                }

                def.hasOutputPort = data.hasOutputPort;
                if (data.hasOutputPort)
                {
                    def.outputPort = new BlueprintPort
                    {
                        localPos = new Vector2Int(data.outputPort.x, data.outputPort.y),
                        facing = data.outputPort.facing,
                        itemType = data.outputPort.itemType,
                        machineRef = null
                    };
                }

                def.elements = new List<BlueprintElementData>();
                foreach (var e in data.elements)
                {
                    var elem = new BlueprintElementData
                    {
                        elementType = e.elementType,
                        machineKind = e.machineKind,
                        portKind = e.portKind,
                        currentRecipe = ResolveRecipeById(e.recipeId),
                        localPos = new Vector2Int(e.x, e.y),
                        inputDirection = e.inputDirection,
                        outputDirection = e.outputDirection
                    };
                    def.elements.Add(elem);
                }

                return def;
            }
        }

        private static RecipeDefinition ResolveRecipeById(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
                return null;

            var allRecipes = Resources.LoadAll<RecipeDefinition>("");
            foreach (var r in allRecipes)
            {
                if (r != null && r.id == recipeId)
                    return r;
            }

            Debug.LogWarning($"[BlueprintSerialization] Recipe mit ID '{recipeId}' nicht gefunden.");
            return null;
        }
    }
}
