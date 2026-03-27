using UnityEngine;

using TMPro;

using GridFactory.Inventory;

public class GoldWidgetUI : MonoBehaviour
{
    private static InventoryManager IM => InventoryManager.Instance;
    private static GoldRateTrackingManager GRTM => GoldRateTrackingManager.Instance;

    [Header("Refs")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text gpsText;

    [Header("Format")]
    [SerializeField] private string gpsPrefix = "Gold/Min Ø:";
    [SerializeField] private int gpsDecimals = 1;

    [Header("UI Refresh ")]
    [SerializeField] private float uiRefreshInterval = 0.10f;

    private float _uiTimer;
    private bool _dirty;

    private void MarkDirty(int value) => _dirty = true;

    void Start()
    {
        _dirty = true;
        IM.OnGoldAdded += MarkDirty;
    }

    private void Update()
    {
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
        gpsText.SetText($"{gpsPrefix}\n{GRTM.GoldPerMinute.ToString($"F{gpsDecimals}")}");
    }
}
