using System;
using System.Collections.Generic;
using GridFactory.Grid;
using UnityEngine;

namespace GridFactory.Core
{
    public class GridShopManager : MonoBehaviour
    {
        public static GridShopManager Instance { get; private set; }
        private static GridDefinitionManager GDM => GridDefinitionManager.Instance;

        [Header("Offer Composition (Default: 2 Presets + 2 Random)")]
        [SerializeField] private int offerCount = 4;
        [SerializeField] private int minPresets = 2;
        [SerializeField] private int minRandom = 2;

        [Header("Random Settings")]
        [SerializeField, Range(0f, 0.9f)] private float maxLockedPercent = 0.25f;
        [SerializeField] private Vector2Int randomSizeMin = new Vector2Int(3, 3);
        [SerializeField] private Vector2Int randomSizeMax = new Vector2Int(6, 6);

        [Header("Economy")]
        [SerializeField] private int pricePerFreeCell = 3;
        [SerializeField] private int refreshBaseCost = 50;
        [SerializeField] private int refreshCostStep = 25;

        private readonly List<GridShopOfferSaveData> _currentOffers = new List<GridShopOfferSaveData>();
        private int _refreshCount;

        public IReadOnlyList<GridShopOfferSaveData> CurrentOffers => _currentOffers;
        public int RefreshCount => _refreshCount;

        public Action _onGridBuy;
        public event Action OnBuyGrid;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void GenerateInitialOffers()
        {
            _refreshCount = 0;
            RollNewOffers();
        }

        public int GetRefreshCost()
        {
            return refreshBaseCost + _refreshCount * refreshCostStep;
        }

        public bool TryRefresh(Func<int> getGold, Action<int> spendGold)
        {
            int cost = GetRefreshCost();

            if (getGold == null || spendGold == null)
                return false;
            if (getGold() < cost)
                return false;

            spendGold(cost);
            _refreshCount++;
            RollNewOffers();

            return true;
        }

        public bool BuyOfferWithCallbacks(string offerId, Func<int> getGold, Action<int> spendGold)
        {
            if (string.IsNullOrWhiteSpace(offerId))
                return false;
            if (getGold == null || spendGold == null)
                return false;

            var offer = _currentOffers.Find(o => o.offerId == offerId);
            if (offer == null)
                return false;

            if (getGold() < offer.price)
                return false;

            if (offer.isPreset)
            {
                if (GDM.IsOwned(offer.presetId))
                    return false;
            }
            else
            {
                string rndId = $"rnd_{offer.width}x{offer.height}_{offer.seed}";
                if (GDM.IsOwned(rndId))
                    return false;
            }

            spendGold(offer.price);

            if (offer.isPreset)
            {
                var presets = GDM.GetPresetPool();
                GridDefinition preset = null;
                for (int i = 0; i < presets.Count; i++)
                {
                    if (presets[i] != null && presets[i].id == offer.presetId)
                    {
                        preset = presets[i];
                        break;
                    }
                }

                if (preset == null)
                    return false;

                GDM.AddOwnedFromPreset(preset);
            }
            else
            {
                var settings = new RandomGridGenerator.Settings
                {
                    maxLockedPercent = maxLockedPercent,
                    maxAttempts = 200
                };

                var locks = RandomGridGenerator.GenerateUnlockableLocks(offer.width, offer.height, offer.seed, settings);
                GDM.AddOwnedRandom(offer.width, offer.height, offer.seed, offer.displayName, locks);
            }

            _currentOffers.RemoveAll(o => o.offerId == offerId);

            OnBuyGrid?.Invoke();
            _onGridBuy?.Invoke();

            return true;
        }

        private void RollNewOffers()
        {
            _currentOffers.Clear();

            int presetsToAdd = Mathf.Min(minPresets, offerCount);
            int randomToAdd = Mathf.Min(minRandom, offerCount - presetsToAdd);
            int remaining = offerCount - (presetsToAdd + randomToAdd);

            AddPresetOffers(presetsToAdd);
            AddRandomOffers(randomToAdd);

            for (int i = 0; i < remaining; i++)
            {
                if (i % 2 == 0) AddPresetOffers(1);
                else AddRandomOffers(1);
            }
        }

        private void AddPresetOffers(int count)
        {
            var pool = GDM.GetPresetPool();
            if (pool == null || pool.Count == 0) return;

            for (int i = 0; i < pool.Count && count > 0; i++)
            {
                var p = pool[i];
                if (p == null) continue;
                if (GDM.IsOwned(p.id)) continue;

                var offer = new GridShopOfferSaveData
                {
                    offerId = Guid.NewGuid().ToString(),
                    isPreset = true,
                    presetId = p.id,
                    width = p.width,
                    height = p.height,
                    seed = 0,
                    displayName = p.displayName
                };

                int lockedCount = (p.unlockableLockedCells != null) ? p.unlockableLockedCells.Length : 0;
                int freeCells = p.width * p.height - lockedCount;
                if (p.price > 0)
                    offer.price = p.price;
                else
                    offer.price = CalcPriceFromFreeCells(freeCells);

                _currentOffers.Add(offer);
                count--;
            }
        }

        private void AddRandomOffers(int count)
        {
            for (; count > 0; count--)
            {
                int w = UnityEngine.Random.Range(randomSizeMin.x, randomSizeMax.x + 1);
                int h = UnityEngine.Random.Range(randomSizeMin.y, randomSizeMax.y + 1);
                int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                var settings = new RandomGridGenerator.Settings { maxLockedPercent = maxLockedPercent, maxAttempts = 200 };
                var locks = RandomGridGenerator.GenerateUnlockableLocks(w, h, seed, settings);
                int freeCells = w * h - (locks?.Count ?? 0);

                var offer = new GridShopOfferSaveData
                {
                    offerId = Guid.NewGuid().ToString(),
                    isPreset = false,
                    presetId = null,
                    width = w,
                    height = h,
                    seed = seed,
                    displayName = $"Random {w}x{h}",
                    price = CalcPriceFromFreeCells(freeCells)
                };

                _currentOffers.Add(offer);
            }
        }

        private int CalcPriceFromFreeCells(int freeCells)
        {
            return Mathf.Max(0, freeCells) * Mathf.Max(1, pricePerFreeCell);
        }

        public GridShopStateSaveData ToSaveData()
        {
            return new GridShopStateSaveData
            {
                refreshCount = _refreshCount,
                offers = new List<GridShopOfferSaveData>(_currentOffers)
            };
        }

        public void ApplySaveData(GridShopStateSaveData data)
        {
            _refreshCount = data != null ? data.refreshCount : 0;
            _currentOffers.Clear();

            if (data?.offers != null)
                _currentOffers.AddRange(data.offers);
            if (_currentOffers.Count == 0)
                RollNewOffers();
        }
    }
}
