using System;
using System.Collections.Generic;
using System.Linq;

using GridFactory.Analysis;
using GridFactory.Grid;

namespace GridFactory.Core
{

    public static class BlueprintMetaSolver
    {
        /// <summary>
        /// Berechnet den Output eines Blueprints in Items/Minute,
        /// wenn von außen MetaInput-Raten pro ItemType vorgegeben werden.
        ///
        /// metaInputRates[type] = Items/Minute, die ins Blueprint fließen sollen.
        /// Rückgabe: Items/Minute pro ItemType, die über OutputPorts wieder herauskommen.
        /// </summary>
        public static Dictionary<ItemType, double> SolveBlueprintThroughput(
            GridManager grid,
            Dictionary<ItemType, double> metaInputRates)
        {
            // 1) Graph für Meta-Level bauen
            var graph = BlueprintThroughputBuilder.BuildForMeta(
                grid,
                out var inputNodesByType,
                out var outputNodes);

            // 2) InputNodes gemäß Meta-Raten setzen
            if (metaInputRates != null)
            {
                foreach (var kv in metaInputRates)
                {
                    var type = kv.Key;
                    double desiredRate = kv.Value;

                    if (!inputNodesByType.TryGetValue(type, out var nodes))
                        continue; // Blueprint hat keine Inputs dieses Typs

                    // Simple Strategie: gleichmäßig auf alle Ports gleichen Typs verteilen
                    double perNode = desiredRate / nodes.Count;

                    foreach (var n in nodes)
                    {
                        n.SourceRate = perNode;
                    }
                }
            }

            // 3) Graph lösen
            graph.SolveThroughput();

            // 4) Outputs pro ItemType aggregieren
            var result = new Dictionary<ItemType, double>();

            foreach (var outNode in outputNodes)
            {
                var type = outNode.HeldItemType;
                if (type == ItemType.None) continue;

                double rate = outNode.CollectedRate;
                if (rate <= 0) continue;

                if (!result.TryGetValue(type, out double current))
                    current = 0;

                result[type] = current + rate;
            }

            return result;
        }
    }
    public class MetaEdge
    {
        public MetaNode From { get; }
        public MetaNode To { get; }
        public double Rate;             // berechnete Items/Minute
        public double MaxCapacity;      // 0 oder weniger = unendlich

        public ItemType ItemType { get; set; } = ItemType.None;

        public MetaEdge(MetaNode from, MetaNode to, double maxCapacity = 0)
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

    public abstract class MetaNode
    {
        public string Id { get; }
        public List<MetaEdge> Inputs { get; } = new();
        public List<MetaEdge> Outputs { get; } = new();
        public ItemType HeldItemType { get; protected set; } = ItemType.None;

        protected MetaNode(string id)
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

    public class MetaSourceNode : MetaNode
    {
        public double SourceRate; // Items/Minute

        public ItemType SourceItemType { get; }

        public MetaSourceNode(string id, double sourceRate, ItemType itemType) : base(id)
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

    public class MetaBlueprintNode : MetaNode
    {
        private readonly GridManager _grid;

        /// <summary>
        /// Letztes berechnetes Resultat des Blueprints pro ItemType (Items/Minute).
        /// </summary>
        public Dictionary<ItemType, double> LastOutputRates { get; } = new();

        public MetaBlueprintNode(string id, GridManager grid)
            : base(id)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        public override void Compute()
        {
            LastOutputRates.Clear();

            // 1) Input-Raten pro ItemType aus MetaEdges einsammeln
            var metaInputRates = new Dictionary<ItemType, double>();

            foreach (var e in Inputs)
            {
                if (e.ItemType == ItemType.None || e.Rate <= 0)
                    continue;

                if (!metaInputRates.TryGetValue(e.ItemType, out double current))
                    current = 0;

                metaInputRates[e.ItemType] = current + e.Rate;
            }

            // Wenn keine Inputs -> Blueprint produziert ggf. nichts
            if (metaInputRates.Count == 0)
            {
                // Alle Outputs auf 0 setzen
                foreach (var edge in Outputs)
                {
                    edge.Rate = 0;
                    // ItemType auf dem Edge bleibt, wie er für das Meta-Design gedacht ist
                }
                HeldItemType = ItemType.None;
                return;
            }

            // 2) Blueprint-Throughput mit diesen MetaInputs berechnen
            var outputRates = BlueprintMetaSolver.SolveBlueprintThroughput(_grid, metaInputRates);

            foreach (var kv in outputRates)
            {
                LastOutputRates[kv.Key] = kv.Value;
            }

            // 3) Ausgabe-Raten auf Meta-Edges verteilen
            // Annahme: Jede ausgehende MetaEdge ist für einen festen ItemType "designed" (edge.ItemType).
            // Wir verteilen die OutputRate[type] gleichmäßig auf alle passenden Edges und berücksichtigen Capacity.

            // Start: alle Output-Edges auf 0 setzen
            foreach (var edge in Outputs)
            {
                edge.Rate = 0;
            }

            foreach (var kv in outputRates)
            {
                ItemType type = kv.Key;
                double totalRate = kv.Value;
                if (totalRate <= 0) continue;

                // Alle Edges, die diesen Typ transportieren sollen
                var edgesForType = Outputs.Where(e => e.ItemType == type).ToList();
                if (edgesForType.Count == 0)
                {
                    // Kein Meta-Abnehmer für diesen ItemType -> ignorieren
                    continue;
                }

                double remaining = totalRate;
                double idealShare = totalRate / edgesForType.Count;

                foreach (var edge in edgesForType)
                {
                    if (remaining <= 0)
                    {
                        edge.Rate = 0;
                        continue;
                    }

                    double cap = edge.GetAvailableCapacity();
                    double assign = Math.Min(idealShare, Math.Min(cap, remaining));

                    edge.Rate = assign;
                    // ItemType bleibt = type (sollte bereits so gesetzt sein)
                    remaining -= assign;
                }
            }

            // 4) Für Debug: HeldItemType = Typ mit größter Output-Rate (oder None)
            ItemType dominant = ItemType.None;
            double best = 0.0;

            foreach (var kv in LastOutputRates)
            {
                if (kv.Value > best)
                {
                    best = kv.Value;
                    dominant = kv.Key;
                }
            }

            HeldItemType = dominant;
        }
    }
    public class MetaSinkNode : MetaNode
    {
        public double CollectedRate { get; private set; }

        public MetaSinkNode(string id) : base(id) { }

        public override void Compute()
        {
            CollectedRate = TotalInputRate();
            HeldItemType = DominantInputItemType();
        }
    }


    public class MetaFactoryGraph
    {
        public List<MetaNode> Nodes { get; } = new();
        public List<MetaEdge> Edges { get; } = new();

        public T AddNode<T>(T node) where T : MetaNode
        {
            Nodes.Add(node);
            return node;
        }

        public MetaEdge AddEdge(MetaNode from, MetaNode to, double maxCapacity = 0)
        {
            var e = new MetaEdge(from, to, maxCapacity);
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

        private List<MetaNode> TopologicalSort()
        {
            var inDegree = new Dictionary<MetaNode, int>();
            foreach (var n in Nodes) inDegree[n] = 0;
            foreach (var e in Edges) inDegree[e.To]++;

            var queue = new Queue<MetaNode>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            var result = new List<MetaNode>();

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
    }
}
