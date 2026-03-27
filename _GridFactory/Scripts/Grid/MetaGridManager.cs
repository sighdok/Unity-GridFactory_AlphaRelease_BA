using UnityEngine;

using GridFactory.Core;
using GridFactory.Meta;
using GridFactory.Conveyor;
using GridFactory.Blueprints;

namespace GridFactory.Grid
{
    // TODO: Umbenennen in GridMetaManager?
    public class MetaGridManager : MonoBehaviour
    {
        public static MetaGridManager Instance { get; private set; }

        private static BlueprintManager BPM => BlueprintManager.Instance;

        [Header("Visuals")]
        [SerializeField] private GameObject cellVisualPrefab;

        [Header("GridDesign")]
        [SerializeField] private GridDefinition gridDef;

        [Header("Sorting")]
        [SerializeField] private int sortingScale = 100;     // höher = mehr Auflösung bei Zwischenpositionen
        [SerializeField] private int sortingOffset = 10;      // optionaler Offset (z.B. für Layer-Gruppen)

        [Header("Save / Load Data")]
        [SerializeField] private SaveLoadMetaRegistry registry;

        private MetaCell[,] _cells;
        private int _width = 3;
        private int _height = 3;
        private float _cellSize = 1f;

        public MetaCell[,] AllCells => _cells;
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
            if (gridDef == null)
                return;

            _width = gridDef.width;
            _height = gridDef.height;

            _cells = new MetaCell[_width, _height];

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var pos = new Vector2Int(x, y);

                    _cells[x, y] = new MetaCell(pos);

                    if (cellVisualPrefab != null)
                    {
                        Vector3 worldPos = GridToWorld(pos);
                        GameObject tile = Instantiate(cellVisualPrefab, worldPos, Quaternion.identity, transform);
                        var spriteRenderer = tile.GetComponent<SpriteRenderer>();

                        tile.name = $"Cell_{x}_{y}";
                        _cells[x, y].Tile = tile;
                        _cells[x, y].Sprite = spriteRenderer;
                    }
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
                    float y = sr.transform.parent.transform.position.y;
                    float x = sr.transform.parent.transform.position.x;

                    int order;
                    if (sr.transform.parent.name.ToLowerInvariant().Contains("convey"))
                        order = sortingOffset + Mathf.RoundToInt(-y * (sortingScale * 10)) + Mathf.RoundToInt(-x);

                    order = sortingOffset + Mathf.RoundToInt(-y * sortingScale) + Mathf.RoundToInt(-x);

                    sr.sortingOrder = order;
                }
            }
        }

        public MetaCell GetCell(Vector2Int gridPos)
        {
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

        public MetaCell GetCellFromWorld(Vector3 worldPos)
        {
            Vector2Int gridPos = WorldToGrid(worldPos);
            return GetCell(gridPos);
        }

        public MetaGridStructureSaveData ToStructureSaveData()
        {
            var data = new MetaGridStructureSaveData { width = _width, height = _height };

            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                {
                    var cell = _cells[x, y];

                    var c = new MetaGridStructureSaveData.Cell
                    {
                        x = x,
                        y = y,
                    };

                    if (cell.Machine != null)
                    {
                        c.machineId = cell.Machine.saveId;
                        c.machineFacing = cell.Machine.Facing;

                        if (cell.Machine is MetaResourceNode rn)
                            c.resourceItemType = rn.ResourceItem;
                        else if (cell.Machine is MetaBlueprintModule bm)
                            c.blueprintId = bm.blueprint != null ? bm.blueprint.id : null;

                    }
                    if (cell.Conveyor != null)
                    {

                        c.conveyorId = cell.Conveyor.saveId;
                        c.conveyorOut = cell.Conveyor.OutputDirection;
                        c.conveyorIn = cell.Conveyor.InputDirection;
                    }

                    data.cells.Add(c);
                }
            return data;
        }

        public void ApplyStructureSaveData2Pass(MetaGridStructureSaveData data)
        {

            if (data == null)
                return;

            SaveLoadContext.BeginLoad();
            ClearAllPlacedObjects();

            foreach (var c in data.cells)
            {
                var cell = GetCell(new Vector2Int(c.x, c.y));
                if (cell == null)
                    continue;

                if (!string.IsNullOrEmpty(c.machineId))
                {
                    Debug.Log(c.machineId);
                    var prefab = registry != null ? registry.GetMachine(c.machineId) : null;
                    if (prefab != null)
                    {
                        var go = Instantiate(prefab, GridToWorld(cell.Position), Quaternion.identity, transform);
                        var m = go.GetComponent<MetaMachineBase>();

                        if (m != null)
                        {
                            cell.Machine = m;
                            m.Init(cell);
                            m.SetFacing(c.machineFacing);
                            Debug.Log(cell.Machine);
                            Debug.Log(cell);
                            if (m is MetaResourceNode rn)
                            {
                                rn.ResourceItem = c.resourceItemType;
                            }
                            else if (m is MetaBlueprintModule bm)
                            {
                                var bp = BPM.FindBlueprintById(c.blueprintId);
                                if (bp != null)
                                {
                                    bm.Init(cell);
                                    bm.SetFacing(bm.Facing, false);
                                    bm.InitWithBlueprint(bp);
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("no Prefab Found");
                    }
                }

                if (!string.IsNullOrEmpty(c.conveyorId))
                {
                    var prefab = registry != null ? registry.GetConveyor(c.conveyorId) : null;
                    if (prefab != null)
                    {
                        var go = Instantiate(prefab, GridToWorld(cell.Position), Quaternion.identity, transform);
                        var conv = go.GetComponent<MetaConveyorBase>();

                        if (conv != null)
                        {
                            cell.Conveyor = conv;
                            conv.Init(cell);
                            conv.SetOutputDirection(c.conveyorOut);
                            conv.SetInputDirection(c.conveyorIn, updateShape: false);
                        }
                    }
                }
            }

            foreach (var c in data.cells)
            {
                var cell = GetCell(new Vector2Int(c.x, c.y));
                if (cell == null)
                    continue;

                if (cell.Conveyor != null)
                {
                    cell.Conveyor.UpdateShapeAndRotation();
                    cell.Conveyor.ActivateAfterLoadOrPlace();
                }

                if (cell.Machine != null)
                    cell.Machine.ActivateAfterLoadOrPlace();
            }

            UpdateSpriteSortingByY();
            SaveLoadContext.EndLoad();
        }

        private void ClearAllPlacedObjects()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var cell = _cells[x, y];

                    if (cell.Machine != null)
                    {
                        Destroy(cell.Machine.gameObject);
                        cell.Machine = null;
                    }

                    if (cell.Conveyor != null)
                    {
                        Destroy(cell.Conveyor.gameObject);
                        cell.Conveyor = null;
                    }
                }
            }
        }
    }
}
