using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using GridFactory.Core;
using GridFactory.Grid;
using GridFactory.Machines;
using GridFactory.Blueprints;
using GridFactory.Directions;

namespace GridFactory.Analysis
{
    public static class BlueprintThroughputBuilder
    {
        // ---- Simulation constants ----
        public const double TickDurationSeconds = 0.025;

        // ---- DEBUG SWITCHES ----
        private const bool DBG = false;                      // master switch
        private const bool DBG_VERBOSE_PATH = true;         // logs every traversal step
        private const bool DBG_VERBOSE_START_GATES = true;  // logs each attempted start dir
        private const bool DBG_DUMP_GRAPH = true;           // dumps all edges at end
        private const bool DBG_DUMP_NODE_IO = false;        // dumps inputs/outputs per node at end
        private const int DBG_MAX_STEPS = 300;              // traversal step limit to avoid infinite loops

        private static string D(Direction d) => d.ToString();
        private static string P(Vector2Int p) => $"({p.x},{p.y})";

        private static void Log(string msg) { if (DBG) Debug.Log(msg); }
        private static void Warn(string msg) { if (DBG) Debug.LogWarning(msg); }
        private static void Err(string msg) { if (DBG) Debug.LogError(msg); }

        // ---- Public API ----

        public static FactoryGraph BuildFromGrid(GridManager grid)
        {
            return BuildInternal(
                grid,
                metaMode: false,
                out _,
                out _);
        }

        public static FactoryGraph BuildForMeta(
            GridManager grid,
            out Dictionary<ItemType, List<InputNode>> inputNodesByType,
            out List<OutputNode> outputNodes)
        {
            return BuildInternal(
                grid,
                metaMode: true,
                out inputNodesByType,
                out outputNodes);
        }

        // ---- Core build ----

        private static FactoryGraph BuildInternal(
            GridManager grid,
            bool metaMode,
            out Dictionary<ItemType, List<InputNode>> inputNodesByType,
            out List<OutputNode> outputNodes)
        {
            var graph = new FactoryGraph();

            inputNodesByType = new Dictionary<ItemType, List<InputNode>>();
            outputNodes = new List<OutputNode>();

            var nodeMap = new Dictionary<MachineBase, Node>();

            // 1) Create nodes
            foreach (var cell in grid.AllCells)
            {
                if (cell.Machine == null) continue;

                var machine = cell.Machine;

                // Crossing: transparent, no node
                if (machine is Crossing)
                    continue;

                Node node = CreateNodeForMachine(machine, metaMode, inputNodesByType, outputNodes);
                if (node == null) continue;

                graph.AddNode(node);
                nodeMap[machine] = node;

                // Debug: Machine -> cell mapping sanity
                if (DBG)
                {
                    var mCell = grid.GetCellFromWorld(machine.transform.position);
                    if (mCell == null)
                        Warn($"[TB] Machine '{machine.name}' world={machine.transform.position} -> cell=NULL (GetCellFromWorld failed)");
                    else
                        Log($"[TB] Machine '{machine.name}' type={machine.GetType().Name} world={machine.transform.position} -> cell={P(mCell.Position)}");
                }
            }

            // 2) Build edges by traversing from each node in each direction
            foreach (var kvp in nodeMap)
            {
                MachineBase originMachine = kvp.Key;
                Node fromNode = kvp.Value;

                var originCell = grid.GetCellFromWorld(originMachine.transform.position);
                if (originCell == null)
                {
                    Warn($"[TB] SKIP origin '{originMachine.name}' because originCell is NULL");
                    continue;
                }

                Vector2Int originPos = originCell.Position;

                foreach (Direction dir in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
                {
                    TryBuildEdgeFromDirection(
                        grid,
                        graph,
                        nodeMap,
                        originMachine,
                        originPos,
                        fromNode,
                        dir);
                }
            }

            // Final debug dumps
            if (DBG_DUMP_GRAPH)
            {
                Log("[TB] ===== GRAPH EDGES =====");
                foreach (var e in graph.Edges)
                {
                    string cap = (e.MaxCapacity <= 0) ? "inf" : e.MaxCapacity.ToString("F2");
                    Log($"[TB] EDGE {e.From.Id} -> {e.To.Id} cap={cap}");
                }
                Log("[TB] =======================");
            }

            if (DBG_DUMP_NODE_IO)
            {
                Log("[TB] ===== NODE IO =====");
                foreach (var n in graph.Nodes)
                {
                    Log($"[TB] Node '{n.Id}' type={n.GetType().Name} IN={n.Inputs.Count} OUT={n.Outputs.Count}");
                    foreach (var i in n.Inputs) Log($"[TB]   IN  {i.From.Id} -> {n.Id}");
                    foreach (var o in n.Outputs) Log($"[TB]   OUT {n.Id} -> {o.To.Id}");
                }
                Log("[TB] ===================");
            }

            return graph;
        }

        // ---- Traversal per direction ----

        private static void TryBuildEdgeFromDirection(
            GridManager grid,
            FactoryGraph graph,
            Dictionary<MachineBase, Node> nodeMap,
            MachineBase originMachine,
            Vector2Int originPos,
            Node fromNode,
            Direction startDir)
        {
            Vector2Int firstPos = originPos + DirectionUtils.DirectionToOffset(startDir);
            var firstCell = grid.GetCell(firstPos);

            if (DBG_VERBOSE_START_GATES)
                Log($"[TB] START origin='{originMachine.name}' ({fromNode.Id}) startDir={D(startDir)} originPos={P(originPos)} firstPos={P(firstPos)} firstCell={(firstCell == null ? "NULL" : DescribeCell(firstCell))}");

            if (firstCell == null)
                return;

            // Gate A: first neighbor must be a valid "receiving" segment from the origin
            if (!CellReceivesFrom(grid, firstPos, originPos, startDir, out string startFail))
            {
                if (DBG_VERBOSE_START_GATES)
                    Log($"[TB]  START-REJECT origin='{originMachine.name}' dir={D(startDir)} firstPos={P(firstPos)} reason={startFail}");
                return;
            }

            // Traverse along conveyors/crossings until we hit a target machine (non-crossing)
            var visited = new HashSet<Vector2Int>();
            visited.Add(originPos);

            double minCapacity = double.MaxValue;

            Vector2Int currentPos = firstPos;
            Direction travelDir = startDir;

            int steps = 0;

            while (true)
            {
                steps++;
                if (steps > DBG_MAX_STEPS)
                {
                    Warn($"[TB]  ABORT step-limit origin='{originMachine.name}' dir0={D(startDir)} at={P(currentPos)} travelDir={D(travelDir)}");
                    return;
                }

                if (!visited.Add(currentPos))
                {
                    Warn($"[TB]  ABORT loop-detected origin='{originMachine.name}' dir0={D(startDir)} at={P(currentPos)} travelDir={D(travelDir)}");
                    return;
                }

                GridCell c = grid.GetCell(currentPos);
                if (c == null)
                {
                    Log($"[TB]  ABORT cell-null origin='{originMachine.name}' dir0={D(startDir)} at={P(currentPos)}");
                    return;
                }

                if (DBG_VERBOSE_PATH)
                    Log($"[TB]   STEP origin='{originMachine.name}' dir0={D(startDir)} at={P(currentPos)} travelDir={D(travelDir)} cell={DescribeCell(c)}");

                // Target machine?
                if (c.Machine != null && c.Machine != originMachine && c.Machine is not Crossing)
                {
                    // Gate B: the segment directly before the machine must actually output into the machine cell
                    Vector2Int prevPos = currentPos + DirectionUtils.DirectionToOffset(DirectionUtils.Opposite(travelDir));

                    if (!CellOutputsInto(grid, prevPos, currentPos, travelDir, out string endFail))
                    {
                        Log($"[TB]  END-REJECT origin='{originMachine.name}' dir0={D(startDir)} target='{c.Machine.name}' targetPos={P(currentPos)} prevPos={P(prevPos)} travelDir={D(travelDir)} reason={endFail}");
                        return;
                    }

                    if (!nodeMap.TryGetValue(c.Machine, out Node toNode))
                    {
                        Warn($"[TB]  END-NODEMISSING origin='{originMachine.name}' hit machine='{c.Machine.name}' type={c.Machine.GetType().Name} but not in nodeMap");
                        return;
                    }

                    double cap = (minCapacity == double.MaxValue) ? 0.0 : minCapacity;

                    Log($"[TB]  EDGE-OK {fromNode.Id} -> {toNode.Id} cap={cap:F2} origin='{originMachine.name}' dir0={D(startDir)} target='{c.Machine.name}'");
                    if (c.Machine is Splitter splitter)
                    {
                        Direction incomingSide = DirectionUtils.Opposite(travelDir);

                        // TODO: diese Funktion musst du auf Basis deiner Splitter-Rotation/Facing korrekt machen
                        Direction inputPortSide = splitter.InputDirection;

                        if (incomingSide != inputPortSide)
                        {
                            Log($"[TB]  END-REJECT(SPLITTER-PORT) origin='{originMachine.name}' -> splitter='{c.Machine.name}' " +
                                $"incomingSide={D(incomingSide)} expectedInputSide={D(inputPortSide)} travelDir={D(travelDir)}");
                            return;
                        }
                    }

                    graph.AddEdge(fromNode, toNode, cap);
                    return;
                }



                // Step forward over conveyor/crossing
                if (!TryStepFromCell(grid, currentPos, travelDir, out Vector2Int nextPos, out Direction nextDir, out int ticksPerStep, out string stepFail))
                {
                    Log($"[TB]  ABORT dead-end origin='{originMachine.name}' dir0={D(startDir)} at={P(currentPos)} travelDir={D(travelDir)} reason={stepFail}");
                    return;
                }

                // Update capacity (min over path)
                int safeTicks = Mathf.Max(1, ticksPerStep);
                double timePerStepSeconds = safeTicks * TickDurationSeconds;
                double stepsPerMinute = 60.0 / timePerStepSeconds;
                double segCap = stepsPerMinute;

                if (segCap < minCapacity) minCapacity = segCap;

                currentPos = nextPos;
                travelDir = nextDir;
            }
        }

        // ---- Gate / Step helpers (geometry-based) ----

        /// <summary>
        /// Checks whether the cell at cellPos (Conveyor or Crossing) is a valid first segment
        /// that can be fed by fromPos when the intended start direction is travelDir.
        /// </summary>
        private static bool CellReceivesFrom(
            GridManager grid,
            Vector2Int cellPos,
            Vector2Int fromPos,
            Direction travelDir,
            out string reason)
        {
            reason = "OK";

            var cell = grid.GetCell(cellPos);
            if (cell == null) { reason = "cell=null"; return false; }

            if (cell.Conveyor != null)
            {
                var belt = cell.Conveyor;

                // Position-based: belt receives from the neighbor located at belt.inputDirection
                Vector2Int expectedFromPos = cellPos + DirectionUtils.DirectionToOffset(belt.InputDirection);
                if (expectedFromPos != fromPos)
                {
                    reason = $"conveyor input mismatch: belt.inputDir={D(belt.InputDirection)} expectedFrom={P(expectedFromPos)} fromPos={P(fromPos)} belt.outDir={D(belt.OutputDirection)}";
                    return false;
                }

                return true;
            }

            if (cell.Machine is Crossing crossing)
            {
                // Entering crossing from fromPos -> compute entry side relative to crossing
                // We don't know your exact crossing rules. We log both checks.
                Direction enterSide = DirectionUtils.Opposite(travelDir);

                bool okA = crossing.HasInputInDirection(travelDir);
                bool okB = crossing.HasInputInDirection(enterSide);

                if (!okA && !okB)
                {
                    reason = $"crossing rejects: HasInput(travelDir={D(travelDir)})={okA}, HasInput(enterSide={D(enterSide)})={okB}";
                    return false;
                }

                return true;
            }

            reason = "no conveyor and not crossing";
            return false;
        }

        /// <summary>
        /// Checks whether the cell at cellPos (Conveyor or Crossing) outputs into toPos.
        /// travelDir is the direction we are moving as we reach the machine.
        /// </summary>
        private static bool CellOutputsInto(
            GridManager grid,
            Vector2Int cellPos,
            Vector2Int toPos,
            Direction travelDir,
            out string reason)
        {
            reason = "OK";

            var cell = grid.GetCell(cellPos);
            if (cell == null) { reason = "cell=null"; return false; }

            if (cell.Conveyor != null)
            {
                var belt = cell.Conveyor;

                Vector2Int outPos = cellPos + DirectionUtils.DirectionToOffset(belt.OutputDirection);
                if (outPos != toPos)
                {
                    reason = $"conveyor output mismatch: belt.outDir={D(belt.OutputDirection)} outPos={P(outPos)} toPos={P(toPos)} belt.inputDir={D(belt.InputDirection)}";
                    return false;
                }

                return true;
            }

            if (cell.Machine is Crossing crossing)
            {
                // "Pass-through" assumption: crossing outputs in travelDir
                Vector2Int outPos = cellPos + DirectionUtils.DirectionToOffset(travelDir);

                bool okA = crossing.HasInputInDirection(travelDir);
                bool okB = crossing.HasInputInDirection(DirectionUtils.Opposite(travelDir));

                if (!okA && !okB)
                {
                    reason = $"crossing cannot pass-through: HasInput(travelDir={D(travelDir)})={okA}, HasInput(opposite)={okB}";
                    return false;
                }

                if (outPos != toPos)
                {
                    reason = $"crossing outPos mismatch: travelDir={D(travelDir)} outPos={P(outPos)} toPos={P(toPos)}";
                    return false;
                }

                return true;
            }

            reason = "no conveyor and not crossing";
            return false;
        }

        /// <summary>
        /// Moves from the current cell to the next cell following conveyor/crossing logic.
        /// </summary>
        private static bool TryStepFromCell(
            GridManager grid,
            Vector2Int currentPos,
            Direction travelDir,
            out Vector2Int nextPos,
            out Direction nextDir,
            out int ticksPerStep,
            out string reason)
        {
            nextPos = currentPos;
            nextDir = travelDir;
            ticksPerStep = BlueprintManager.Instance.conveyorBaseTicksPerStepForBlueprintCalculation;
            reason = "OK";

            var cell = grid.GetCell(currentPos);
            if (cell == null) { reason = "cell=null"; return false; }

            if (cell.Conveyor != null)
            {
                var belt = cell.Conveyor;

                // Follow belt output direction
                nextDir = belt.OutputDirection;
                ticksPerStep = belt.ticksPerStep;

                nextPos = currentPos + DirectionUtils.DirectionToOffset(nextDir);
                return true;
            }

            if (cell.Machine is Crossing crossing)
            {


                // Pass-through assumption: keep travelDir
                bool okA = crossing.HasInputInDirection(travelDir);
                //bool okB = crossing.HasInputInDirection(DirectionUtils.Opposite(travelDir));

                /*
                
                 private static ConveyorSim CreateBeltFromCrossing(Crossing crossing, Direction expectedInputDirection, int ticksPerStep)
        {
            if (crossing.HasInputInDirection(expectedInputDirection))
            {
                // Don't CHANGE the PARAMETERS! IT doesn't make sense but works... -.-
                var conv = new ConveyorSim(DirectionUtils.Opposite(expectedInputDirection), expectedInputDirection, ticksPerStep);
                return conv;
            }
            return null;
        }
        */
                if (!okA)
                {
                    reason = $"crossing blocks travelDir={D(travelDir)} (HasInput(travelDir)={okA})";
                    Debug.Log(reason);
                    return false;
                }

                nextDir = travelDir;
                ticksPerStep = BlueprintManager.Instance.conveyorBaseTicksPerStepForBlueprintCalculation;
                nextPos = currentPos + DirectionUtils.DirectionToOffset(nextDir);
                return true;
            }

            reason = "no conveyor/crossing to step";
            return false;
        }

        // ---- Node creation ----

        private static Node CreateNodeForMachine(
            MachineBase machine,
            bool metaMode,
            Dictionary<ItemType, List<InputNode>> inputNodesByType,
            List<OutputNode> outputNodes)
        {
            if (machine is PortMarker port)
            {
                if (port.portKind == PortKind.Input)
                {
                    if (metaMode)
                    {
                        var n = new InputNode(port.name, 0.0, port.portItemType);
                        if (!inputNodesByType.TryGetValue(port.portItemType, out var list))
                        {
                            list = new List<InputNode>();
                            inputNodesByType[port.portItemType] = list;
                        }
                        list.Add(n);
                        return n;
                    }
                    else
                    {
                        double craftTimeMs = port.TicksPerProcess * TickDurationSeconds * 1000.0;
                        double rate = (craftTimeMs <= 0) ? 0.0 : (60000.0 / craftTimeMs);
                        return new InputNode(port.name, rate, port.portItemType);
                    }
                }

                if (port.portKind == PortKind.Output)
                {
                    var n = new OutputNode(port.name);
                    outputNodes?.Add(n);
                    return n;
                }

                return null;
            }

            if (machine is Splitter) return new SplitterNode(machine.name);
            if (machine is Merger) return new MergerNode(machine.name);

            double craftTimeMs2 = machine.TicksPerProcess * TickDurationSeconds * 1000.0;

            if (machine is MachineWithRecipeBase mwb && mwb.CurrentRecipe != null)
            {
                var recipe = mwb.CurrentRecipe;

                var inputAmounts = new Dictionary<ItemType, int>();
                foreach (var ri in recipe.inputItems)
                {
                    if (ri?.item == null) continue;
                    var type = ri.item.type;
                    if (type == ItemType.None) continue;
                    inputAmounts[type] = inputAmounts.TryGetValue(type, out int cur) ? (cur + ri.amount) : ri.amount;
                }

                var outputAmounts = new Dictionary<ItemType, int>();
                foreach (var ro in recipe.outputItems)
                {
                    if (ro?.item == null) continue;
                    var type = ro.item.type;
                    if (type == ItemType.None) continue;
                    outputAmounts[type] = outputAmounts.TryGetValue(type, out int cur) ? (cur + ro.amount) : ro.amount;
                }

                return new RecipeMachineNode(machine.name, craftTimeMs2, inputAmounts, outputAmounts, parallelMachines: 1);
            }

            GetMachineItemTypes(machine, out ItemType inType, out ItemType outType);
            return new MachineNode(machine.name, craftTimeMs2, inType, outType, parallelMachines: 1);
        }

        private static void GetMachineItemTypes(MachineBase machine, out ItemType inputType, out ItemType outputType)
        {
            inputType = ItemType.None;
            outputType = ItemType.None;

            if (machine is MachineWithRecipeBase myMachine && myMachine.CurrentRecipe != null)
            {
                if (myMachine.CurrentRecipe.inputItems != null &&
                    myMachine.CurrentRecipe.inputItems.Length > 0 &&
                    myMachine.CurrentRecipe.inputItems[0]?.item != null)
                {
                    inputType = myMachine.CurrentRecipe.inputItems[0].item.type;
                }

                if (myMachine.CurrentRecipe.outputItems != null &&
                    myMachine.CurrentRecipe.outputItems.Length > 0 &&
                    myMachine.CurrentRecipe.outputItems[0]?.item != null)
                {
                    outputType = myMachine.CurrentRecipe.outputItems[0].item.type;
                }
            }
        }

        // ---- Debug helpers ----

        private static string DescribeCell(GridCell c)
        {
            string m = (c.Machine == null) ? "mach=null" : $"mach={c.Machine.GetType().Name}('{c.Machine.name}')";
            string conv = (c.Conveyor == null) ? "conv=null" : $"conv(in={D(c.Conveyor.InputDirection)}, out={D(c.Conveyor.OutputDirection)}, tps={c.Conveyor.ticksPerStep})";
            return $"{m}, {conv}, pos={P(c.Position)}";
        }
    }
}
