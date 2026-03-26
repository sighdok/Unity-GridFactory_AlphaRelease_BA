using System.Collections.Generic;
using System;

using GridFactory.Directions;
using GridFactory.Grid;
using GridFactory.Blueprints;

namespace GridFactory.Core
{
    [Serializable]
    public class InventoryItemStackData
    {
        public ItemType type;
        public int count;
    }

    [Serializable]
    public class InventorySaveData
    {
        public int gold;
        public int goldRateSumInWindow;
        public float goldPerSecond;
        public float energyFloat;

        public float energyGeneratedThisTick;


        public List<InventoryItemStackData> items = new List<InventoryItemStackData>();
    }


    [System.Serializable]
    public class TechSkillStateSaveData
    {
        public int internalId;
        public int level;
    }

    [System.Serializable]
    public class TechTreeSaveData
    {
        public List<TechSkillStateSaveData> skills = new List<TechSkillStateSaveData>();
    }


    [Serializable]
    public struct BlueprintPortSaveData
    {
        public int x;
        public int y;
        public Direction facing;
        public ItemType itemType;
    }

    [Serializable]
    public class BlueprintElementSaveData
    {
        public BlueprintElementType elementType;
        public MachineKind machineKind;
        public PortKind portKind;
        //public RecipeDefinition currentRecipe;
        public string recipeId;
        public int x;
        public int y;
        public Direction inputDirection;
        public Direction outputDirection;
    }

    [Serializable]
    public class BlueprintSaveData
    {
        public string id;
        public string displayName;

        public string baseGridId;

        public int machineCount;
        public int beltCount;
        public int blueprintCells;

        public int sizeX;
        public int sizeY;

        public float ticksPerProcess;


        public List<BlueprintPortSaveData> inputPorts = new List<BlueprintPortSaveData>();

        public bool hasOutputPort;
        public BlueprintPortSaveData outputPort;

        public List<BlueprintElementSaveData> elements = new List<BlueprintElementSaveData>();
    }

    [Serializable]
    public class BlueprintLibrarySaveData
    {
        public List<BlueprintSaveData> blueprints = new List<BlueprintSaveData>();
    }

    [Serializable]
    public struct CellLockSaveData
    {
        public int x;
        public int y;
        public LockType lockType;
    }

    [Serializable]
    public class OwnedGridDefinitionSaveData
    {
        public string id;
        public string displayName;
        public int width;
        public int height;
        public bool isPreset;
        public int seed;
        public List<CellLockSaveData> locks = new List<CellLockSaveData>();
    }

    [Serializable]
    public class GridLibrarySaveData
    {
        public List<OwnedGridDefinitionSaveData> owned = new List<OwnedGridDefinitionSaveData>();
    }

    [Serializable]
    public class GridShopOfferSaveData
    {
        public string offerId;
        public bool isPreset;

        // preset
        public string presetId;

        // random
        public int width;
        public int height;
        public int seed;

        public int price;
        public string displayName;
    }

    [Serializable]
    public class MetaGridStructureSaveData
    {
        public int width;
        public int height;
        public List<Cell> cells = new();

        [Serializable]
        public class Cell
        {
            public int x, y;
            public bool locked;

            public string machineId;         // null = keine
            public Direction machineFacing;  // = outputDirection

            public ItemType resourceItemType = ItemType.None;  // nur für MetaResourceNode
            public string blueprintId;

            public string conveyorId;        // null = keine
            public Direction conveyorOut;
            public Direction conveyorIn;


            public bool isProcessing;
            public int ticksPerProcess;
            public float currentProcessTick;
            public ItemType itemInProcess;
        }
    }

    [Serializable]
    public class GridShopStateSaveData
    {
        public int refreshCount;
        public List<GridShopOfferSaveData> offers = new List<GridShopOfferSaveData>();
    }



    [System.Serializable]
    public class ResearchCenterItemProgressSaveData
    {
        public ItemType itemType;
        public int current;
        public int target;
    }

    [System.Serializable]
    public class ResearchCenterSaveData
    {
        public string saveId;

        public string currentResearchInternalId; // null/empty = nichts ausgewählt
        public bool processingResearch;

        public float tickCounterFloat; // MetaMachineBase._tickCounterFloat
        public float ticksPerProcess;  // MetaMachineBase.ticksPerProcess

        public List<ResearchCenterItemProgressSaveData> items = new List<ResearchCenterItemProgressSaveData>();
    }

    [System.Serializable]
    public class ResearchCenterLibrarySaveData
    {
        public List<ResearchCenterSaveData> centers = new List<ResearchCenterSaveData>();
    }
}