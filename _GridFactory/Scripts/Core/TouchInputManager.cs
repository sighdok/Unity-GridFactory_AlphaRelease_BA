using UnityEngine;

public class TouchInputManager : MonoBehaviour
{
    public static TouchInputManager Instance { get; private set; }

    [Header("Double Tap Settings")]
    [SerializeField] private float doubleTapTime = 0.3f;

    [Header("Hit Detection")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask cellMask = ~0;

    private Collider2D _lastTappedCellCollider;
    private float _lastTapTime = -999f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool IsDoubleTapOnSameCell() => IsDoubleTapOnSameCell(out _);
    public bool IsDoubleTapOnSameCell(out Collider2D currentCell)
    {
        currentCell = GetCellUnderPointer();
        if (currentCell == null)
            return false;

        bool isDoubleTap =
            (Time.time - _lastTapTime) <= doubleTapTime &&
            _lastTappedCellCollider != null &&
            currentCell == _lastTappedCellCollider;

        if (isDoubleTap)
        {
            ResetState();
            return true;
        }

        _lastTapTime = Time.time;
        _lastTappedCellCollider = currentCell;

        return false;
    }

    public void ResetState()
    {
        _lastTapTime = -999f;
        _lastTappedCellCollider = null;
    }

    private Collider2D GetCellUnderPointer()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
        if (worldCamera == null)
            return null;

        Vector3 screenPos = Input.mousePosition;
        Vector3 world = worldCamera.ScreenToWorldPoint(screenPos);
        Vector2 world2D = new Vector2(world.x, world.y);

        return Physics2D.OverlapPoint(world2D, cellMask);
    }
}