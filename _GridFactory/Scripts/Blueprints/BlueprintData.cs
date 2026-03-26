using System;

using UnityEngine;

using GridFactory.Core;
using GridFactory.Directions;
using GridFactory.Machines;

namespace GridFactory.Blueprints
{
    public enum BlueprintElementType
    {
        Machine,
        Conveyor
    }

    [Serializable]
    public struct BlueprintStats
    {
        public int machineCount;
        public int beltCount;
        public int blueprintCells;

        public BlueprintStats(int machineCount, int beltCount, int blueprintCells)
        {
            this.machineCount = machineCount;
            this.beltCount = beltCount;
            this.blueprintCells = blueprintCells;
        }
    }

    [Serializable]
    public struct BlueprintPort
    {
        public Vector2Int localPos; // Gridpos im Micro-Grid
        public Direction facing;    // Richtung nach INNEN (für Input) bzw. aus dem Prozess (für Output)
        public ItemType itemType;   // z.B. Ore, Bar, Plate
        public PortMarker machineRef;
    }

    [Serializable]
    public class BlueprintElementData
    {
        public BlueprintElementType elementType;
        public MachineKind machineKind; // nur relevant, wenn Machine
        public RecipeDefinition currentRecipe; // nur relevant, wenn Machine
        public PortKind portKind; // nur relevant, wenn Machine
        public Vector2Int localPos;     // Position relativ zum Blueprint-Ursprung
        public Direction inputDirection;     // Conveyor.direction oder Maschinen-Facing/Output
        public Direction outputDirection;     // Conveyor.direction oder Maschinen-Facing/Output
    }
}
