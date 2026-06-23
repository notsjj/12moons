using System;
using System.Collections.Generic;
using System.Linq;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEngine;

namespace TwelveMoons.City
{
    public sealed class CitySideEventService : MonoBehaviour
    {
        private const string TableName = "SideEventConfig";

        [Header("配置来源：读取 SideEventConfig")]
        [Tooltip("配置管理器；留空时会在场景中自动查找，用于读取 SideEventConfig、StoryConfig、CharacterConfig 等配置。")]
        [SerializeField] private ConfigManager configManager;

        [Header("依赖服务：回合、任务、道具和剧情")]
        [Tooltip("运行时数据服务；用于读取当前回合，并记录每个支线事件是否已经触发。")]
        [SerializeField] private RuntimeDataService runtimeDataService;
        [Tooltip("回合服务；用于在回合推进时刷新当前可见支线事件并通知场景点位重新绑定。")]
        [SerializeField] private RoundService roundService;
        [Tooltip("剧情服务；点击支线角色后通过它播放 SideEventConfig.StoryId 对应剧情。")]
        [SerializeField] private StoryService storyService;
        [Tooltip("任务服务；仅用于检查 RequiredTaskId 和 RequiredTaskState，不直接由支线表触发任务。")]
        [SerializeField] private TaskService taskService;
        [Tooltip("物品栏服务；用于检查 RequiredItemId 和 RequiredItemCount 是否满足。")]
        [SerializeField] private InventoryService inventoryService;

        [Header("支线剧情显示：打开剧情面板")]
        [Tooltip("UI 启动器；支线事件成功启动 StoryService 后，会通过它打开剧情面板。为空时运行时自动查找。")]
        [SerializeField] private BaseSceneUIBootstrap uiBootstrap;

        [Header("运行时只读快照：支线事件状态")]
        [Tooltip("SideEventConfig 中成功读取到的支线事件数量。")]
        [SerializeField] private int inspectorConfigCount;
        [Tooltip("当前回合可显示的支线事件 ID 列表；用于确认城区点位是否应该出现支线角色。")]
        [SerializeField] private string inspectorVisibleSideEventIds;
        [Tooltip("已经点击触发过的支线事件 ID 列表；一次性支线会据此隐藏。")]
        [SerializeField] private string inspectorTriggeredSideEventIds;
        [Tooltip("最近一次点击支线角色的执行结果；用于确认是否成功播放剧情。")]
        [SerializeField] private string inspectorLastResult;

        private readonly List<SideEventDefinition> definitions = new List<SideEventDefinition>();
        private readonly Dictionary<string, SideEventDefinition> definitionsById =
            new Dictionary<string, SideEventDefinition>(StringComparer.Ordinal);

        public event Action SideEventsChanged;

        public IReadOnlyList<SideEventDefinition> Definitions => definitions;

        public int ConfigCount => inspectorConfigCount;

        public string VisibleSideEventIds => inspectorVisibleSideEventIds;

        public string TriggeredSideEventIds => inspectorTriggeredSideEventIds;

        public string LastResult => inspectorLastResult;

        public StoryService StoryService => storyService;

        private void Awake()
        {
            ResolveDependencies();
            LoadDefinitions();
            RefreshInspectorSnapshot();
        }


        private void OnEnable()
        {
            ResolveDependencies();
            if (roundService != null)
            {
                roundService.RoundChanged -= HandleRoundChanged;
                roundService.RoundChanged += HandleRoundChanged;
            }
        }

        private void OnDisable()
        {
            if (roundService != null)
            {
                roundService.RoundChanged -= HandleRoundChanged;
            }
        }

        private void HandleRoundChanged()
        {
            Refresh();
        }

        private void Start()
        {
            Refresh();
        }

        [ContextMenu("刷新支线事件配置与当前回合状态")]
        public void Refresh()
        {
            ResolveDependencies();
            LoadDefinitions();
            RefreshInspectorSnapshot();
            SideEventsChanged?.Invoke();
        }

        public bool TryGetDefinition(string sideEventId, out SideEventDefinition definition)
        {
            return definitionsById.TryGetValue(sideEventId ?? string.Empty, out definition);
        }

        public IReadOnlyList<SideEventDefinition> GetVisibleEvents()
        {
            return definitions
                .Where(IsVisibleNow)
                .OrderBy(definition => definition.CityAreaId)
                .ThenBy(definition => definition.PointId)
                .ThenBy(definition => definition.SideEventId)
                .ToList();
        }

        public bool IsVisibleNow(SideEventDefinition definition)
        {
            if (definition == null || runtimeDataService == null)
            {
                return false;
            }

            var currentRound = runtimeDataService.Data.CurrentRound;
            if (currentRound < definition.Round)
            {
                return false;
            }

            if (definition.ExpireRound > 0 && currentRound > definition.ExpireRound)
            {
                return false;
            }

            if (definition.IsOneTime &&
                runtimeDataService.Data.TryGetSideEvent(definition.SideEventId, out var state) &&
                state.HasTriggered)
            {
                return false;
            }

            return MeetsTaskRequirement(definition) && MeetsItemRequirement(definition);
        }

        public bool TryStartSideEvent(string sideEventId, out string resultMessage)
        {
            resultMessage = string.Empty;
            if (!TryGetDefinition(sideEventId, out var definition))
            {
                resultMessage = $"支线事件 {sideEventId} 未配置在 SideEventConfig 中。";
                SetLastResult(resultMessage);
                return false;
            }

            if (!IsVisibleNow(definition))
            {
                resultMessage = $"支线事件 {sideEventId} 当前回合或条件不满足，不能播放。";
                SetLastResult(resultMessage);
                return false;
            }

            if (storyService == null)
            {
                resultMessage = "缺少 StoryService，无法播放支线剧情。";
                SetLastResult(resultMessage);
                return false;
            }

            if (!storyService.StartStory(definition.StoryId))
            {
                resultMessage = $"支线事件 {sideEventId} 的 StoryId={definition.StoryId} 无法播放，请检查 StoryConfig。";
                SetLastResult(resultMessage);
                return false;
            }

            uiBootstrap?.ShowStory();

            runtimeDataService.Data.GetOrCreateSideEvent(definition.SideEventId)
                .RecordTriggered(runtimeDataService.Data.CurrentRound);
            resultMessage = $"已播放支线事件 {definition.SideEventId}，剧情 {definition.StoryId}。";
            SetLastResult(resultMessage);
            return true;
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

            if (roundService == null)
            {
                roundService = FindFirstObjectByType<RoundService>(FindObjectsInactive.Include);
            }

            if (storyService == null)
            {
                storyService = FindFirstObjectByType<StoryService>(FindObjectsInactive.Include);
            }

            if (taskService == null)
            {
                taskService = FindFirstObjectByType<TaskService>(FindObjectsInactive.Include);
            }

            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<InventoryService>(FindObjectsInactive.Include);
            }

            if (uiBootstrap == null)
            {
                uiBootstrap = FindFirstObjectByType<BaseSceneUIBootstrap>(FindObjectsInactive.Include);
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
                AddDefinition(new SideEventDefinition(row));
            }

            AddStoryPointDefinitions();
            inspectorConfigCount = definitions.Count;
        }

        private void AddDefinition(SideEventDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.SideEventId))
            {
                return;
            }

            definitions.Add(definition);
            definitionsById[definition.SideEventId] = definition;
        }

        private void AddStoryPointDefinitions()
        {
            if (configManager == null || !configManager.TryGetTable("StoryConfig", out var storyTable))
            {
                return;
            }

            var sideEventStoryIds = definitions
                .Where(definition => definition != null && !string.IsNullOrEmpty(definition.StoryId))
                .Select(definition => definition.StoryId)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var row in storyTable.Rows)
            {
                var story = new StoryDefinition(row);
                if (string.IsNullOrEmpty(story.StoryId) ||
                    sideEventStoryIds.Contains(story.StoryId) ||
                    story.RoundNumber <= 0 ||
                    !StoryTriggerUnitIds.IsCityPointTrigger(story.TriggerUnitId))
                {
                    continue;
                }

                var pointId = StoryTriggerUnitIds.ResolvePointId(
                    story.TriggerUnitId,
                    story.StoryId,
                    story.RoundNumber);
                if (string.IsNullOrEmpty(pointId))
                {
                    continue;
                }

                AddDefinition(CreateStoryPointDefinition(story, pointId));
            }
        }

        private static SideEventDefinition CreateStoryPointDefinition(StoryDefinition story, string pointId)
        {
            return new SideEventDefinition(new ConfigRow(new Dictionary<string, string>
            {
                { "SideEventId", $"Story_{story.StoryId}" },
                { "Round", story.RoundNumber.ToString() },
                { "CityAreaId", string.Empty },
                { "PointId", pointId },
                { "DisplayCharacterId", string.Empty },
                { "StoryId", story.StoryId },
                { "ExpireRound", story.RoundNumber.ToString() },
                { "IsOneTime", "1" },
                { "RequiredTaskId", string.Empty },
                { "RequiredTaskState", string.Empty },
                { "RequiredItemId", string.Empty },
                { "RequiredItemCount", "0" },
                { "Remark", "StoryConfig scheduled story" }
            }));
        }

        private bool MeetsTaskRequirement(SideEventDefinition definition)
        {
            if (string.IsNullOrEmpty(definition.RequiredTaskId))
            {
                return true;
            }

            if (runtimeDataService == null)
            {
                return false;
            }

            var state = runtimeDataService.Data.Tasks
                .FirstOrDefault(candidate => candidate.TaskId == definition.RequiredTaskId);
            if (state == null || state.Status == TaskRuntimeStatus.NotStarted)
            {
                return false;
            }

            return string.IsNullOrEmpty(definition.RequiredTaskState) ||
                   string.Equals(state.Status.ToString(), definition.RequiredTaskState, StringComparison.OrdinalIgnoreCase);
        }

        private bool MeetsItemRequirement(SideEventDefinition definition)
        {
            if (string.IsNullOrEmpty(definition.RequiredItemId) || definition.RequiredItemCount <= 0)
            {
                return true;
            }

            if (inventoryService != null)
            {
                return inventoryService.HasItem(definition.RequiredItemId, definition.RequiredItemCount);
            }

            return runtimeDataService != null &&
                   runtimeDataService.Data.Items.Any(candidate =>
                       candidate.ItemId == definition.RequiredItemId &&
                       candidate.Count >= definition.RequiredItemCount);
        }

        private void SetLastResult(string resultMessage)
        {
            inspectorLastResult = resultMessage;
            Debug.Log(resultMessage, this);
            RefreshInspectorSnapshot();
        }

        private void RefreshInspectorSnapshot()
        {
            inspectorVisibleSideEventIds = string.Join(", ", GetVisibleEvents().Select(definition => definition.SideEventId));
            inspectorTriggeredSideEventIds = runtimeDataService == null
                ? string.Empty
                : string.Join(
                    ", ",
                    runtimeDataService.Data.SideEvents
                        .Where(state => state.HasTriggered)
                        .OrderBy(state => state.SideEventId)
                        .Select(state => $"{state.SideEventId}@{state.TriggeredRound}"));
        }
    }
}
