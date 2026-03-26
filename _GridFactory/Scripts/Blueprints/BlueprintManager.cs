using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using GridFactory.Grid;
using GridFactory.Core;
using GridFactory.Machines;
using GridFactory.Conveyor;
using GridFactory.UI;
using GridFactory.Analysis;
using GridFactory.Meta;
using GridFactory.Inventory;

namespace GridFactory.Blueprints
{
    public class BlueprintManager : MonoBehaviour
    {
        public static BlueprintManager Instance { get; private set; }

        private static GridManager GrM => GridManager.Instance;
        private static MetaGridManager MGM => MetaGridManager.Instance;
        private static EnergyManager EM => EnergyManager.Instance;
        private static UIConfirmationManager UICONFIRMM => UIConfirmationManager.Instance;
        private static PortBuildingController PBC => PortBuildingController.Instance;
        private static BlueprintListUI BPLUI => BlueprintListUI.Instance;
        private static InventoryManager IM => InventoryManager.Instance;
        private static TickManager TM => TickManager.Instance;
        private static GridDefinitionManager GDM => GridDefinitionManager.Instance;

        [Header("Compression Settings")]
        public int beltsPerCompressionPoint = 2;

        [Header("Prefabs für Rekonstruktion")]
        [SerializeField] private GameObject smelterPrefab;
        [SerializeField] private GameObject sawmillPrefab;
        [SerializeField] private GameObject masonPrefab;
        [SerializeField] private GameObject ovenPrefab;
        [SerializeField] private GameObject conveyorPrefab;
        [SerializeField] private GameObject inputPortPrefab;
        [SerializeField] private GameObject outputPortPrefab;
        [SerializeField] private GameObject splitterPrefab;
        [SerializeField] private GameObject mergerPrefab;
        [SerializeField] private GameObject crossingPrefab;

        [Header("Runtime Blueprints")]
        public List<BlueprintDefinition> runtimeBlueprints = new List<BlueprintDefinition>();
        public int conveyorBaseTicksPerStepForBlueprintCalculation;

        public event Action<BluePrintInfo> OnBlueprintInfoUpdated;
        public event Action _onBlueprintSaved;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ClearGrid()
        {
            UICONFIRMM.Show(
                 "Clear Grid?",
                 () =>
                 {
                     ForceClearGrid();
                 }, () => { }
             );
        }

        private void ForceClearGrid()
        {
            for (int x = 0; x < GrM.Width; x++)
            {
                for (int y = 0; y < GrM.Height; y++)
                {
                    GridCell cell = GrM.GetCell(new Vector2Int(x, y));
                    if (cell == null)
                        continue;

                    if (cell.Machine != null)
                    {
                        Destroy(cell.Machine.gameObject);
                        cell.Machine = null;
                    }

                    if (cell.Conveyor != null)
                    {
                        Destroy(cell.Conveyor.gameObject);
                        cell.Conveyor = null;
                    }
                }
            }
            PBC.ResetPorts();
        }

        public BlueprintStats PreviewCurrentGrid()
        {
            int machineCount = 0;
            int beltCount = 0;

            for (int x = 0; x < GrM.Width; x++)
            {
                for (int y = 0; y < GrM.Height; y++)
                {
                    var cell = GrM.GetCell(new Vector2Int(x, y));
                    if (cell == null)
                        continue;

                    if (cell.Machine != null)
                        machineCount++;

                    if (cell.Conveyor != null)
                        beltCount++;
                }
            }
            return CalculateStats(machineCount, beltCount);
        }

        private BlueprintStats CalculateStats(int machines, int belts)
        {
            if (machines <= 0)
                return new BlueprintStats(0, belts, 0);

            int baseCells = machines;
            int K = Mathf.Max(1, beltsPerCompressionPoint);
            int compressionPoints = belts / K;
            int minCells = Mathf.CeilToInt(machines / 2f);
            int blueprintCells = Mathf.Max(minCells, baseCells - compressionPoints);

            return new BlueprintStats(machines, belts, blueprintCells);
        }

        public BlueprintDefinition CreateBlueprintFromCurrentGrid(string name, BlueprintDefinition overwriteBP = null, bool addToRuntime = true)
        {
            BlueprintDefinition myBp;

            int machineCount = 0;
            int beltCount = 0;
            int width = GrM.Width;
            int height = GrM.Height;

            if (overwriteBP == null)
            {
                myBp = ScriptableObject.CreateInstance<BlueprintDefinition>();
                myBp.id = Guid.NewGuid().ToString();
                myBp.displayName = name;
            }
            else
            {
                myBp = overwriteBP;
                myBp.inputPorts.Clear();
                myBp.hasOutputPort = false;

            }

            myBp.baseGridId = GrM.CurrentGridDefinitionId;
            myBp.size = new Vector2Int(width, height);
            myBp.elements.Clear();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var cell = GrM.GetCell(pos);
                    if (cell == null)
                        continue;

                    var machine = cell.Machine;
                    if (machine != null)
                    {
                        if (machine is PortMarker portMarker)
                        {
                            HandlePortMarker(portMarker, pos, myBp);

                            var data = new BlueprintElementData
                            {
                                elementType = BlueprintElementType.Machine,
                                machineKind = DetectMachineKind(machine),
                                portKind = portMarker.portKind,
                                localPos = pos,
                                inputDirection = machine.InputDirection,
                                outputDirection = machine.OutputDirection
                            };

                            myBp.elements.Add(data);
                        }
                        else
                        {
                            var data = new BlueprintElementData
                            {
                                elementType = BlueprintElementType.Machine,
                                machineKind = DetectMachineKind(machine),
                                localPos = pos,
                                inputDirection = machine.InputDirection,
                                outputDirection = machine.OutputDirection,
                                currentRecipe = (machine is MachineWithRecipeBase mwrb) ? mwrb.CurrentRecipe : null
                            };

                            myBp.elements.Add(data);
                        }

                        machineCount++;
                    }
                    else if (cell.Conveyor != null)
                    {
                        beltCount++;

                        var data = new BlueprintElementData
                        {
                            elementType = BlueprintElementType.Conveyor,
                            machineKind = 0,
                            localPos = pos,
                            inputDirection = cell.Conveyor.InputDirection,
                            outputDirection = cell.Conveyor.OutputDirection
                        };

                        myBp.elements.Add(data);
                    }
                }
            }

            if (ValidatePorts(myBp))
            {
                var stats = CalculateStats(machineCount, beltCount);

                myBp.machineCount = stats.machineCount;
                myBp.beltCount = stats.beltCount;
                myBp.blueprintCells = stats.blueprintCells;

                myBp = GetAnalysedBP(myBp);

                OnBlueprintInfoUpdated?.Invoke(myBp.blueprintInfo);
                if (overwriteBP != null)
                    runtimeBlueprints.Remove(overwriteBP);

                if (addToRuntime)
                {
                    runtimeBlueprints.Add(myBp);
                    _onBlueprintSaved?.Invoke();
                }

                BPLUI.RefreshList();
            }

            return myBp;
        }

        public BlueprintDefinition GetAnalysedBP(BlueprintDefinition bp)
        {
            bp.blueprintInfo = new BluePrintInfo();

            var graph = BlueprintThroughputBuilder.BuildFromGrid(GrM);
            graph.ComputeTTFI();

            var validation = graph.Validate();
            if (!validation.IsValid)
            {
                foreach (var err in validation.Errors)
                    bp.blueprintInfo.errors.Add(err);
                foreach (var warn in validation.Warnings)
                    bp.blueprintInfo.warnings.Add(warn);
            }

            try
            {
                graph.SolveThroughput();
                foreach (var inputPort in bp.inputPorts)
                {
                    if (inputPort.machineRef != null)
                    {
                        var node = graph.Nodes.OfType<InputNode>().FirstOrDefault(n => n.Id == inputPort.machineRef.name);
                        bp.blueprintInfo.inputItems.Add(IM.GetDefinition(inputPort.itemType));
                    }
                }

                if (bp.hasOutputPort && bp.outputPort.machineRef != null)
                {
                    var sink = graph.Nodes.OfType<OutputNode>().FirstOrDefault(n => n.Id == bp.outputPort.machineRef.name);

                    if (sink != null)
                    {
                        double minCap = double.MaxValue;
                        foreach (var e in sink.Inputs)
                            minCap = Mathf.Min((float)minCap, (float)e.GetAvailableCapacity());

                        double itemsPerSek = sink.CollectedRate / 60.0;
                        double itemsPerTick = itemsPerSek / (1 / TM.TickInterval);
                        float ticksForProcess = float.PositiveInfinity;

                        if (itemsPerTick > 0)
                        {
                            double conv = 1.0 / itemsPerTick;
                            ticksForProcess = Mathf.Ceil((float)conv);
                        }

                        if (sink.HeldItemType == ItemType.None)
                            bp.blueprintInfo.errors.Add("No Output Item - Check the Blueprint.");

                        bp.blueprintInfo.outputItem = IM.GetDefinition(sink.HeldItemType);
                        bp.blueprintInfo.outputsMin = (float)sink.CollectedRate;
                        bp.blueprintInfo.ticksForItem = ticksForProcess;
                        bp.ticksPerProcess = ticksForProcess;
                        bp.outputPort.itemType = sink.HeldItemType;
                    }
                }

                bp.blueprintInfo.expectedEnergyConsumption = UpkeepForBlueprint(bp);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlueprintManager] Fehler beim Lösen: {ex.Message}");
            }
            return bp;
        }

        public void OverwriteBlueprint(BlueprintDefinition bp)
        {
            BlueprintDefinition newBp = CreateBlueprintFromCurrentGrid(bp.name, bp);

            var cells = MGM.AllCells;
            for (int x = 0; x < MGM.Width; x++)
                for (int y = 0; y < MGM.Height; y++)
                    if (cells[x, y].Machine && cells[x, y].Machine is MetaBlueprintModule myModule && myModule.blueprint.id == newBp.id)
                        myModule.UpdateBlueprint(newBp);
        }

        public void ApplyBlueprint(BlueprintDefinition bp, Vector2Int anchorGridPos)
        {
            if (bp == null)
                return;

            ForceClearGrid();
            TryApplyBlueprintBaseGrid(bp);
            PBC.ResetPorts();

            var elementMap = new Dictionary<Vector2Int, BlueprintElementData>();
            foreach (var elem in bp.elements)
                elementMap[elem.localPos] = elem;

            for (int x = 0; x < bp.size.x; x++)
            {
                for (int y = 0; y < bp.size.y; y++)
                {
                    Vector2Int localPos = new Vector2Int(x, y);
                    Vector2Int gridPos = anchorGridPos + localPos;

                    GridCell cell = GrM.GetCell(gridPos);
                    if (cell == null)
                        continue;

                    if (!elementMap.TryGetValue(localPos, out var elem))
                    {
                        if (cell.Machine != null)
                        {
                            Destroy(cell.Machine.gameObject);
                            cell.Machine = null;
                        }
                        if (cell.Conveyor != null)
                        {
                            Destroy(cell.Conveyor.gameObject);
                            cell.Conveyor = null;
                        }
                        continue;
                    }

                    if (cell.Machine != null)
                    {
                        Destroy(cell.Machine.gameObject);
                        cell.Machine = null;
                    }
                    if (cell.Conveyor != null)
                    {
                        Destroy(cell.Conveyor.gameObject);
                        cell.Conveyor = null;
                    }

                    switch (elem.elementType)
                    {
                        case BlueprintElementType.Machine:
                            PlaceMachineFromBlueprint(bp, elem, cell);
                            break;

                        case BlueprintElementType.Conveyor:
                            PlaceConveyorFromBlueprint(elem, cell);
                            break;
                    }
                }
            }

            RefreshAllConveyors();
            HackyUpdateCurrentBlueprintInfo();
            GrM.UpdateSpriteSortingByY();
        }


        private void TryApplyBlueprintBaseGrid(BlueprintDefinition bp)
        {
            if (bp == null || string.IsNullOrWhiteSpace(bp.baseGridId))
                return;

            var owned = GDM.GetOwnedById(bp.baseGridId);
            if (owned != null)
            {
                GrM.ApplyOwnedGrid(owned);
                return;
            }
        }

        public void DeleteBlueprint(BlueprintDefinition bp)
        {
            if (bp == null)
                return;

            var allBlueprintModules = FindObjectsByType<MetaBlueprintModule>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allBlueprintModules)
                if (c is MetaBlueprintModule mbpm && mbpm.blueprint != null && mbpm.blueprint.id == bp.id)
                    mbpm.ResetBlueprint();

            runtimeBlueprints.Remove(bp);
            BPLUI.RefreshList();
        }

        private void HandlePortMarker(PortMarker portMarker, Vector2Int pos, BlueprintDefinition bp)
        {
            var port = new BlueprintPort
            {
                localPos = pos,
                facing = portMarker.OutputDirection,
                itemType = portMarker.portItemType,
                machineRef = portMarker
            };

            if (portMarker.portKind == PortKind.Input)
            {
                bp.inputPorts.Add(port);
                bp.hasInputPort = true;
            }
            else if (portMarker.portKind == PortKind.Output)
            {
                bp.outputPort = port;
                bp.hasOutputPort = true;
            }
        }

        public List<MetaBlueprintModule> GetMachinesWithBlueprint(BlueprintDefinition bp)
        {
            List<MetaBlueprintModule> myMachines = new List<MetaBlueprintModule>();
            var allBlueprintModules = FindObjectsByType<MetaBlueprintModule>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var c in allBlueprintModules)
                if (c is MetaBlueprintModule mbpm && mbpm.blueprint != null && mbpm.blueprint.id == bp.id)
                    myMachines.Add(mbpm);
            return myMachines;
        }

        private MachineKind DetectMachineKind(MachineBase machine)
        {
            if (machine is Smelter) return MachineKind.Smelter;
            if (machine is Sawmill) return MachineKind.Sawmill;
            if (machine is Mason) return MachineKind.Mason;
            if (machine is Oven) return MachineKind.Oven;
            if (machine is Splitter) return MachineKind.Splitter;
            if (machine is Merger) return MachineKind.Merger;
            if (machine is Crossing) return MachineKind.Crossing;
            if (machine is PortMarker) return MachineKind.PortMarker; // oder eigener Eintrag, wenn du willst           

            return MachineKind.None;
        }

        private void PlaceMachineFromBlueprint(BlueprintDefinition bp, BlueprintElementData elem, GridCell cell)
        {
            if (cell.Machine != null || cell.Conveyor != null)
                return;

            GameObject prefab = null;

            switch (elem.machineKind)
            {
                case MachineKind.Smelter: prefab = smelterPrefab; break;
                case MachineKind.Sawmill: prefab = sawmillPrefab; break;
                case MachineKind.Mason: prefab = masonPrefab; break;
                case MachineKind.Oven: prefab = ovenPrefab; break;
                case MachineKind.Splitter: prefab = splitterPrefab; break;
                case MachineKind.Merger: prefab = mergerPrefab; break;
                case MachineKind.Crossing: prefab = crossingPrefab; break;
                case MachineKind.PortMarker:
                    if (elem.portKind == PortKind.Input)
                        prefab = inputPortPrefab;
                    else
                        prefab = outputPortPrefab;
                    break;
            }

            if (prefab == null)
                return;

            Vector3 worldPos = GrM.GridToWorld(cell.Position);
            GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);
            go.transform.parent = GrM.transform;

            var machine = go.GetComponent<MachineBase>();
            if (machine != null)
            {
                machine.Init(cell);
                machine.SetFacing(elem.outputDirection);

                if (machine is MachineWithRecipeBase mwrb)
                {
                    mwrb.CurrentRecipe = elem.currentRecipe;
                    mwrb.BuildInputOutputSides();
                    mwrb.UpdateArrowPositions();
                }

                if (machine is PortMarker portMarker)
                {
                    portMarker.portKind = elem.portKind;

                    if (elem.portKind == PortKind.Input)
                    {
                        var bpPort = bp.inputPorts.FirstOrDefault(p => p.localPos == elem.localPos);
                        portMarker.portItemType = bpPort.itemType;
                    }
                    else if (elem.portKind == PortKind.Output)
                    {
                        if (bp.hasOutputPort && bp.outputPort.localPos == elem.localPos)
                            portMarker.portItemType = bp.outputPort.itemType;
                    }
                    PBC.SetPortOnCell(portMarker, cell.Position);
                }

                cell.Machine = machine;
            }
        }

        private void PlaceConveyorFromBlueprint(BlueprintElementData elem, GridCell cell)
        {
            if (cell.Machine != null || cell.Conveyor != null || conveyorPrefab == null)
                return;

            Vector3 worldPos = GrM.GridToWorld(cell.Position);
            GameObject go = Instantiate(conveyorPrefab, worldPos, Quaternion.identity);
            go.transform.parent = GrM.transform;

            var conv = go.GetComponent<ConveyorBase>();
            if (conv != null)
            {
                conv.Init(cell);
                conv.SetOutputDirection(elem.outputDirection);
                conv.SetInputDirection(elem.inputDirection);
                cell.Conveyor = conv;
            }
        }

        private float UpkeepForBlueprint(BlueprintDefinition bp)
        {
            if (bp == null || bp.elements == null)
                return 0f;

            int conveyorCount = 0;
            int machineCountTemp = 0;
            float machineWeightSum = 0f;

            foreach (BlueprintElementData e in bp.elements)
            {
                if (e == null)
                    continue;

                if (e.elementType == BlueprintElementType.Conveyor)
                {
                    conveyorCount++;
                    continue;
                }

                if (e.elementType == BlueprintElementType.Machine)
                {
                    machineCountTemp++;
                    bool isPort = e.portKind == PortKind.Input || e.portKind == PortKind.Output;
                    machineWeightSum += isPort ? (EM.upkeepPerPort / Mathf.Max(0.00001f, EM.upkeepPerMachine)) : 1f;
                }
            }
            float result =
                EM.upkeepBase +
                machineWeightSum * EM.upkeepPerMachine +
                conveyorCount * EM.upkeepPerConveyor;
            var resultDouble = Mathf.Round(result * 1000) / 1000.0;
            return (float)resultDouble;
        }

        public BlueprintDefinition FindBlueprintById(string id)
        {
            if (string.IsNullOrEmpty(id) || runtimeBlueprints == null)
                return null;

            for (int i = 0; i < runtimeBlueprints.Count; i++)
                if (runtimeBlueprints[i] != null && runtimeBlueprints[i].id == id)
                    return runtimeBlueprints[i];
            return null;
        }

        public BlueprintDefinition HackyUpdateCurrentBlueprintInfo()
        {
            var analysedBP = CreateBlueprintFromCurrentGrid("test", null, false);
            return analysedBP;
        }

        private bool ValidatePorts(BlueprintDefinition bp)
        {
            bool check = true;

            if (bp.inputPorts == null || bp.inputPorts.Count == 0)
                check = false;

            if (!bp.hasOutputPort)
                check = false;

            if (bp.inputPorts == null || bp.inputPorts.Count == 0 || !bp.hasOutputPort)
                return check;
            /*
            Vector2Int size = bp.size;
            foreach (var input in bp.inputPorts)
            {
                if (!IsOnEdge(input.localPos, size))
                    check = false;
                if (!IsFacingInside(input, true))
                    check = false;
            }

            if (!IsOnEdge(bp.outputPort.localPos, size))
                check = false;

            if (!IsFacingInside(bp.outputPort, false))
                check = false;
            */
            foreach (var input in bp.inputPorts)
                if (input.localPos == bp.outputPort.localPos && input.facing == bp.outputPort.facing)
                    check = false;

            return check;
        }

        private void RefreshAllConveyors()
        {
            var allConv = FindObjectsByType<ConveyorBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allConv)
                c.UpdateShapeAndRotation();
        }

        /*
        private bool IsOnEdge(Vector2Int pos, Vector2Int size)
        {
            return pos.x == 0 || pos.y == 0 || pos.x == size.x - 1 || pos.y == size.y - 1;
        }

        private bool IsFacingInside(BlueprintPort port, bool isInput)
        {
            Vector2Int size = new Vector2Int(GrM.Width, grid.Height);

            bool atLeft = port.localPos.x == 0;
            bool atRight = port.localPos.x == size.x - 1;
            bool atBottom = port.localPos.y == 0;
            bool atTop = port.localPos.y == size.y - 1;

            if (isInput)
            {
                if (atLeft && port.facing == Direction.Left) return false;
                if (atRight && port.facing == Direction.Right) return false;
                if (atBottom && port.facing == Direction.Down) return false;
                if (atTop && port.facing == Direction.Up) return false;
            }
            else
            {
                if (atLeft && port.facing == Direction.Right) return false;
                if (atRight && port.facing == Direction.Left) return false;
                if (atBottom && port.facing == Direction.Up) return false;
                if (atTop && port.facing == Direction.Down) return false;
            }

            return true;
        }
        */
    }
}
