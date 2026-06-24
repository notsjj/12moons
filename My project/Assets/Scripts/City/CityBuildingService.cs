using System;
using System.Collections.Generic;
using System.Linq;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.City
{
    public sealed class CityBuildingService : MonoBehaviour
    {
        private const string TableName = "CityBuildingConfig";
        private const string ResourceEffectType = "Resource";
        private const string SuspicionEffectType = "Suspicion";

        [Header("配置来源：读取 CityBuildingConfig")]
        [Tooltip("配置管理器；留空时会在场景中自动查找，用于读取 CityBuildingConfig。")]
        [SerializeField] private ConfigManager configManager;

        [Header("依赖服务：执行建筑点击效果")]
        [Tooltip("运行时数据服务；用于读取建筑解锁状态、当前回合，并记录建筑本回合是否已经领取。")]
        [SerializeField] private RuntimeDataService runtimeDataService;
        [Tooltip("物品栏服务；资源类建筑点击后通过它把金币、建材、食物或道具放入 InventoryPanel。")]
        [SerializeField] private InventoryService inventoryService;
        [Tooltip("阵营质疑度服务；质疑度类建筑点击后通过它降低指定阵营的质疑度。")]
        [SerializeField] private FactionService factionService;

        [Header("运行时只读快照：建筑配置与状态")]
        [Tooltip("CityBuildingConfig 中成功读取到的建筑配置数量。")]
        [SerializeField] private int inspectorConfigCount;
        [Tooltip("运行时已经解锁的建筑 ID 列表；用于确认公文选项是否正确解锁建筑。")]
        [SerializeField] private string inspectorUnlockedBuildingIds;
        [Tooltip("最近一次点击建筑的执行结果；用于在 Inspector 中确认资源或质疑度变化。")]
        [SerializeField] private string inspectorLastCollectResult;

        private readonly List<CityBuildingDefinition> definitions = new List<CityBuildingDefinition>();
        private readonly Dictionary<string, CityBuildingDefinition> definitionsById =
            new Dictionary<string, CityBuildingDefinition>(StringComparer.Ordinal);

        public event Action BuildingStatesChanged;

        public IReadOnlyList<CityBuildingDefinition> Definitions => definitions;

        public int ConfigCount => inspectorConfigCount;

        public string UnlockedBuildingIds => inspectorUnlockedBuildingIds;

        public string LastCollectResult => inspectorLastCollectResult;

        private void Awake()
        {
            ResolveDependencies();
            LoadDefinitions();
            EnsureRuntimeBuildingStates();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            if (runtimeDataService != null)
            {
                runtimeDataService.BuildingUnlocked += OnBuildingUnlocked;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (runtimeDataService != null)
            {
                runtimeDataService.BuildingUnlocked -= OnBuildingUnlocked;
            }
        }

        private void Start()
        {
            Refresh();
        }

        [ContextMenu("刷新建筑配置与运行时状态")]
        public void Refresh()
        {
            ResolveDependencies();
            LoadDefinitions();
            EnsureRuntimeBuildingStates();
            RefreshInspectorSnapshot();
            BuildingStatesChanged?.Invoke();
        }

        public bool TryGetDefinition(string buildingId, out CityBuildingDefinition definition)
        {
            return definitionsById.TryGetValue(buildingId ?? string.Empty, out definition);
        }

        public bool IsUnlocked(string buildingId)
        {
            if (runtimeDataService == null || string.IsNullOrEmpty(buildingId))
            {
                return false;
            }

            return runtimeDataService.Data.GetOrCreateBuilding(buildingId).IsUnlocked;
        }

        public bool CanCollect(string buildingId)
        {
            if (runtimeDataService == null || !TryGetDefinition(buildingId, out var definition))
            {
                return false;
            }

            var state = runtimeDataService.Data.GetOrCreateBuilding(buildingId);
            if (!state.IsUnlocked)
            {
                return false;
            }

            var cooldownRound = Mathf.Max(1, definition.CooldownRound);
            return state.LastCollectedRound <= 0 ||
                   runtimeDataService.Data.CurrentRound - state.LastCollectedRound >= cooldownRound;
        }

        public IReadOnlyList<string> CollectAvailableOutputsForSettlement()
        {
            ResolveDependencies();
            LoadDefinitions();
            EnsureRuntimeBuildingStates();

            var results = new List<string>();
            foreach (var definition in definitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.BuildingId) || !CanCollect(definition.BuildingId))
                {
                    continue;
                }

                if (TryCollect(definition.BuildingId, out var resultMessage) && !string.IsNullOrWhiteSpace(resultMessage))
                {
                    var buildingName = string.IsNullOrWhiteSpace(definition.BuildingName)
                        ? definition.BuildingId
                        : definition.BuildingName;
                    results.Add($"{buildingName}：{resultMessage.Trim()}");
                }
            }

            return results;
        }

        public bool TryCollect(string buildingId, out string resultMessage)
        {
            resultMessage = string.Empty;

            if (runtimeDataService == null)
            {
                resultMessage = "缺少 RuntimeDataService，无法执行建筑点击。";
                SetLastResult(resultMessage);
                return false;
            }

            if (!TryGetDefinition(buildingId, out var definition))
            {
                resultMessage = $"建筑 {buildingId} 未配置在 CityBuildingConfig 中。";
                SetLastResult(resultMessage);
                return false;
            }

            if (!CanCollect(buildingId))
            {
                resultMessage = $"建筑 {buildingId} 尚未解锁或本回合冷却未结束。";
                SetLastResult(resultMessage);
                return false;
            }

            var applied = ApplyEffect(definition, out resultMessage);
            if (applied)
            {
                runtimeDataService.Data.GetOrCreateBuilding(buildingId)
                    .RecordCollected(runtimeDataService.Data.CurrentRound);
            }

            SetLastResult(resultMessage);
            BuildingStatesChanged?.Invoke();
            return applied;
        }

        private bool ApplyEffect(CityBuildingDefinition definition, out string resultMessage)
        {
            if (string.Equals(definition.BuildingEffectType, ResourceEffectType, StringComparison.OrdinalIgnoreCase))
            {
                if (inventoryService == null)
                {
                    resultMessage = "缺少 InventoryService，无法发放建筑产出。";
                    return false;
                }

                if (string.IsNullOrEmpty(definition.ProduceItemId) || definition.ProduceCount <= 0)
                {
                    resultMessage = $"建筑 {definition.BuildingId} 的资源产出配置无效。";
                    return false;
                }

                if (!inventoryService.AddItem(definition.ProduceItemId, definition.ProduceCount))
                {
                    resultMessage = $"建筑 {definition.BuildingId} 产出道具失败，请检查 ItemConfig。";
                    return false;
                }

                var itemName = definition.ProduceItemId;
                if (inventoryService.TryGetDefinition(definition.ProduceItemId, out var itemDefinition) &&
                    !string.IsNullOrEmpty(itemDefinition.ItemName))
                {
                    itemName = itemDefinition.ItemName;
                }

                resultMessage = $"获得 {itemName} x{definition.ProduceCount}";
                return true;
            }

            if (string.Equals(definition.BuildingEffectType, SuspicionEffectType, StringComparison.OrdinalIgnoreCase))
            {
                if (factionService == null)
                {
                    resultMessage = "缺少 FactionService，无法降低阵营质疑度。";
                    return false;
                }

                if (string.IsNullOrEmpty(definition.ReduceFactionId) || definition.ReduceSuspicionValue <= 0)
                {
                    resultMessage = $"建筑 {definition.BuildingId} 的质疑度配置无效。";
                    return false;
                }

                var state = factionService.ChangeSuspicion(
                    definition.ReduceFactionId,
                    -Mathf.Max(0, definition.ReduceSuspicionValue));
                if (state == null)
                {
                    resultMessage = $"建筑 {definition.BuildingId} 降低质疑度失败，请检查 FactionConfig。";
                    return false;
                }

                var factionName = definition.ReduceFactionId;
                if (factionService.TryGetDefinition(definition.ReduceFactionId, out var factionDefinition) &&
                    !string.IsNullOrEmpty(factionDefinition.FactionName))
                {
                    factionName = factionDefinition.FactionName;
                }

                resultMessage = $"{factionName} 质疑度 -{definition.ReduceSuspicionValue}";
                return true;
            }

            resultMessage = $"建筑 {definition.BuildingId} 的 BuildingEffectType={definition.BuildingEffectType} 不受支持。";
            return false;
        }

        private void ResolveDependencies()
        {
            if (configManager == null)
            {
                configManager = FindFirstObjectByType<ConfigManager>(FindObjectsInactive.Include);
            }

            if (runtimeDataService == null)
            {
                runtimeDataService = FindFirstObjectByType<RuntimeDataService>(FindObjectsInactive.Include);
            }

            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<InventoryService>(FindObjectsInactive.Include);
            }

            if (factionService == null)
            {
                factionService = FindFirstObjectByType<FactionService>(FindObjectsInactive.Include);
            }
        }

        private void LoadDefinitions()
        {
            definitions.Clear();
            definitionsById.Clear();

            if (configManager == null || !configManager.TryGetTable(TableName, out var table))
            {
                inspectorConfigCount = 0;
                return;
            }

            foreach (var row in table.Rows)
            {
                var definition = new CityBuildingDefinition(row);
                if (string.IsNullOrEmpty(definition.BuildingId))
                {
                    continue;
                }

                definitions.Add(definition);
                definitionsById[definition.BuildingId] = definition;
            }

            inspectorConfigCount = definitions.Count;
        }

        private void EnsureRuntimeBuildingStates()
        {
            if (runtimeDataService == null)
            {
                return;
            }

            foreach (var definition in definitions)
            {
                var state = runtimeDataService.Data.GetOrCreateBuilding(definition.BuildingId);
                if (definition.DefaultVisible)
                {
                    state.Unlock();
                }
            }
        }

        private void OnBuildingUnlocked(RuntimeBuildingState _)
        {
            RefreshInspectorSnapshot();
            BuildingStatesChanged?.Invoke();
        }

        private void SetLastResult(string resultMessage)
        {
            inspectorLastCollectResult = resultMessage;
            Debug.Log(resultMessage, this);
            RefreshInspectorSnapshot();
        }

        private void RefreshInspectorSnapshot()
        {
            if (runtimeDataService == null)
            {
                inspectorUnlockedBuildingIds = string.Empty;
                return;
            }

            inspectorUnlockedBuildingIds = string.Join(
                ", ",
                runtimeDataService.Data.Buildings
                    .Where(state => state.IsUnlocked)
                    .Select(state => state.BuildingId)
                    .OrderBy(id => id));
        }
    }
}
