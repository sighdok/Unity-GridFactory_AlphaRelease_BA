using System.Collections.Generic;
using GridFactory.Core;
using UnityEngine;

namespace GridFactory.Blueprints
{
    public class BluePrintInfo
    {
        public List<ItemDefinition> inputItems;
        public ItemDefinition outputItem;
        public float outputsMin;
        public float ticksForItem;
        public float expectedEnergyConsumption;
        public List<string> errors;
        public List<string> warnings;

        public BluePrintInfo()
        {
            inputItems = new List<ItemDefinition>();
            outputItem = null;
            outputsMin = 0;
            ticksForItem = 0;
            errors = new List<string>();
            warnings = new List<string>();
            expectedEnergyConsumption = 0;
        }
    }

    [CreateAssetMenu(menuName = "GridFactory/BlueprintDefinition")]
    public class BlueprintDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string baseGridId;

        public int machineCount;
        public int beltCount;
        public int blueprintCells;

        public bool hasInputPort;
        public bool hasOutputPort;
        public Vector2Int size;

        public List<BlueprintElementData> elements = new List<BlueprintElementData>();
        public List<BlueprintPort> inputPorts = new List<BlueprintPort>();
        public BlueprintPort outputPort;

        public float ticksPerProcess;

        public BluePrintInfo blueprintInfo;
    }
}