using UnityEngine;
using TMPro;
using GridFactory.Inventory;

public class GoldWidgetUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text gpsText;
    [SerializeField] private GoldRateTrackingManager tracker;
    [SerializeField] private InventoryManager inventoryManager;
    [Header("Format")]
    //[SerializeField] private string goldPrefix = "Gold: ";
    [SerializeField] private string gpsPrefix = "Gold/Min Ø:";
    [SerializeField] private int gpsDecimals = 1;

    [Header("UI Refresh (anti-flicker)")]
    [SerializeField, Tooltip("UI refresh rate independent of OnEnergyChanged.")]
    private float uiRefreshInterval = 0.10f;

    private float _uiTimer;
    private bool _dirty;

    private void Reset()
    {
        tracker = FindFirstObjectByType<GoldRateTrackingManager>();
        //goldText = GetComponentInChildren<TMP_Text>();
    }
    private void MarkDirty(int value) => _dirty = true;
    void OnEnable()
    {
        _dirty = true;
        inventoryManager.OnGoldAdded += MarkDirty;
    }
    void OnDisable()
    {
        _dirty = false;
        inventoryManager.OnGoldAdded -= MarkDirty;
    }


    private void Update()
    {


        //if (goldText) goldText.text = $"{goldPrefix}{inv.Gold}";
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

    private void Refresh()
    {
        if (gpsText && tracker)
            gpsText.text = $"{gpsPrefix}\n{tracker.GoldPerMinute.ToString($"F{gpsDecimals}")}";
    }
}
