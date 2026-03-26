using System.Collections.Generic;

using UnityEngine;

using GridFactory.Grid;

namespace GridFactory.Core
{
    public class GridDefinitionManager : MonoBehaviour
    {
        public static GridDefinitionManager Instance { get; private set; }

        [Header("Preset Pool (Handcrafted)")]
        [SerializeField] private List<GridDefinition> presetPool = new List<GridDefinition>();
        [Header("Starter Presets (owned from start)")]
        [SerializeField] private List<GridDefinition> starterPresets = new List<GridDefinition>();

        private readonly List<GridDefinitionOwned> _owned = new List<GridDefinitionOwned>();
        private readonly HashSet<string> _ownedIds = new HashSet<string>();

        public bool IsOwned(string id) => _ownedIds.Contains(id);
        public IReadOnlyList<GridDefinition> GetPresetPool() => presetPool;
        public IReadOnlyList<GridDefinitionOwned> Owned => _owned;

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
            InitDefaults();
        }

        public void InitDefaults()
        {
            _owned.Clear();
            _ownedIds.Clear();

            if (starterPresets != null)
            {
                foreach (var p in starterPresets)
                {
                    if (p == null)
                        continue;
                    AddOwnedFromPreset(p);
                }
            }

            if (_owned.Count == 0 && presetPool != null && presetPool.Count > 0 && presetPool[0] != null)
                AddOwnedFromPreset(presetPool[0]);
        }

        public GridDefinitionOwned AddOwnedFromPreset(GridDefinition preset)
        {
            if (preset == null)
                return null;

            string id = preset.id;
            if (string.IsNullOrWhiteSpace(id)) id = preset.name;

            if (_ownedIds.Contains(id))
                return GetOwnedById(id);

            var def = new GridDefinitionOwned
            {
                id = id,
                displayName = preset.displayName,
                width = preset.width,
                height = preset.height,
                isPreset = true,
                seed = 0,
                locks = new List<CellLockData>()
            };

            if (preset.unlockableLockedCells != null)
            {
                foreach (var p in preset.unlockableLockedCells)
                {
                    if (p.x < 0 || p.y < 0 || p.x >= def.width || p.y >= def.height)
                        continue;

                    def.locks.Add(new CellLockData(p.x, p.y, LockType.Unlockable));
                }
            }

            _owned.Add(def);
            _ownedIds.Add(def.id);
            return def;
        }

        public GridDefinitionOwned AddOwnedRandom(int width, int height, int seed, string displayName, List<CellLockData> locks)
        {
            string id = $"rnd_{width}x{height}_{seed}";
            if (_ownedIds.Contains(id))
                return GetOwnedById(id);

            var def = new GridDefinitionOwned
            {
                id = id,
                displayName = string.IsNullOrWhiteSpace(displayName) ? $"Random {width}x{height}" : displayName,
                width = width,
                height = height,
                isPreset = false,
                seed = seed,
                locks = locks ?? new List<CellLockData>()
            };

            _owned.Add(def);
            _ownedIds.Add(def.id);
            return def;
        }

        public GridDefinitionOwned GetOwnedById(string id)
        {
            for (int i = 0; i < _owned.Count; i++)
                if (_owned[i].id == id)
                    return _owned[i];
            return null;
        }

        public GridLibrarySaveData ToSaveData()
        {
            var data = new GridLibrarySaveData();

            foreach (var g in _owned)
            {
                var s = new OwnedGridDefinitionSaveData
                {
                    id = g.id,
                    displayName = g.displayName,
                    width = g.width,
                    height = g.height,
                    isPreset = g.isPreset,
                    seed = g.seed,
                    locks = new List<CellLockSaveData>()
                };

                if (g.locks != null)
                {
                    foreach (var l in g.locks)
                    {
                        s.locks.Add(new CellLockSaveData
                        {
                            x = l.x,
                            y = l.y,
                            lockType = l.lockType
                        });
                    }
                }
                data.owned.Add(s);
            }

            return data;
        }

        public void ApplySaveData(GridLibrarySaveData data)
        {
            _owned.Clear();
            _ownedIds.Clear();

            if (data?.owned == null) return;

            foreach (var s in data.owned)
            {
                var g = new GridDefinitionOwned
                {
                    id = s.id,
                    displayName = s.displayName,
                    width = s.width,
                    height = s.height,
                    isPreset = s.isPreset,
                    seed = s.seed,
                    locks = new List<CellLockData>()
                };

                if (s.locks != null)
                {
                    foreach (var l in s.locks)
                    {
                        g.locks.Add(new CellLockData(l.x, l.y, l.lockType));
                    }
                }

                _owned.Add(g);
                _ownedIds.Add(g.id);
            }
        }
    }
}
