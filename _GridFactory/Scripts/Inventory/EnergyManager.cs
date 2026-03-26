using System;
using System.Collections.Generic;

using UnityEngine;

using GridFactory.Core;

namespace GridFactory.Inventory
{
    [Serializable]
    public struct ConsumerSnapshot
    {
        public string name;
        public float demandPerTick;

        public ConsumerSnapshot(string name, float demandPerTick)
        {
            this.name = name;
            this.demandPerTick = demandPerTick;
        }
    }
    public enum EnergyStatus
    {
        NoConsumers,
        Ok,
        Brownout,
        NoEnergy
    }

    public class EnergyManager : MonoBehaviour
    {
        public static EnergyManager Instance { get; private set; }

        private static TickManager TM => TickManager.Instance;

        [Header("Battery")]
        [SerializeField] private float maxEnergy = 100f;

        [Header("Brownout")]
        [SerializeField, Range(0f, 0.5f)] private float minPowerRatio = 0.05f;
        [SerializeField] private float maxChargePerTick = 5;
        [SerializeField] private float maxDischargePerTick = 1f;

        [Header("Baseline Power")]
        [SerializeField] private float freeSupplyPerTick = 0.1f;

        [Header("Energy")]
        public float upkeepBase = 0.025f;
        public float upkeepPerMachine = 0.10f;
        public float upkeepPerConveyor = 0.01f;
        public float upkeepPerPort = 0.025f;

        [Header("Generation Smoothing")]
        [SerializeField] private int generationAverageWindowTicks = 60;

        [Header("Smoothing")]
        [SerializeField, Range(0.01f, 1f)] private float powerRatioLerp = 0.2f;

        private readonly List<IEnergyConsumer> _consumers = new();
        private readonly List<ConsumerSnapshot> _topConsumersLastTick = new();

        private float _energyFloat = 0f;
        private int _genRingIndex = 0;
        private int _genRingCount = 0;
        private float[] _genRing;
        private float _genRingSum = 0f;
        private float _powerRatioSmoothed = 1f;
        private float _generatedThisTick = 0f;

        public IReadOnlyList<ConsumerSnapshot> TopConsumersLastTick => _topConsumersLastTick;
        public float AvgGeneratedPerTick { get; private set; } = 0f;
        public float AvgGeneratedPerSecond { get; private set; } = 0f;
        public float MaxEnergy => maxEnergy;
        public int Energy => Mathf.FloorToInt(_energyFloat);
        public float EnergyFloat => _energyFloat;
        public float PowerRatio { get; private set; } = 1f;
        public float LastDemandPerTick { get; private set; }
        public float LastConsumedPerTick { get; private set; }
        public float LastGeneratedPerTick { get; private set; }
        public float LastEnergyAtTickStart { get; private set; }
        public float LastAvailableForDemand { get; private set; }

        public event Action OnEnergyChanged;

        public EnergyStatus CurrentStatus
        {
            get
            {
                if (LastDemandPerTick <= 0.0001f) return EnergyStatus.NoConsumers;
                if (PowerRatio >= 0.999f) return EnergyStatus.Ok;
                if (PowerRatio <= 0.0001f) return EnergyStatus.NoEnergy;
                return EnergyStatus.Brownout;
            }
        }

        public string StatusMessage
        {
            get
            {
                switch (CurrentStatus)
                {
                    case EnergyStatus.NoConsumers:
                        return "Keine Verbraucher.";
                    case EnergyStatus.Ok:
                        return "OK";
                    case EnergyStatus.Brownout:
                        return "BROWNOUT";
                    case EnergyStatus.NoEnergy:
                        return "KEINE ENERGIE";
                    default:
                        return "";
                }
            }
        }

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
            int n = Mathf.Max(1, generationAverageWindowTicks);
            _genRing = new float[n];
        }

        private void OnEnable()
        {
            TM.OnTickEnergy += StepTick;
        }

        private void OnDisable()
        {
            TM.OnTickEnergy -= StepTick;
        }

        private void StepTick()
        {
            // -----------------------------------------
            // 0) Finalize generation from last tick window
            //    - includes baseline
            //    - this is the "live supply" available for THIS tick
            // -----------------------------------------
            float baseline = Mathf.Max(0f, freeSupplyPerTick);
            float generatedSinceLastTick = Mathf.Max(0f, _generatedThisTick);

            // "Live supply" for this tick (counts towards demand first)
            float liveSupplyThisTick = generatedSinceLastTick + baseline;

            LastGeneratedPerTick = liveSupplyThisTick;
            PushGenerationSample(LastGeneratedPerTick);

            // Reset the generation window for the next tick
            _generatedThisTick = 0f;

            // Snapshot battery at tick start
            LastEnergyAtTickStart = _energyFloat;

            // -----------------------------------------
            // 1) Collect demand + snapshots
            // -----------------------------------------
            float demand = 0f;
            List<ConsumerSnapshot> snapshots = null;

            for (int i = _consumers.Count - 1; i >= 0; i--)
            {
                var c = _consumers[i];
                if (c == null)
                {
                    _consumers.RemoveAt(i);
                    continue;
                }

                float d = Mathf.Max(0f, c.DemandPerTick);
                demand += d;

                if (d > 0f)
                {
                    snapshots ??= new List<ConsumerSnapshot>(8);
                    string n = (c as MonoBehaviour) != null ? ((MonoBehaviour)c).name : c.GetType().Name;
                    snapshots.Add(new ConsumerSnapshot(n, d));
                }
            }

            LastDemandPerTick = demand;

            // -----------------------------------------
            // 2) No consumers => charge battery with all generation, ratio 1
            // -----------------------------------------
            if (demand <= 0.0001f)
            {
                // Everything is surplus -> battery charge
                float chargeCap = Mathf.Max(0f, maxChargePerTick);
                float charge = Mathf.Min(liveSupplyThisTick, chargeCap);
                if (charge > 0f)
                    _energyFloat = Mathf.Clamp(_energyFloat + charge, 0f, maxEnergy);

                LastConsumedPerTick = 0f;
                LastAvailableForDemand = 0f;

                // Smooth stays at 1
                _powerRatioSmoothed = Mathf.Lerp(_powerRatioSmoothed, 1f, powerRatioLerp);
                PowerRatio = _powerRatioSmoothed;

                UpdateTopConsumers(snapshots);
                OnEnergyChanged?.Invoke();
                return;
            }

            // -----------------------------------------
            // 3) Supply first, then battery buffers deficit (limited per tick)
            // -----------------------------------------
            float supplyToDemand = Mathf.Min(liveSupplyThisTick, demand);
            float remainingDemand = demand - supplyToDemand;

            float dischargeLimit = Mathf.Max(0f, maxDischargePerTick);
            float batteryCanProvide = Mathf.Min(Mathf.Max(0f, _energyFloat), dischargeLimit);

            float batteryToDemand = Mathf.Min(remainingDemand, batteryCanProvide);

            // For UI: how much was actually available to meet demand this tick
            float availableForDemand = supplyToDemand + batteryToDemand;
            LastAvailableForDemand = availableForDemand;

            // Consume battery part
            if (batteryToDemand > 0f)
                _energyFloat = Mathf.Clamp(_energyFloat - batteryToDemand, 0f, maxEnergy);

            // Any surplus generation after feeding demand charges battery
            float surplus = liveSupplyThisTick - supplyToDemand; // >= 0
            if (surplus > 0f)
            {
                float chargeCap = Mathf.Max(0f, maxChargePerTick);
                float charge = Mathf.Min(surplus, chargeCap);
                if (charge > 0f)
                    _energyFloat = Mathf.Clamp(_energyFloat + charge, 0f, maxEnergy);
            }

            // -----------------------------------------
            // 4) Compute target ratio and apply brownout floor + smoothing
            // -----------------------------------------
            float targetRatio = Mathf.Clamp01(availableForDemand / demand);

            // If we have ANY power at all, enforce minimal "Notbetrieb" (optional feel)
            if (availableForDemand > 0.0001f)
                targetRatio = Mathf.Max(targetRatio, minPowerRatio);

            // Smooth ratio to avoid "sprunghaft"
            _powerRatioSmoothed = Mathf.Lerp(_powerRatioSmoothed, targetRatio, Mathf.Clamp01(powerRatioLerp));
            PowerRatio = _powerRatioSmoothed;

            // Effective consumed energy from the "grid" point of view (for UI/debug)
            // This is what machines effectively received.
            LastConsumedPerTick = demand * PowerRatio;

            UpdateTopConsumers(snapshots);
            OnEnergyChanged?.Invoke();
        }


        private void PushGenerationSample(float sample)
        {
            if (_genRing == null || _genRing.Length == 0)
                return;

            if (_genRingCount == _genRing.Length)
                _genRingSum -= _genRing[_genRingIndex];
            else
                _genRingCount++;

            _genRing[_genRingIndex] = sample;
            _genRingSum += sample;
            _genRingIndex++;

            if (_genRingIndex >= _genRing.Length)
                _genRingIndex = 0;

            AvgGeneratedPerTick = _genRingSum / Mathf.Max(1, _genRingCount);

            float tickInterval = Mathf.Max(0.0001f, TM.TickInterval);

            AvgGeneratedPerSecond = AvgGeneratedPerTick / tickInterval;
        }

        private void UpdateTopConsumers(List<ConsumerSnapshot> snapshots)
        {
            _topConsumersLastTick.Clear();

            if (snapshots == null || snapshots.Count == 0)
                return;

            snapshots.Sort((a, b) => b.demandPerTick.CompareTo(a.demandPerTick));

            const int TOP_N = 5;
            int n = Mathf.Min(TOP_N, snapshots.Count);
            for (int i = 0; i < n; i++)
                _topConsumersLastTick.Add(snapshots[i]);
        }

        public void RegisterConsumer(IEnergyConsumer consumer)
        {

            if (consumer == null)
                return;
            if (!_consumers.Contains(consumer))
                _consumers.Add(consumer);

            OnEnergyChanged?.Invoke();
        }

        public void UnregisterConsumer(IEnergyConsumer consumer)
        {
            if (consumer == null)
                return;
            _consumers.Remove(consumer);

            OnEnergyChanged?.Invoke();
        }

        public void ClearConsumersOnLoad()
        {
            _consumers.Clear();
        }
        public void AddEnergyFloat(float amount)
        {
            if (amount <= 0f) return;
            _generatedThisTick += amount;

            OnEnergyChanged?.Invoke();
        }

        public void LoadEnergyData(float eFloat)
        {
            _energyFloat = Mathf.Clamp(eFloat, 0f, maxEnergy);
            OnEnergyChanged?.Invoke();
        }
    }
}
