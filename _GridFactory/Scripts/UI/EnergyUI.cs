using System.Text;
using UnityEngine;
using UnityEngine.UI;
using GridFactory.Inventory;
using GridFactory.Core;

namespace GridFactory.UI
{
    public class EnergyUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnergyManager energyManager;

        [Header("Slider (Battery)")]
        [SerializeField] private Slider energySlider;
        [SerializeField] private bool sliderShowsPercent = true; // true => 0..1 ; false => 0..MaxEnergy

        [Header("5 Text Fields (TMP)")]
        [SerializeField] private TMPro.TextMeshProUGUI nettoButtonTMP;  // Battery + optional raw
        [SerializeField] private TMPro.TextMeshProUGUI energyTMP;  // Battery + optional raw
        [SerializeField] private TMPro.TextMeshProUGUI flowTMP;    // Prod/s, Cons/s, Net/s
        [SerializeField] private TMPro.TextMeshProUGUI ratioTMP;   // Power ratio
        [SerializeField] private TMPro.TextMeshProUGUI statusTMP;  // Guidance
        [SerializeField] private TMPro.TextMeshProUGUI debugTMP;   // Debug panel (optional)

        [Header("Mode")]
        [SerializeField, Tooltip("Shows debug panel (tick values + top consumers).")]
        private bool showDebugPanel = false;

        [SerializeField, Tooltip("If true, debug panel focuses on per-tick numbers. Otherwise still shows some tick + per-second mix.")]
        private bool debugTickMode = true;

        [SerializeField, Tooltip("Hotkey to toggle debug panel at runtime.")]
        private bool enableHotkeyToggle = true;

        [SerializeField] private KeyCode debugToggleKey = KeyCode.F3;

        [Header("Formatting")]
        [SerializeField] private int decimals = 2;
        [SerializeField] private int topConsumerCount = 5;

        [Header("UI Refresh (anti-flicker)")]
        [SerializeField, Tooltip("UI refresh rate independent of OnEnergyChanged.")]
        private float uiRefreshInterval = 0.10f;

        private float _uiTimer;
        private bool _dirty;

        private void Awake()
        {
            if (energyManager == null)
                energyManager = EnergyManager.Instance;
        }

        private void OnEnable()
        {
            if (energyManager == null)
                energyManager = EnergyManager.Instance;

            if (energyManager != null)
                energyManager.OnEnergyChanged += MarkDirty;

            ApplyDebugVisibility();
            _dirty = true;
            Refresh();
        }

        private void OnDisable()
        {
            if (energyManager != null)
                energyManager.OnEnergyChanged -= MarkDirty;
        }

        private void MarkDirty() => _dirty = true;

        private void Update()
        {
            if (enableHotkeyToggle && Input.GetKeyDown(debugToggleKey))
            {
                showDebugPanel = !showDebugPanel;
                ApplyDebugVisibility();
                _dirty = true;
            }

            _uiTimer += Time.deltaTime;
            if (_uiTimer >= uiRefreshInterval)
            {
                _uiTimer = 0f;
                if (_dirty)
                {
                    _dirty = false;
                    Refresh();
                }
            }
        }

        private void ApplyDebugVisibility()
        {
            if (debugTMP != null)
                debugTMP.gameObject.SetActive(showDebugPanel);
        }

        private float TickIntervalSafe()
        {
            if (TickManager.Instance != null)
                return Mathf.Max(0.0001f, TickManager.Instance.TickInterval);
            return 0.25f;
        }

        private void Refresh()
        {
            if (energyManager == null)
            {
                SetAllText("No EnergyManager");
                UpdateSlider(0f, 1f);
                return;
            }

            float tickInterval = TickIntervalSafe();

            // --- Battery ---
            float e = energyManager.EnergyFloat;
            float eMax = Mathf.Max(0.0001f, energyManager.MaxEnergy);
            float battery01 = Mathf.Clamp01(e / eMax);
            float batteryPct = battery01 * 100f;

            // --- Power ratio ---
            float ratio = Mathf.Clamp01(energyManager.PowerRatio);

            // --- Tick values (debug/core) ---
            float genPerTick = energyManager.LastGeneratedPerTick;
            float demandPerTick = energyManager.LastDemandPerTick;
            float consumedPerTick = energyManager.LastConsumedPerTick;

            // --- Player-facing per second ---
            float genPerSec = energyManager.LastGeneratedPerTick / tickInterval;   // stable
            float demandPerSec = demandPerTick / tickInterval;
            float consumedPerSec = consumedPerTick / tickInterval;
            float netPerSec = genPerSec - consumedPerSec;

            // --- 1) ENERGY TEXT ---
            // Player: percent. Optional raw in small text.
            var sbEnergy = new StringBuilder(96);
            sbEnergy.AppendLine($"<b>Battery:</b> {batteryPct:0}%");
            // sbEnergy.AppendLine($"{batteryPct:0}%");
            //sbEnergy.AppendLine($"<alpha=#AA><size=80%>{e:0.###}/{eMax:0.###} EU</size></alpha>");
            SetText(energyTMP, sbEnergy.ToString().TrimEnd());

            // --- 2) FLOW TEXT ---
            var sbFlow = new StringBuilder(160);
            sbFlow.AppendLine($"<b>Input/Sec:</b> {(netPerSec >= 0 ? "+" : "")}{netPerSec.ToString($"F{decimals}")}");
            //sbFlow.AppendLine($"Prod/s: {genPerSec.ToString($"F{decimals}")}");
            //sbFlow.AppendLine($"Verbrauch/s: {consumedPerSec.ToString($"F{decimals}")}");
            //sbFlow.AppendLine($"{(netPerSec >= 0 ? "+" : "")}{netPerSec.ToString($"F{decimals}")}");
            SetText(flowTMP, sbFlow.ToString().TrimEnd());

            var sbButton = new StringBuilder(160);
            var shortNettoS = "";
            if (Mathf.Round(netPerSec * 100) / 100.0 > 0)
            {
                shortNettoS += "<color=#467B52>";
            }
            else if (Mathf.Round(netPerSec * 100) / 100.0 < 0)
            {
                shortNettoS += "<color=#934E4B>";

            }
            else
            {
                shortNettoS += "<color=#8D934B>";
            }
            shortNettoS += netPerSec.ToString($"F{decimals}");
            shortNettoS += "</color>";
            sbButton.AppendLine(shortNettoS);
            SetText(nettoButtonTMP, sbButton.ToString().TrimEnd());

            // --- 3) RATIO TEXT ---
            var sbRatio = new StringBuilder(128);
            sbRatio.AppendLine($"<b>Ratio:</b> {(ratio * 100f):0}%");

            // sbRatio.AppendLine($"{(ratio * 100f):0}%");
            if (energyManager.LastDemandPerTick <= 0.0001f)
                sbRatio.AppendLine("<color=#000000><size=80%><indent=5%>No Consumer</indent></size></color>");
            else if (ratio >= 0.999f)
                sbRatio.AppendLine("<color=#467B52><size=80%><indent=5%>Standard</indent></size></color>");
            else if (ratio <= 0.001f)
                sbRatio.AppendLine("<color=#467B52><size=80%><indent=5%>Stopped</indent></size></color>");
            else
                sbRatio.AppendLine("<color=#934E4B><size=80%><indent=5%>Brownout</indent></size></color>");

            SetText(ratioTMP, sbRatio.ToString().TrimEnd());

            // --- 4) STATUS TEXT (what to do) ---
            SetText(statusTMP, BuildStatusLine(ratio, batteryPct, netPerSec));


            // --- 5) DEBUG TEXT (optional panel) ---
            if (showDebugPanel)
                SetText(debugTMP, BuildDebugText(tickInterval, genPerTick, demandPerTick, consumedPerTick, genPerSec, demandPerSec, consumedPerSec, netPerSec));

            // --- Slider ---
            UpdateSlider(e, eMax);
        }

        private void UpdateSlider(float energy, float maxEnergy)
        {

            if (energySlider == null) return;

            if (sliderShowsPercent)
            {
                energySlider.minValue = 0f;
                energySlider.maxValue = 1f;
                energySlider.value = Mathf.Clamp01(energy / Mathf.Max(0.0001f, maxEnergy));
            }
            else
            {
                energySlider.minValue = 0f;
                energySlider.maxValue = Mathf.Max(0.0001f, maxEnergy);
                energySlider.value = Mathf.Clamp(energy, 0f, maxEnergy);
            }
        }

        private string BuildStatusLine(float powerRatio, float batteryPct, float netPerSec)
        {
            /*
                // Player guidance, not dev jargon
                if (energyManager.LastDemandPerTick <= 0.0001f)
                    return "Keine Verbraucher aktiv.";

                if (powerRatio >= 0.999f)
                {
                    if (netPerSec >= 0f) return "Strom stabil. Batterie lädt.";
                    return "Strom stabil, aber Verbrauch > Produktion.\nTipp: Mehr Brennstoff / mehr Kraftwerke.";
                }

                if (powerRatio <= 0.001f)
                    return "Kein Strom: Maschinen stehen.\nTipp: Brennstoff ins Kraftwerk liefern.";

                // Brownout
                if (batteryPct < 10f && netPerSec < 0f)
                    return $"Strom knapp: {(powerRatio * 100f):0}% Leistung.\nTipp: Produktion erhöhen oder Verbraucher reduzieren.";

                return $"Strom knapp: {(powerRatio * 100f):0}% Leistung.\nMaschinen laufen langsamer.";
                */
            return "";
        }

        private string BuildDebugText(
            float tickInterval,
            float genPerTick,
            float demandPerTick,
            float consumedPerTick,
            float genPerSec,
            float demandPerSec,
            float consumedPerSec,
            float netPerSec)
        {
            var sb = new StringBuilder(700);

            sb.AppendLine("<b>Debug</b>");
            sb.AppendLine($"TickInterval: {tickInterval:0.###}s");

            if (debugTickMode)
            {
                float netPerTick = genPerTick - consumedPerTick;
                sb.AppendLine($"Gen/T: {genPerTick:0.###}");
                sb.AppendLine($"Demand/T: {demandPerTick:0.###}");
                sb.AppendLine($"Consumed/T: {consumedPerTick:0.###}");
                sb.AppendLine($"Netto/T: {(netPerTick >= 0 ? "+" : "")}{netPerTick:0.###}");
                sb.AppendLine($"AvailForDemand/T: {energyManager.LastAvailableForDemand:0.###}");
                sb.AppendLine($"EnergyStart/T: {energyManager.LastEnergyAtTickStart:0.###}");
                sb.AppendLine($"AvgGen/T: {energyManager.AvgGeneratedPerTick:0.###}");
            }
            else
            {
                sb.AppendLine($"Prod/s: {genPerSec:0.###}");
                sb.AppendLine($"Bedarf/s: {demandPerSec:0.###}");
                sb.AppendLine($"Verbrauch/s: {consumedPerSec:0.###}");
                sb.AppendLine($"Netto/s: {(netPerSec >= 0 ? "+" : "")}{netPerSec:0.###}");
                sb.AppendLine($"Gen/T (last): {genPerTick:0.###}");
                sb.AppendLine($"AvgGen/T: {energyManager.AvgGeneratedPerTick:0.###}");
            }

            sb.AppendLine();
            sb.Append(BuildTopConsumersText());

            return sb.ToString().TrimEnd();
        }

        private string BuildTopConsumersText()
        {
            var top = energyManager.TopConsumersLastTick;
            if (top == null || top.Count == 0)
                return "Top-Verbraucher:\n(keine)";

            int n = Mathf.Min(topConsumerCount, top.Count);
            var sb = new StringBuilder(256);
            sb.AppendLine("Top-Verbraucher:");
            for (int i = 0; i < n; i++)
            {
                sb.Append(i + 1);
                sb.Append(". ");
                sb.Append(top[i].name);
                sb.Append(" — ");
                sb.Append(top[i].demandPerTick.ToString($"F{decimals}"));
                sb.AppendLine("/T");
            }
            return sb.ToString().TrimEnd();
        }

        private void SetText(TMPro.TextMeshProUGUI t, string s)
        {
            if (t != null) t.text = s;
        }

        private void SetAllText(string message)
        {
            SetText(energyTMP, message);
            SetText(flowTMP, message);
            SetText(ratioTMP, message);
            SetText(statusTMP, message);
            SetText(debugTMP, message);
        }
    }
}
