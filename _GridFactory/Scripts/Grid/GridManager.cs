using System.Linq;

using UnityEngine;

using GridFactory.Core;
using GridFactory.UI;
using System.Collections.Generic;

[System.Flags]
public enum GridEdge
{
    None = 0,
    Left = 1 << 0,
    Right = 1 << 1,
    Bottom = 1 << 2,
    Top = 1 << 3
}

namespace GridFactory.Grid
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        private static GridDefinitionManager GDM => GridDefinitionManager.Instance;
        private static PortBuildingController PBC => PortBuildingController.Instance;

        [Header("Visuals")]
        [SerializeField] private GameObject cellVisualPrefab;

        [Header("GridDesign")]
        [SerializeField] private GridDefinition gridDef; // optional fallback
        [SerializeField] private GridPickerModalUI gridPickerInvoke;

        [Header("Sorting")]
        [SerializeField] private int sortingScale = 100;     // höher = mehr Auflösung bei Zwischenpositionen
        [SerializeField] private int sortingOffset = 10;      // optionaler Offset (z.B. für Layer-Gruppen)


        private GridDefinitionOwned _activeOwned;
        private GridDefinition _activePreset;
        private GridCell[,] _cells;
        private string _currentGridDefinitionId;
        private int _width = 3;
        private int _height = 3;
        private float _cellSize = 1f;

        public GridCell[,] AllCells => _cells;
        public string CurrentGridDefinitionId => _currentGridDefinitionId;
        public int Width => _width;
        public int Height => _height;

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
            if (gridPickerInvoke)
                gridPickerInvoke.onPicked += ApplyOwnedGrid;

            if (_cells != null && _cells.Length > 0)
                return;

            if (_activeOwned != null)
            {
                ApplyOwnedGrid(_activeOwned);
                return;
            }

            if (_activePreset != null)
            {
                ApplyGridDefinition(_activePreset);
                return;
            }

            if (GDM.Owned.Count > 0)
            {
                ApplyOwnedGrid(GDM.Owned[0]);
                return;
            }

            if (gridDef != null)
            {
                ApplyGridDefinition(gridDef);
                return;
            }

            ApplyRuntimeGrid(1, 5, (x, y) => false);
        }

        public void ApplyGridDefinition(GridDefinition preset)
        {
            _activePreset = preset;
            _activeOwned = null;

            if (preset == null)
                return;

            int w = preset.width;
            int h = preset.height;

            var locks = (preset.unlockableLockedCells != null && preset.unlockableLockedCells.Length > 0)
                ? preset.unlockableLockedCells
                : preset.lockedCells;

            _currentGridDefinitionId = preset.id;

            ApplyRuntimeGrid(w, h, (x, y) => locks != null && locks.Contains(new Vector2Int(x, y)));
        }

        public void ApplyOwnedGrid(GridDefinitionOwned owned)
        {
            _activeOwned = owned;
            _activePreset = null;

            if (owned == null)
                return;

            _currentGridDefinitionId = owned.id;

            ApplyRuntimeGrid(owned.width, owned.height, (x, y) => owned.IsLocked(new Vector2Int(x, y)));
        }

        private void ApplyRuntimeGrid(int w, int h, System.Func<int, int, bool> isLocked)
        {
            DestroyExistingVisuals();

            _width = Mathf.Max(1, w);
            _height = Mathf.Max(1, h);

            _cells = new GridCell[_width, _height];

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    bool locked = isLocked != null && isLocked(x, y);

                    _cells[x, y] = new GridCell(pos, locked);

                    if (cellVisualPrefab != null)
                    {
                        Vector3 worldPos = GridToWorld(pos);
                        GameObject tile = Instantiate(cellVisualPrefab, worldPos, Quaternion.identity, transform);

                        var spriteRenderer = tile.transform.Find("CellBG").GetComponent<SpriteRenderer>();
                        tile.name = $"Cell_{x}_{y}";
                        _cells[x, y].Sprite = spriteRenderer;

                        UpdateCellVisual(_cells[x, y]);
                    }
                }
            }
            UpdateSpriteSortingByY();
            PBC.ResetPorts();
        }
        public void LockPortCells()
        {
            if (_cells == null)
                return;
            if (_height < 3)
                return;

            foreach (var p in EnumeratePortCells())
            {
                var cell = GetCell(p);
                if (cell == null)
                    continue;

                if (!cell.IsLocked)
                {
                    cell.IsLocked = true;
                    cell.LockType = LockType.SoftLock;
                }

                UpdateCellVisual(cell);
            }
        }

        public void UnlockPortCells()
        {
            if (_cells == null)
                return;
            if (_height < 3)
                return;

            foreach (var p in EnumeratePortCells())
            {
                var cell = GetCell(p);
                if (cell == null)
                    continue;

                if (cell.IsLocked && cell.LockType == LockType.SoftLock)
                {
                    cell.IsLocked = false;
                    cell.LockType = LockType.NotLocked;
                }

                UpdateCellVisual(cell);
            }
        }

        public void UnlockAllSides()
        {
            UnlockSide(GridEdge.Left);
            UnlockSide(GridEdge.Right);
            UnlockSide(GridEdge.Top);
            UnlockSide(GridEdge.Bottom);
        }

        public void LockSide(GridEdge side)
        {
            if (_cells == null)
                return;
            if (_width < 1 || _height < 1)
                return;

            foreach (var p in EnumerateSideCells(side, includeCorners: true))
            {
                var cell = GetCell(p);
                if (cell == null)
                    continue;

                if (!cell.IsLocked)
                {
                    cell.IsLocked = true;
                    cell.LockType = LockType.SoftLock;
                }

                UpdateCellVisual(cell);
            }
        }

        public void UnlockSide(GridEdge side)
        {
            if (_cells == null)
                return;
            if (_width < 1 || _height < 1)
                return;

            foreach (var p in EnumerateSideCells(side, includeCorners: false))
            {
                var cell = GetCell(p);
                if (cell == null)
                    continue;

                if (cell.IsLocked && cell.LockType == LockType.SoftLock)
                {
                    cell.IsLocked = false;
                    cell.LockType = LockType.NotLocked;
                }

                UpdateCellVisual(cell);
            }
        }

        private void UpdateCellVisual(GridCell cell)
        {
            if (cell?.Sprite == null) return;

            if (cell.IsLocked)
            {
                if (cell.LockType == LockType.SoftLock)
                    cell.Sprite.color = new Color(0.4f, 0f, 0.1f, 0.25f);
                else
                    cell.Sprite.color = new Color(0f, 0f, 0f, 0.25f);
            }
            else
            {
                cell.Sprite.color = new Color(0.26f, 0.18f, 0f, 0.25f);
            }
        }

        private void DestroyExistingVisuals()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        private IEnumerable<Vector2Int> EnumerateSideCells(GridEdge side, bool includeCorners)
        {
            int minX = 0;
            int maxX = _width - 1;
            int minY = 0;
            int maxY = _height - 1;

            switch (side)
            {
                case GridEdge.Top:
                    {
                        int y = maxY;
                        int startX = includeCorners ? minX : minX + 1;
                        int endX = includeCorners ? maxX : maxX - 1;
                        for (int x = startX; x <= endX; x++)
                            yield return new Vector2Int(x, y);
                        break;
                    }

                case GridEdge.Bottom:
                    {
                        int y = minY;
                        int startX = includeCorners ? minX : minX + 1;
                        int endX = includeCorners ? maxX : maxX - 1;
                        for (int x = startX; x <= endX; x++)
                            yield return new Vector2Int(x, y);
                        break;
                    }

                case GridEdge.Left:
                    {
                        int x = minX;
                        int startY = includeCorners ? minY : minY + 1;
                        int endY = includeCorners ? maxY : maxY - 1;
                        for (int y = startY; y <= endY; y++)
                            yield return new Vector2Int(x, y);
                        break;
                    }

                case GridEdge.Right:
                    {
                        int x = maxX;
                        int startY = includeCorners ? minY : minY + 1;
                        int endY = includeCorners ? maxY : maxY - 1;
                        for (int y = startY; y <= endY; y++)
                            yield return new Vector2Int(x, y);
                        break;
                    }
            }
        }

        private IEnumerable<Vector2Int> EnumeratePortCells()
        {
            if (_cells == null)
                yield break;

            int minX = 0;
            int maxX = _width - 1;
            int minY = 0;
            int maxY = _height - 1;

            yield return new Vector2Int(minX, minY);
            yield return new Vector2Int(maxX, minY);
            yield return new Vector2Int(minX, maxY);
            yield return new Vector2Int(maxX, maxY);

            for (int x = 1; x < _width - 1; x++)
            {
                for (int y = 1; y < _height - 1; y++)
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }

        public void UpdateSpriteSortingByY(bool includeInactive = true)
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive);

            foreach (var sr in renderers)
            {
                if (sr == null)
                    continue;

                if (sr.gameObject.name.ToLowerInvariant().Contains("objectsprite"))
                {

                    float y = sr.transform.position.y;
                    float x = sr.transform.position.x;
                    int order = sortingOffset + Mathf.RoundToInt(-y * sortingScale);

                    sr.sortingOrder = order + Mathf.RoundToInt(x);
                }
            }
        }

        public GridCell GetCell(Vector2Int gridPos)
        {
            if (_cells == null)
                return null;
            if (gridPos.x < 0 || gridPos.y < 0 || gridPos.x >= _width || gridPos.y >= _height)
                return null;
            return _cells[gridPos.x, gridPos.y];
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            float halfWidth = (_width - 1) / 2f;
            float halfHeight = (_height - 1) / 2f;

            float localX = (gridPos.x - halfWidth) * _cellSize;
            float localY = (gridPos.y - halfHeight) * _cellSize;

            return transform.position + new Vector3(localX, localY, 0f);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3 local = worldPos - transform.position;

            float halfWidth = (_width - 1) / 2f;
            float halfHeight = (_height - 1) / 2f;

            float gx = local.x / _cellSize + halfWidth;
            float gy = local.y / _cellSize + halfHeight;

            int x = Mathf.RoundToInt(gx);
            int y = Mathf.RoundToInt(gy);

            return new Vector2Int(x, y);
        }

        public GridCell GetCellFromWorld(Vector3 worldPos)
        {
            Vector2Int gridPos = WorldToGrid(worldPos);
            return GetCell(gridPos);
        }
    }
}
