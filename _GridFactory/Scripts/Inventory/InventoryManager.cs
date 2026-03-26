using System;
using System.Collections.Generic;

using UnityEngine;

using GridFactory.Core;

namespace GridFactory.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Item Definitions")]
        [SerializeField] private List<ItemDefinition> itemDefinitions = new List<ItemDefinition>();
        [SerializeField] private List<ItemType> itemTypes = new List<ItemType>();

        [Header("Currency")]
        [SerializeField] private int gold;

        private Dictionary<ItemType, ItemDefinition> _definitionByType;
        private Dictionary<ItemType, int> _itemCounts = new Dictionary<ItemType, int>();

        public int Gold => gold;

        public event Action<int> OnGoldAdded;
        public event Action OnInventoryChanged;

        public List<ItemDefinition> AllItems
        {
            get => itemDefinitions;
        }

        public List<ItemType> allTypes
        {
            get => itemTypes;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            BuildDefinitionLookup();
        }

        private void Start()
        {
            InitItemCounts();
        }

        private void BuildDefinitionLookup()
        {
            _definitionByType = new Dictionary<ItemType, ItemDefinition>();
            ItemDefinition[] myDefinitions = Resources.LoadAll<ItemDefinition>("ItemDefinitions/");

            foreach (ItemDefinition def in myDefinitions)
            {
                ItemDefinition w = (ItemDefinition)def;
                itemDefinitions.Add(w);
                itemTypes.Add(w.type);
                if (!_definitionByType.ContainsKey(def.type))
                    _definitionByType.Add(def.type, def);
            }
        }

        private void InitItemCounts()
        {
            _itemCounts.Clear();
            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                if (type == ItemType.None)
                    continue;
                _itemCounts[type] = 0;
            }
        }

        public void AddItem(ItemType type, int amount = 1)
        {
            if (type == ItemType.None || amount <= 0)
                return;

            if (!_itemCounts.ContainsKey(type))
                _itemCounts[type] = 0;

            _itemCounts[type] += amount;

            OnInventoryChanged?.Invoke();
        }

        public bool RemoveItem(ItemType type, int amount = 1)
        {
            if (type == ItemType.None || amount <= 0)
                return false;
            if (!_itemCounts.TryGetValue(type, out var current))
                return false;
            if (current < amount)
                return false;

            _itemCounts[type] = current - amount;

            OnInventoryChanged?.Invoke();
            return true;
        }

        public int GetItemCount(ItemType type)
        {
            if (type == ItemType.None)
                return 0;
            return _itemCounts.TryGetValue(type, out var c) ? c : 0;
        }

        public Sprite GetItemSprite(ItemType type)
        {
            if (type == ItemType.None)
                return null;
            return GetDefinition(type).icon;
        }

        public ItemDefinition GetDefinition(ItemType type)
        {
            if (_definitionByType != null && _definitionByType.TryGetValue(type, out var def))
                return def;
            return null;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            gold += amount;
            OnGoldAdded?.Invoke(amount);
            OnInventoryChanged?.Invoke();
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0) return false;
            if (gold < amount) return false;

            gold -= amount;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public Dictionary<ItemType, int> GetAllItemCountsCopy()
        {
            return new Dictionary<ItemType, int>(_itemCounts);
        }

        public void SetAllItemCounts(Dictionary<ItemType, int> newCounts)
        {
            _itemCounts.Clear();
            foreach (var kvp in newCounts)
            {
                if (kvp.Key == ItemType.None)
                    continue;
                _itemCounts[kvp.Key] = Mathf.Max(0, kvp.Value);
            }
            OnInventoryChanged?.Invoke();
        }

        public void SetGoldDirect(int amount)
        {
            gold = Mathf.Max(0, amount);
            OnInventoryChanged?.Invoke();
        }

        public void ClearInventory()
        {
            InitItemCounts();
            gold = 0;

            OnInventoryChanged?.Invoke();
        }
    }
}