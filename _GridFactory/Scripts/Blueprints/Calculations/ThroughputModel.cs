using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GridFactory.Core
{
    public class Edge
    {
        public Node From { get; }
        public Node To { get; }
        public double Rate;             // berechnete Items/Minute
        public double MaxCapacity;      // 0 oder weniger = unendlich

        public ItemType ItemType { get; set; } = ItemType.None;

        public Edge(Node from, Node to, double maxCapacity = 0)
        {
            From = from;
            To = to;
            MaxCapacity = maxCapacity;
        }

        public double GetAvailableCapacity()
        {
            if (MaxCapacity <= 0) return double.MaxValue;
            return MaxCapacity;
        }
    }

    public abstract class Node
    {
        public string Id { get; }
        public List<Edge> Inputs { get; } = new();
        public List<Edge> Outputs { get; } = new();
        public ItemType HeldItemType { get; protected set; } = ItemType.None;

        /// <summary>
        /// Approximate Time-To-First-Item at this node in seconds.
        /// </summary>
        public double TimeToFirstItemSeconds { get; set; } = double.PositiveInfinity;

        protected Node(string id)
        {
            Id = id;
        }

        public abstract void Compute();

        /// <summary>
        /// Hilfsfunktion: gibt den dominanten ItemType aus allen Inputs zurück.
        /// Aktuell simpel: erster Input mit Rate > 0.
        /// </summary>
        protected ItemType DominantInputItemType()
        {
            foreach (var e in Inputs)
            {
                if (e.Rate > 0 && e.ItemType != ItemType.None)
                    return e.ItemType;
            }
            return ItemType.None;
        }

        protected double TotalInputRate() => Inputs.Sum(e => e.Rate);
    }

    public class InputNode : Node
    {
        public double SourceRate; // Items/Minute

        public ItemType SourceItemType { get; }

        public InputNode(string id, double sourceRate, ItemType itemType) : base(id)
        {
            SourceRate = sourceRate;
            SourceItemType = itemType;
        }

        public override void Compute()
        {
            HeldItemType = SourceItemType;

            if (Outputs.Count == 0) return;
            double remaining = SourceRate;

            double ideal = SourceRate / Outputs.Count;

            foreach (var edge in Outputs)
            {
                if (remaining <= 0)
                {
                    edge.Rate = 0;
                    edge.ItemType = HeldItemType;
                    continue;
                }

                double cap = edge.GetAvailableCapacity();
                double assign = Math.Min(ideal, cap);
                edge.Rate = assign;
                edge.ItemType = HeldItemType;
                remaining -= assign;
            }
        }
    }

    public class MachineNode : Node
    {
        public double CraftTimeMs;   // Zeit pro Item
        public int ParallelMachines;
        public ItemType InputItemType { get; }
        public ItemType OutputItemType { get; }

        public MachineNode(string id, double craftTimeMs, ItemType inputType, ItemType outputType, int parallelMachines = 1)
             : base(id)
        {
            CraftTimeMs = craftTimeMs;
            ParallelMachines = parallelMachines;
            InputItemType = inputType;
            OutputItemType = outputType;
        }

        private double MaxOutputRate()
        {
            return (60000.0 / CraftTimeMs) * ParallelMachines;
        }

        public override void Compute()
        {
            if (Outputs.Count == 0) return;

            double inputRate = TotalInputRate();
            double maxOut = MaxOutputRate();

            // Input-Typ bestimmen (für Debug/Prüfung)
            var incomingType = DominantInputItemType();

            // Optional: Validierungs-Check
            if (incomingType != ItemType.None && incomingType != InputItemType)
            {
                // Hier könntest du debuggen / warnen
                // Console.WriteLine($"[WARN] {Id} erwartet {InputItemType}, bekommt {incomingType}");
            }

            // Output kann nicht mehr sein als Input
            double rawOutput = Math.Min(inputRate, maxOut);

            var edge = Outputs[0];
            double cap = edge.GetAvailableCapacity();
            double finalRate = Math.Min(rawOutput, cap);
            edge.Rate = finalRate;

            // ItemType wird zum OutputType der Maschine
            HeldItemType = finalRate > 0 ? OutputItemType : ItemType.None;
            edge.ItemType = HeldItemType;
        }
    }

    public class SplitterNode : Node
    {
        public SplitterNode(string id) : base(id) { }

        public override void Compute()
        {
            double totalIn = TotalInputRate();
            ItemType inType = DominantInputItemType();
            HeldItemType = (totalIn > 0) ? inType : ItemType.None;

            if (Outputs.Count == 0 || totalIn <= 0)
            {
                foreach (var o in Outputs)
                {
                    o.Rate = 0;
                    o.ItemType = HeldItemType;
                }
                return;
            }

            double remaining = totalIn;
            double idealShare = totalIn / Outputs.Count;

            foreach (var edge in Outputs)
            {
                if (remaining <= 0)
                {
                    edge.Rate = 0;
                    edge.ItemType = HeldItemType;
                    continue;
                }

                double cap = edge.GetAvailableCapacity();
                double assign = Math.Min(idealShare, Math.Min(cap, remaining));
                edge.Rate = assign;
                edge.ItemType = HeldItemType;
                remaining -= assign;
            }
        }
    }


    public class MergerNode : Node
    {
        public Dictionary<ItemType, double> OutputRatesPerType { get; } = new();
        public MergerNode(string id) : base(id) { }

        public override void Compute()
        {
            OutputRatesPerType.Clear();

            double totalIn = TotalInputRate();
            if (Outputs.Count == 0 || totalIn <= 0)
            {
                HeldItemType = ItemType.None;

                foreach (var o in Outputs)
                {
                    o.Rate = 0;
                    o.ItemType = ItemType.None;
                }

                return;
            }

            // 1) Eingänge nach ItemType aufaddieren
            var inputByType = new Dictionary<ItemType, double>();
            foreach (var e in Inputs)
            {
                if (e.ItemType == ItemType.None || e.Rate <= 0) continue;

                if (!inputByType.TryGetValue(e.ItemType, out double current))
                    current = 0;

                inputByType[e.ItemType] = current + e.Rate;
            }

            // Wenn alle Inputs "None" waren, dann haben wir effektiv keinen sinnvollen Input
            if (inputByType.Count == 0)
            {
                HeldItemType = ItemType.None;
                foreach (var o in Outputs)
                {
                    o.Rate = 0;
                    o.ItemType = ItemType.None;
                }
                return;
            }

            // 2) Gesamtrate (über alle Typen) und Kapazität
            double totalRate = inputByType.Values.Sum();
            var edge = Outputs[0];
            double cap = edge.GetAvailableCapacity();
            double finalRate = Math.Min(totalRate, cap);

            // 3) Proportionale Verteilung auf ItemTypes, falls begrenzt
            double scale = (totalRate > 0) ? (finalRate / totalRate) : 0.0;

            foreach (var kv in inputByType)
            {
                ItemType type = kv.Key;
                double inRate = kv.Value;
                double outRate = inRate * scale;

                if (outRate <= 0) continue;

                OutputRatesPerType[type] = outRate;
            }

            // 4) Für Abwärtskompatibilität: "dominanter" Typ bleibt für HeldItemType / Edge.ItemType
            ItemType dominantType = ItemType.None;
            double bestRate = 0;

            foreach (var kv in OutputRatesPerType)
            {
                if (kv.Value > bestRate)
                {
                    bestRate = kv.Value;
                    dominantType = kv.Key;
                }
            }

            HeldItemType = (finalRate > 0) ? dominantType : ItemType.None;

            edge.Rate = finalRate;
            edge.ItemType = HeldItemType;
        }
    }

    public class OutputNode : Node
    {
        public double CollectedRate { get; private set; }

        public OutputNode(string id) : base(id) { }

        public override void Compute()
        {
            CollectedRate = TotalInputRate();
            HeldItemType = DominantInputItemType();
        }
    }

    public class RecipeMachineNode : Node
    {
        public double CraftTimeMs;              // Zeit pro Rezept (deine ticksPerProcess → CraftTimeMs)
        public int ParallelMachines;

        // Zutaten: ItemType -> benötigte Menge pro Rezept
        public Dictionary<ItemType, int> InputAmounts { get; }

        // Produkte: ItemType -> produzierte Menge pro Rezept
        public Dictionary<ItemType, int> OutputAmounts { get; }

        // Debug / Analyse:
        public Dictionary<ItemType, double> InputRatesPerType { get; } = new();
        public Dictionary<ItemType, double> OutputRatesPerType { get; } = new();
        public ItemType LimitingIngredient { get; private set; } = ItemType.None;

        // Aktuell: wir gehen von EINEM Haupt-Produkt-Typ aus (z.B. Bar)
        public ItemType PrimaryOutputType { get; }

        public RecipeMachineNode(
            string id,
            double craftTimeMs,
            Dictionary<ItemType, int> inputAmounts,
            Dictionary<ItemType, int> outputAmounts,
            int parallelMachines = 1
        ) : base(id)
        {
            CraftTimeMs = craftTimeMs;
            ParallelMachines = parallelMachines;
            InputAmounts = inputAmounts ?? throw new ArgumentNullException(nameof(inputAmounts));
            OutputAmounts = outputAmounts ?? throw new ArgumentNullException(nameof(outputAmounts));


            if (OutputAmounts.Count == 0)
                PrimaryOutputType = ItemType.None;
            else
                PrimaryOutputType = OutputAmounts.Keys.First(); // später: Multi-Output sauber verteilen
        }

        private double MaxRecipeExecutionsPerMinute()
        {
            if (CraftTimeMs <= 0) return double.MaxValue;
            return (60000.0 / CraftTimeMs) * ParallelMachines;
        }

        public override void Compute()
        {
            InputRatesPerType.Clear();
            OutputRatesPerType.Clear();
            LimitingIngredient = ItemType.None;
            HeldItemType = ItemType.None;

            if (Outputs.Count == 0) return;

            // 1) Eingangs-Raten nach ItemType aufaddieren
            foreach (var e in Inputs)
            {
                if (e.Rate <= 0) continue;

                // Spezieller Fall: MergerNode mit Multi-Type-Info
                if (e.From is MergerNode merger && merger.OutputRatesPerType.Count > 0)
                {
                    foreach (var kv in merger.OutputRatesPerType)
                    {
                        var type = kv.Key;
                        double rate = kv.Value;
                        if (type == ItemType.None || rate <= 0) continue;

                        if (!InputRatesPerType.TryGetValue(type, out double current))
                            current = 0;

                        InputRatesPerType[type] = current + rate;
                    }
                }
                else
                {
                    // Normalfall: Single-Type-Edge
                    if (e.ItemType == ItemType.None) continue;

                    if (!InputRatesPerType.TryGetValue(e.ItemType, out double current))
                        current = 0;

                    InputRatesPerType[e.ItemType] = current + e.Rate;
                }
            }

            if (InputAmounts.Count == 0)
            {
                // Kein Ingredient? Dann können wir nichts Sinnvolles berechnen.
                foreach (var edge in Outputs)
                {
                    edge.Rate = 0;
                    edge.ItemType = ItemType.None;
                }
                return;
            }

            // 2) Rezept-Rate bestimmen: min(inputRate[type] / amountRequired[type]) über alle benötigten Inputs
            double recipeRate = double.MaxValue;
            foreach (var req in InputAmounts)
            {
                var type = req.Key;
                int amountRequired = Math.Max(1, req.Value); // Schutz

                InputRatesPerType.TryGetValue(type, out double availableRate);

                // Wenn ein benötigter Typ gar nicht ankommt -> limitierende Rate = 0
                double possibleRate = availableRate / amountRequired;

                if (possibleRate < recipeRate)
                {
                    recipeRate = possibleRate;
                    LimitingIngredient = type;
                }
            }

            recipeRate = Math.Min(recipeRate, MaxRecipeExecutionsPerMinute());
            if (double.IsInfinity(recipeRate) || double.IsNaN(recipeRate))
                recipeRate = 0;

            // 3) Output-Raten pro ItemType berechnen
            foreach (var prod in OutputAmounts)
            {
                var type = prod.Key;
                int amountPerRecipe = prod.Value;
                double outRate = recipeRate * amountPerRecipe;
                OutputRatesPerType[type] = outRate;
            }

            // 4) Aktuell: wir haben physisch nur einen Output-Belt → wir nehmen PrimaryOutputType
            double primaryOutRate = 0;
            if (PrimaryOutputType != ItemType.None)
                OutputRatesPerType.TryGetValue(PrimaryOutputType, out primaryOutRate);

            var outEdge = Outputs[0];
            double cap = outEdge.GetAvailableCapacity();
            double finalRate = Math.Min(primaryOutRate, cap);

            outEdge.Rate = finalRate;
            outEdge.ItemType = finalRate > 0 ? PrimaryOutputType : ItemType.None;

            HeldItemType = outEdge.ItemType; // für Debug / Downstream-Nodes
        }
    }
    public class FactoryValidationResult
    {
        public bool IsAcyclic = true;
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();

        public bool IsValid => IsAcyclic && Errors.Count == 0;
    }

    public class FactoryGraph
    {
        public List<Node> Nodes { get; } = new();
        public List<Edge> Edges { get; } = new();
        public List<string> ValidationMessages { get; } = new();

        public T AddNode<T>(T node) where T : Node
        {
            Nodes.Add(node);
            return node;
        }

        public Edge AddEdge(Node from, Node to, double maxCapacity = 0)
        {
            var e = new Edge(from, to, maxCapacity);
            Edges.Add(e);
            from.Outputs.Add(e);
            to.Inputs.Add(e);
            return e;
        }

        public void SolveThroughput()
        {
            var ordered = TopologicalSort();
            foreach (var node in ordered)
            {
                node.Compute();
            }
        }

        private List<Node> TopologicalSort()
        {
            var inDegree = new Dictionary<Node, int>();
            foreach (var n in Nodes) inDegree[n] = 0;
            foreach (var e in Edges) inDegree[e.To]++;

            var queue = new Queue<Node>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            var result = new List<Node>();

            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                result.Add(n);

                foreach (var e in n.Outputs)
                {
                    inDegree[e.To]--;
                    if (inDegree[e.To] == 0)
                        queue.Enqueue(e.To);
                }
            }

            if (result.Count != Nodes.Count)
                throw new InvalidOperationException("Graph has cycles; simple topological sort failed.");

            return result;
        }

        public void ComputeTTFI()
        {
            var ordered = TopologicalSort();

            // Reset
            foreach (var node in Nodes)
            {
                node.TimeToFirstItemSeconds = double.PositiveInfinity;
            }

            foreach (var node in ordered)
            {
                switch (node)
                {
                    case InputNode input:
                        // Inputs: erstes Item steht "sofort" zur Verfügung
                        input.TimeToFirstItemSeconds = 0.0;
                        break;

                    case MachineNode mach:
                        {
                            double maxInputTTFI = 0.0;
                            foreach (var e in mach.Inputs)
                            {
                                if (e.From.TimeToFirstItemSeconds > maxInputTTFI)
                                    maxInputTTFI = e.From.TimeToFirstItemSeconds;
                            }

                            double craftTimeSeconds = mach.CraftTimeMs / 1000.0;
                            mach.TimeToFirstItemSeconds = maxInputTTFI + craftTimeSeconds;
                            break;
                        }

                    case RecipeMachineNode recipe:
                        {
                            double maxInputTTFI = 0.0;
                            foreach (var e in recipe.Inputs)
                            {
                                if (e.From.TimeToFirstItemSeconds > maxInputTTFI)
                                    maxInputTTFI = e.From.TimeToFirstItemSeconds;
                            }

                            double craftTimeSeconds = recipe.CraftTimeMs / 1000.0;
                            recipe.TimeToFirstItemSeconds = maxInputTTFI + craftTimeSeconds;
                            break;
                        }

                    case SplitterNode splitter:
                    case MergerNode merger:
                        {
                            // Pass-Through: erstes Item, sobald einer der Inputs das erste Item hat
                            double minInputTTFI = double.PositiveInfinity;
                            foreach (var e in node.Inputs)
                            {
                                if (e.From.TimeToFirstItemSeconds < minInputTTFI)
                                    minInputTTFI = e.From.TimeToFirstItemSeconds;
                            }
                            node.TimeToFirstItemSeconds = minInputTTFI;
                            break;
                        }

                    case OutputNode output:
                        {
                            double minInputTTFI = double.PositiveInfinity;
                            foreach (var e in output.Inputs)
                            {
                                if (e.From.TimeToFirstItemSeconds < minInputTTFI)
                                    minInputTTFI = e.From.TimeToFirstItemSeconds;
                            }
                            output.TimeToFirstItemSeconds = minInputTTFI;
                            break;
                        }
                }
            }
        }


        public FactoryValidationResult Validate()
        {
            var result = new FactoryValidationResult();

            // 1) Azyklisch? (TopologicalSort wirft sonst Exception)
            try
            {
                TopologicalSort();
            }
            catch (Exception ex)
            {
                result.IsAcyclic = false;
                result.Errors.Add($"Graph has cycles: {ex.Message}");
                // wenn zyklen existieren, weitere Checks können verwirrend sein, aber wir machen trotzdem weiter
            }

            // 2) Welche ItemTypes werden benötigt?
            var requiredTypes = new HashSet<ItemType>();
            var producibleTypes = new HashSet<ItemType>();

            foreach (var node in Nodes)
            {
                switch (node)
                {
                    case InputNode input:
                        producibleTypes.Add(input.SourceItemType);
                        break;

                    case MachineNode mach:
                        if (mach.InputItemType != ItemType.None)
                            requiredTypes.Add(mach.InputItemType);
                        if (mach.OutputItemType != ItemType.None)
                            producibleTypes.Add(mach.OutputItemType);
                        break;

                    case RecipeMachineNode recipeNode:
                        foreach (var t in recipeNode.InputAmounts.Keys)
                            requiredTypes.Add(t);
                        foreach (var t in recipeNode.OutputAmounts.Keys)
                            producibleTypes.Add(t);
                        break;
                }
            }

            foreach (var t in requiredTypes)
            {
                if (!producibleTypes.Contains(t))
                {
                    result.Errors.Add($"Missing ingredient: {t} is required by at least one machine/recipe but never produced or provided by an InputNode.");
                }
            }

            // 3) OutputNodes ohne Zufluss
            foreach (var node in Nodes.OfType<OutputNode>())
            {
                if (node.Inputs.Count == 0)
                {
                    result.Warnings.Add($"OutputNode '{node.Id}' has no inputs (no edges).");
                }
            }

            return result;
        }
    }
}
