using System;
using System.Collections.Generic;
using System.Linq;
using TwelveMoons.Core.Config;
using UnityEngine;

namespace TwelveMoons.Core.Runtime
{
    public sealed class RuntimeDataService : MonoBehaviour
    {
        [Header("依赖引用：用于读取配置表")]
        [Tooltip("配置管理器；留空时会在场景中自动查找，用于读取灾难、道具和阵营初始配置。")]
        [SerializeField] private ConfigManager configManager;

        [Header("开局设置：用于编辑器测试新游戏数据")]
        [Tooltip("勾选后 Awake 时立即按初始灾难 ID 创建一局新游戏，方便在 Inspector 中观察运行时数据。")]
        [SerializeField] private bool createNewGameOnAwake;
        [Tooltip("创建新游戏时使用的灾难 ID，必须存在于 DisasterConfig。")]
        [SerializeField] private string initialDisasterId = "disaster_flood_01";

        [Header("运行时调试可视化：只读观察当前回合、公文、任务和剧情")]
        [Tooltip("勾选后每帧末尾刷新下方快照，便于在 Play 模式 Inspector 中观察当前回合数据。")]
        [SerializeField] private bool autoRefreshInspectorSnapshot = true;
        [Tooltip("当前回合与灾难阶段摘要；用于确认回合推进后读取到的整体状态。")]
        [SerializeField] private string inspectorRoundSummary;
        [Tooltip("当前回合已经进入待处理队列的公文，并显示所属任务、阶段和每个选项会追加的后续公文。")]
        [SerializeField] private List<RuntimeInspectorDocumentEntry> inspectorCurrentRoundDocuments =
            new List<RuntimeInspectorDocumentEntry>();
        [Tooltip("已经记录但尚未到触发回合的后续公文；用于确认选项 NextDocumentId 是否正确排队。")]
        [SerializeField] private List<RuntimeInspectorFollowUpDocumentEntry> inspectorPendingFollowUpDocuments =
            new List<RuntimeInspectorFollowUpDocumentEntry>();
        [Tooltip("运行时已经创建的任务状态；用于观察当前任务阶段、分数和完成状态。")]
        [SerializeField] private List<RuntimeInspectorTaskEntry> inspectorRuntimeTasks =
            new List<RuntimeInspectorTaskEntry>();
        [Tooltip("配置表中全部任务及其阶段概览；用于确认当前项目有哪些任务可被激活。")]
        [SerializeField] private List<RuntimeInspectorTaskConfigEntry> inspectorConfiguredTasks =
            new List<RuntimeInspectorTaskConfigEntry>();
        [Tooltip("当前回合可播放的剧情队列；用于确认任务阶段开始、结束和公文前剧情是否已入队。")]
        [SerializeField] private List<RuntimeInspectorStoryEntry> inspectorCurrentRoundStories =
            new List<RuntimeInspectorStoryEntry>();
        [Tooltip("当前正在播放的剧情摘要；为空表示当前没有剧情播放状态。")]
        [SerializeField] private string inspectorCurrentStorySummary;
        [Tooltip("运行时建筑状态快照；用于确认公文选项是否已经把建筑写入解锁状态。")]
        [SerializeField] private List<RuntimeInspectorBuildingEntry> inspectorRuntimeBuildings =
            new List<RuntimeInspectorBuildingEntry>();

        public GameRuntimeData Data { get; } = new GameRuntimeData();

        public event Action<RuntimeLetterState> LetterReceived;

        public event Action<string> LetterRemoved;

        public event Action<RuntimeBuildingState> BuildingUnlocked;

        private void Awake()
        {
            if (configManager == null)
            {
                configManager = FindFirstObjectByType<ConfigManager>();
            }

            if (createNewGameOnAwake)
            {
                CreateNewGame(initialDisasterId);
            }
        }

        private void LateUpdate()
        {
            if (autoRefreshInspectorSnapshot)
            {
                RefreshInspectorSnapshot();
            }
        }

        public void CreateNewGame(string disasterId)
        {
            var totalRound = GetDisasterTotalRound(disasterId);
            Data.Reset(disasterId, totalRound);
            InitializeConfiguredItems();
            InitializeConfiguredFactions();
            InitializeConfiguredBuildings();

            Debug.Log(
                $"Runtime data initialized. Disaster={Data.DisasterId}, Round={Data.CurrentRound}/{Data.TotalRound}, Items={Data.Items.Count}, Factions={Data.Factions.Count}.",
                this);
            RefreshInspectorSnapshot();
        }

        public RuntimeItemState AddItem(string itemId, int delta)
        {
            var item = Data.GetOrCreateItem(itemId);
            item.AddCount(delta);
            return item;
        }

        public RuntimeTaskState ActivateTask(string taskId)
        {
            var task = Data.GetOrCreateTask(taskId);
            task.Activate(Data.CurrentRound);
            return task;
        }

        public RuntimeBuildingState UnlockBuilding(string buildingId)
        {
            var building = Data.GetOrCreateBuilding(buildingId);
            var wasUnlocked = building.IsUnlocked;
            building.Unlock();
            if (!wasUnlocked)
            {
                BuildingUnlocked?.Invoke(building);
            }

            return building;
        }

        public RuntimeLetterState ReceiveLetter(string letterId)
        {
            var letter = Data.AddLetter(letterId);
            LetterReceived?.Invoke(letter);
            return letter;
        }

        public bool RemoveLetter(string letterId)
        {
            var removed = Data.RemoveLetter(letterId);
            if (removed)
            {
                LetterRemoved?.Invoke(letterId);
            }

            return removed;
        }

        [ContextMenu("Create New Game With Initial Disaster")]
        public void CreateNewGameWithInitialDisaster()
        {
            CreateNewGame(initialDisasterId);
        }

        [ContextMenu("Refresh Runtime Inspector Snapshot")]
        public void RefreshInspectorSnapshot()
        {
            var taskService = FindFirstObjectByType<TaskService>();
            var documentService = FindFirstObjectByType<DocumentService>();
            var storyService = FindFirstObjectByType<StoryService>();
            var roundService = FindFirstObjectByType<RoundService>();

            var disasterStage = roundService != null ? roundService.CurrentDisasterStage : null;
            inspectorRoundSummary = $"灾难={Data.DisasterId}, 回合={Data.CurrentRound}/{Data.TotalRound}, 灾难阶段={(disasterStage != null ? $"{disasterStage.StageId} {disasterStage.StageName}" : "未解析")}";

            RefreshInspectorCurrentRoundDocuments(documentService, taskService);
            RefreshInspectorFollowUpDocuments(documentService);
            RefreshInspectorRuntimeTasks(taskService);
            RefreshInspectorConfiguredTasks(taskService);
            RefreshInspectorCurrentRoundStories(storyService);
            RefreshInspectorCurrentStory(storyService);
            RefreshInspectorRuntimeBuildings();
        }

        private int GetDisasterTotalRound(string disasterId)
        {
            if (configManager != null &&
                configManager.TryFindRow("DisasterConfig", "DisasterId", disasterId, out var disasterRow))
            {
                return disasterRow.GetInt("TotalRound", 1);
            }

            Debug.LogWarning($"DisasterConfig missing disaster id {disasterId}; using one round.", this);
            return 1;
        }

        private void InitializeConfiguredItems()
        {
            if (configManager == null || !configManager.TryGetTable("ItemConfig", out var itemTable))
            {
                return;
            }

            foreach (var row in itemTable.Rows)
            {
                var itemId = row.GetString("ItemId");
                if (!string.IsNullOrEmpty(itemId))
                {
                    Data.GetOrCreateItem(itemId);
                }
            }
        }

        private void InitializeConfiguredFactions()
        {
            if (configManager == null || !configManager.TryGetTable("FactionConfig", out var factionTable))
            {
                return;
            }

            foreach (var row in factionTable.Rows)
            {
                var factionId = row.GetString("FactionId");
                if (string.IsNullOrEmpty(factionId))
                {
                    continue;
                }

                var faction = Data.GetOrCreateFaction(factionId, row.GetInt("InitSuspicion"));
                faction.SetSuspicion(faction.Suspicion, row.GetInt("MaxSuspicion", 100));
            }
        }

        private void InitializeConfiguredBuildings()
        {
            if (configManager == null || !configManager.TryGetTable("CityBuildingConfig", out var buildingTable))
            {
                return;
            }

            foreach (var row in buildingTable.Rows)
            {
                var buildingId = row.GetString("BuildingId");
                if (string.IsNullOrEmpty(buildingId))
                {
                    continue;
                }

                var building = Data.GetOrCreateBuilding(buildingId);
                if (row.GetBool("DefaultVisible"))
                {
                    building.Unlock();
                }
            }
        }

        private void RefreshInspectorCurrentRoundDocuments(DocumentService documentService, TaskService taskService)
        {
            inspectorCurrentRoundDocuments.Clear();

            foreach (var entry in Data.DocumentQueue.Where(candidate => candidate.QueuedRound <= Data.CurrentRound))
            {
                DocumentDefinition document = null;
                documentService?.TryGetDefinition(entry.DocumentId, out document);
                var taskId = FirstNonEmpty(entry.TaskId, document?.TaskId);
                var taskStageId = FirstNonEmpty(entry.TaskStageId, document?.TaskStageId);
                var currentStageId = GetCurrentTaskStageId(taskService, taskId);

                inspectorCurrentRoundDocuments.Add(new RuntimeInspectorDocumentEntry
                {
                    documentId = entry.DocumentId,
                    title = document?.Title ?? string.Empty,
                    documentType = document?.DocumentType ?? string.Empty,
                    queuedRound = entry.QueuedRound,
                    taskId = taskId,
                    taskStageId = taskStageId,
                    currentTaskStageId = currentStageId,
                    proposerCharacterId = document?.ProposerCharacterId ?? string.Empty,
                    beforeDocumentCharacterId = entry.BeforeDocumentCharacterId,
                    optionANextDocumentId = document?.OptionA.NextDocumentId ?? string.Empty,
                    optionANextDocumentDelayRound = document?.OptionA.NextDocumentDelayRound ?? 0,
                    optionBNextDocumentId = document?.OptionB.NextDocumentId ?? string.Empty,
                    optionBNextDocumentDelayRound = document?.OptionB.NextDocumentDelayRound ?? 0
                });
            }
        }

        private void RefreshInspectorFollowUpDocuments(DocumentService documentService)
        {
            inspectorPendingFollowUpDocuments.Clear();

            foreach (var state in Data.FollowUpDocuments.OrderBy(candidate => candidate.ActivateRound))
            {
                DocumentDefinition document = null;
                DocumentDefinition sourceDocument = null;
                documentService?.TryGetDefinition(state.DocumentId, out document);
                documentService?.TryGetDefinition(state.SourceDocumentId, out sourceDocument);
                inspectorPendingFollowUpDocuments.Add(new RuntimeInspectorFollowUpDocumentEntry
                {
                    documentId = state.DocumentId,
                    title = document?.Title ?? string.Empty,
                    sourceDocumentId = state.SourceDocumentId,
                    sourceTitle = sourceDocument?.Title ?? string.Empty,
                    taskId = state.TaskId,
                    taskStageId = state.TaskStageId,
                    activateRound = state.ActivateRound,
                    beforeDocumentCharacterId = state.BeforeDocumentCharacterId
                });
            }
        }

        private void RefreshInspectorRuntimeTasks(TaskService taskService)
        {
            inspectorRuntimeTasks.Clear();

            foreach (var state in Data.Tasks)
            {
                TaskDefinition definition = null;
                taskService?.TryGetDefinition(state.TaskId, out definition);
                var currentStage = taskService?.GetCurrentStage(state);
                inspectorRuntimeTasks.Add(new RuntimeInspectorTaskEntry
                {
                    taskId = state.TaskId,
                    taskName = definition?.TaskName ?? string.Empty,
                    status = state.Status.ToString(),
                    activatedRound = state.ActivatedRound,
                    completedRound = state.CompletedRound,
                    score = state.Score,
                    currentTaskStageId = currentStage?.TaskStageId ?? string.Empty,
                    currentStageDescription = currentStage?.StageDescription ?? string.Empty,
                    processedStageStarts = string.Join("|", state.ProcessedStageStarts),
                    processedStageEnds = string.Join("|", state.ProcessedStageEnds)
                });
            }
        }

        private void RefreshInspectorConfiguredTasks(TaskService taskService)
        {
            inspectorConfiguredTasks.Clear();
            if (taskService == null)
            {
                return;
            }

            foreach (var definition in taskService.Definitions)
            {
                var stages = taskService.GetStages(definition.TaskId);
                inspectorConfiguredTasks.Add(new RuntimeInspectorTaskConfigEntry
                {
                    taskId = definition.TaskId,
                    taskName = definition.TaskName,
                    taskType = definition.TaskType,
                    startRound = definition.StartRound,
                    endRound = definition.EndRound,
                    successScore = definition.SuccessScore,
                    failScore = definition.FailScore,
                    stageIds = string.Join("|", stages.Select(stage => stage.TaskStageId)),
                    linkedDocumentIds = string.Join("|", stages.SelectMany(stage => stage.LinkedDocumentIds)),
                    storyIds = string.Join("|", stages.SelectMany(GetStageStoryIds).Where(id => !string.IsNullOrEmpty(id)))
                });
            }
        }

        private void RefreshInspectorCurrentRoundStories(StoryService storyService)
        {
            inspectorCurrentRoundStories.Clear();

            foreach (var entry in Data.StoryQueue.Where(candidate => candidate.QueuedRound <= Data.CurrentRound))
            {
                StoryDefinition story = null;
                storyService?.TryGetStory(entry.StoryId, out story);
                inspectorCurrentRoundStories.Add(new RuntimeInspectorStoryEntry
                {
                    storyId = entry.StoryId,
                    storyName = story?.StoryName ?? string.Empty,
                    storyType = story != null ? story.StoryType.ToString() : string.Empty,
                    queuedRound = entry.QueuedRound,
                    timing = entry.Timing.ToString(),
                    taskId = entry.TaskId,
                    taskStageId = entry.TaskStageId,
                    triggerTaskId = story?.TriggerTaskId ?? string.Empty,
                    addItemId = story?.AddItemId ?? string.Empty,
                    addItemCount = story?.AddItemCount ?? 0
                });
            }
        }

        private void RefreshInspectorCurrentStory(StoryService storyService)
        {
            var playback = storyService != null ? storyService.CurrentPlayback : null;
            if (playback == null)
            {
                inspectorCurrentStorySummary = "当前没有正在播放的剧情。";
                return;
            }

            var story = playback.Story;
            var lineId = playback.CurrentLine != null ? playback.CurrentLine.LineId : string.Empty;
            inspectorCurrentStorySummary = $"剧情={story.StoryId} {story.StoryName}, 类型={story.StoryType}, 行={lineId}, 已完成={playback.IsCompleted}, 等待提交={playback.IsWaitingForSubmission}";
        }

        private void RefreshInspectorRuntimeBuildings()
        {
            inspectorRuntimeBuildings.Clear();
            foreach (var state in Data.Buildings)
            {
                inspectorRuntimeBuildings.Add(new RuntimeInspectorBuildingEntry
                {
                    buildingId = state.BuildingId,
                    isUnlocked = state.IsUnlocked,
                    lastCollectedRound = state.LastCollectedRound
                });
            }
        }

        private static string FirstNonEmpty(string first, string second)
        {
            return !string.IsNullOrEmpty(first) ? first : second ?? string.Empty;
        }

        private string GetCurrentTaskStageId(TaskService taskService, string taskId)
        {
            if (taskService == null || string.IsNullOrEmpty(taskId))
            {
                return string.Empty;
            }

            var taskState = Data.Tasks.FirstOrDefault(candidate => candidate.TaskId == taskId);
            return taskService.GetCurrentStage(taskState)?.TaskStageId ?? string.Empty;
        }

        private static IEnumerable<string> GetStageStoryIds(TaskStageDefinition stage)
        {
            yield return stage.StartStoryId;
            yield return stage.BeforeDocumentStoryId;
            yield return stage.EndStoryId;
        }
    }

    [Serializable]
    public sealed class RuntimeInspectorDocumentEntry
    {
        [Header("公文基础信息")]
        public string documentId;
        public string title;
        public string documentType;
        public int queuedRound;

        [Header("所属任务与阶段")]
        public string taskId;
        public string taskStageId;
        public string currentTaskStageId;

        [Header("角色与后续公文")]
        public string proposerCharacterId;
        public string beforeDocumentCharacterId;
        public string optionANextDocumentId;
        public int optionANextDocumentDelayRound;
        public string optionBNextDocumentId;
        public int optionBNextDocumentDelayRound;
    }

    [Serializable]
    public sealed class RuntimeInspectorFollowUpDocumentEntry
    {
        [Header("后续公文")]
        public string documentId;
        public string title;
        public string sourceDocumentId;
        public string sourceTitle;

        [Header("触发条件")]
        public string taskId;
        public string taskStageId;
        public int activateRound;
        public string beforeDocumentCharacterId;
    }

    [Serializable]
    public sealed class RuntimeInspectorTaskEntry
    {
        [Header("运行时任务")]
        public string taskId;
        public string taskName;
        public string status;
        public int activatedRound;
        public int completedRound;
        public int score;

        [Header("当前阶段与已处理阶段")]
        public string currentTaskStageId;
        public string currentStageDescription;
        public string processedStageStarts;
        public string processedStageEnds;
    }

    [Serializable]
    public sealed class RuntimeInspectorTaskConfigEntry
    {
        [Header("任务配置")]
        public string taskId;
        public string taskName;
        public string taskType;
        public int startRound;
        public int endRound;
        public int successScore;
        public int failScore;

        [Header("阶段、关联公文与剧情")]
        public string stageIds;
        public string linkedDocumentIds;
        public string storyIds;
    }

    [Serializable]
    public sealed class RuntimeInspectorStoryEntry
    {
        [Header("剧情队列")]
        public string storyId;
        public string storyName;
        public string storyType;
        public int queuedRound;
        public string timing;

        [Header("来源与奖励")]
        public string taskId;
        public string taskStageId;
        public string triggerTaskId;
        public string addItemId;
        public int addItemCount;
    }

    [Serializable]
    public sealed class RuntimeInspectorBuildingEntry
    {
        [Header("运行时建筑状态")]
        [Tooltip("建筑 ID；来自 RuntimeBuildingState.BuildingId，应与 CityBuildingConfig.BuildingId 一致。")]
        public string buildingId;
        [Tooltip("当前建筑是否已经被公文或默认配置解锁。")]
        public bool isUnlocked;
        [Tooltip("上次领取建筑产出的回合；0 表示尚未领取。")]
        public int lastCollectedRound;
    }
}
