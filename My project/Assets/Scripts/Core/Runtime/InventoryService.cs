using System;
using System.Collections.Generic;
using System.Linq;
using TwelveMoons.Core.Config;
using UnityEngine;

namespace TwelveMoons.Core.Runtime
{
    public sealed class InventoryService : MonoBehaviour
    {
        [Header("依赖服务：配置和运行时")]
        [Tooltip("配置管理器；用于读取 ItemConfig。")]
        [SerializeField] private ConfigManager configManager;
        [Tooltip("运行时数据服务；用于保存背包物品数量。")]
        [SerializeField] private RuntimeDataService runtimeDataService;

        private readonly List<ItemDefinition> definitions = new List<ItemDefinition>();
        private readonly Dictionary<string, ItemDefinition> definitionsById =
            new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);

        public event Action InventoryChanged;

        public IReadOnlyList<ItemDefinition> Definitions => definitions;

        private void Awake()
        {
            ResolveDependencies();
            LoadItemConfig();
        }

        private void Start()
        {
            EnsureConfiguredRuntimeItems();
            NotifyInventoryChanged();
        }

        public void Refresh()
        {
            LoadItemConfig();
            EnsureConfiguredRuntimeItems();
            NotifyInventoryChanged();
        }

        public bool TryGetDefinition(string itemId, out ItemDefinition definition)
        {
            return definitionsById.TryGetValue(itemId, out definition);
        }

        public int GetCount(string itemId)
        {
            return runtimeDataService == null
                ? 0
                : runtimeDataService.Data.GetOrCreateItem(itemId).Count;
        }

        public bool HasItem(string itemId, int count)
        {
            return count <= 0 || GetCount(itemId) >= count;
        }

        public bool AddItem(string itemId, int count)
        {
            if (count <= 0)
            {
                return false;
            }

            if (!CanUseConfiguredItem(itemId))
            {
                return false;
            }

            runtimeDataService.AddItem(itemId, count);
            NotifyInventoryChanged();
            return true;
        }

        public bool TryRemoveItem(string itemId, int count)
        {
            if (count <= 0)
            {
                return false;
            }

            if (!CanUseConfiguredItem(itemId) || !HasItem(itemId, count))
            {
                return false;
            }

            runtimeDataService.AddItem(itemId, -count);
            NotifyInventoryChanged();
            return true;
        }

        public bool AddByType(InventoryItemType itemType, int count)
        {
            return TryFindSingleDefinitionByType(itemType, out var definition) &&
                   AddItem(definition.ItemId, count);
        }

        public bool TryRemoveByType(InventoryItemType itemType, int count)
        {
            return TryFindSingleDefinitionByType(itemType, out var definition) &&
                   TryRemoveItem(definition.ItemId, count);
        }

        public void AddMoney(int count)
        {
            AddByType(InventoryItemType.Money, count);
        }

        public void AddMaterial(int count)
        {
            AddByType(InventoryItemType.Material, count);
        }

        public void AddFood(int count)
        {
            AddByType(InventoryItemType.Food, count);
        }

        public void RemoveMoney(int count)
        {
            TryRemoveByType(InventoryItemType.Money, count);
        }

        public void RemoveMaterial(int count)
        {
            TryRemoveByType(InventoryItemType.Material, count);
        }

        public void RemoveFood(int count)
        {
            TryRemoveByType(InventoryItemType.Food, count);
        }

        private void ResolveDependencies()
        {
            if (configManager == null)
            {
                configManager = FindFirstObjectByType<ConfigManager>();
            }

            if (runtimeDataService == null)
            {
                runtimeDataService = FindFirstObjectByType<RuntimeDataService>();
            }
        }

        private void LoadItemConfig()
        {
            definitions.Clear();
            definitionsById.Clear();

            if (configManager == null || !configManager.TryGetTable("ItemConfig", out var itemTable))
            {
                Debug.LogWarning("InventoryService cannot load ItemConfig.", this);
                return;
            }

            foreach (var row in itemTable.Rows)
            {
                var definition = new ItemDefinition(row);
                if (string.IsNullOrEmpty(definition.ItemId))
                {
                    continue;
                }

                definitions.Add(definition);
                definitionsById[definition.ItemId] = definition;
            }
        }

        private void EnsureConfiguredRuntimeItems()
        {
            if (runtimeDataService == null)
            {
                Debug.LogWarning("InventoryService missing RuntimeDataService.", this);
                return;
            }

            foreach (var definition in definitions)
            {
                runtimeDataService.Data.GetOrCreateItem(definition.ItemId);
            }
        }

        private bool CanUseConfiguredItem(string itemId)
        {
            if (runtimeDataService == null)
            {
                Debug.LogWarning("InventoryService missing RuntimeDataService.", this);
                return false;
            }

            if (TryGetDefinition(itemId, out _))
            {
                return true;
            }

            Debug.LogWarning($"ItemId {itemId} is not configured in ItemConfig.", this);
            return false;
        }

        private bool TryFindSingleDefinitionByType(InventoryItemType itemType, out ItemDefinition definition)
        {
            definition = definitions.FirstOrDefault(candidate => candidate.ItemType == itemType);
            if (definition != null)
            {
                return true;
            }

            Debug.LogWarning($"ItemConfig has no item of type {itemType}.", this);
            return false;
        }

        private void NotifyInventoryChanged()
        {
            InventoryChanged?.Invoke();
        }
    }
}
