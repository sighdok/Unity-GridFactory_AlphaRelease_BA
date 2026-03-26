using System.Collections.Generic;

using UnityEngine;

using GridFactory.Inventory;
using System.Runtime.InteropServices.WindowsRuntime;

public class GoldRateTrackingManager : MonoBehaviour
{
    public static GoldRateTrackingManager Instance { get; private set; }

    private static InventoryManager IM => InventoryManager.Instance;

    [SerializeField] private float windowSeconds = 60f;

    private readonly Queue<(float t, int gold)> _eventsQ = new();
    private int _sumInWindow;
    private float _goldPerSeconds;

    public float GoldPerMinute => _goldPerSeconds * 60f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        IM.OnGoldAdded += OnGoldAdded;
    }

    private void OnDisable()
    {
        IM.OnGoldAdded -= OnGoldAdded;
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        Prune(now);

        GoldPerSecond = (windowSeconds > 0f) ? (_sumInWindow / windowSeconds) : 0f;
    }

    public float GoldPerSecond
    {
        get => _goldPerSeconds;
        set
        {
            _goldPerSeconds = value;
        }
    }

    public int SumInWindow
    {
        get => _sumInWindow;
        set
        {
            _sumInWindow = value;
        }
    }

    public void SetGoldPerSecondDirect(float gps)
    {
        GoldPerSecond = gps;
    }

    private void OnGoldAdded(int amount)
    {
        float now = Time.unscaledTime;
        _eventsQ.Enqueue((now, amount));
        _sumInWindow += amount;

        Prune(now);
    }

    private void Prune(float now)
    {
        while (_eventsQ.Count > 0 && now - _eventsQ.Peek().t > windowSeconds)
        {
            var e = _eventsQ.Dequeue();
            _sumInWindow -= e.gold;
        }
    }
}
