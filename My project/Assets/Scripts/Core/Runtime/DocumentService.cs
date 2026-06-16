using System;
using System.Collections.Generic;
using TwelveMoons.City;
using TwelveMoons.Core.Config;
using UnityEngine;

namespace TwelveMoons.Core.Runtime
{
    public sealed class DocumentService : MonoBehaviour
    {
        [Header("依赖服务：配置、运行时、物品、阵营、任务和回合")]
        [Tooltip("配置管理器；用于读取 DocumentConfig 和 CharacterConfig。")]
        [SerializeField] private ConfigManager configManager;
        [Tooltip("运行时数据服务；用于读取当前回合、任务、公文队列和后续公文。")]
        [SerializeField] private RuntimeDataService runtimeDataService;
        [Tooltip("背包服务；用于处理公文选项的资源与道具消耗或奖励。")]
        [SerializeField] private InventoryService inventoryService;
        [Tooltip("阵营服务；用于处理公文选项带来的质疑度变化。")]
        [SerializeField] private FactionService factionService;
        [Tooltip("任务服务；用于读取当前任务阶段和处理公文造成的任务分数变化。")]
        [SerializeField] private TaskService taskService;
        [Tooltip("回合服务；用于判断当前灾难阶段并在回合变化时刷新公文队列。")]
        [SerializeField] private RoundService roundService;

        [Header("抽取规则：每回合公文数量限制")]
        [Tooltip("当前回合最多进入待处理队列的公文数量。")]
        [SerializeField] private int maxDocumentsPerRound = 6;
        [Tooltip("每回合优先抽取的全局公文数量。")]
        [SerializeField] private int globalDocumentsPerRound = 2;

        private readonly List<DocumentDefinition> definitions = new List<DocumentDefinition>();
        private readonly Dictionary<string, DocumentDefinition> definitionsById =
            new Dictionary<string, DocumentDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, CharacterDefinition> charactersById =
            new Dictionary<string, CharacterDefinition>(StringComparer.Ordinal);

        public event Action DocumentsChanged;

        public IReadOnlyList<DocumentDefinition> Definitions => definitions;

        private void Awake()
        {
            ResolveDependencies();
            LoadConfigs();
        }

        private void OnEnable()
        {
            if (roundService != null)
            {
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

        private void Start()
        {
            GenerateCurrentRoundDocumentQueue();
        }

        public void Refresh()
        {
            LoadConfigs();
            GenerateCurrentRoundDocumentQueue();
            DocumentsChanged?.Invoke();
        }

        public bool TryGetDefinition(string documentId, out DocumentDefinition definition)
        {
            return definitionsById.TryGetValue(documentId, out definition);
        }

        public bool TryGetCharacter(string characterId, out CharacterDefinition definition)
        {
            return charactersById.TryGetValue(characterId, out definition);
        }

        public bool TryGetNextPendingDocument(out RuntimeDocumentQueueEntry entry, out DocumentDefinition definition)
        {
            entry = null;
            definition = null;

            if (runtimeDataService == null)
            {
                return false;
            }

            LoadConfigs();

            var currentRound = runtimeDataService.Data.CurrentRound;
            foreach (var candidate in runtimeDataService.Data.DocumentQueue)
            {
                if (candidate.QueuedRound > currentRound)
                {
                    continue;
                }

                if (TryGetDefinition(candidate.DocumentId, out definition))
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }

        [ContextMenu("Generate Current Round Document Queue")]
        public int GenerateCurrentRoundDocumentQueue()
        {
            if (runtimeDataService == null)
            {
                return 0;
            }

            var added = 0;
            added += QueueCurrentTaskDocuments();
            added += QueueDueFollowUpDocuments();

            var remainingSlots = GetRemainingCurrentRoundSlots();
            if (remainingSlots > 0)
            {
                added += QueueMatchedDocuments("Global", Mathf.Min(globalDocumentsPerRound, remainingSlots), IsGlobalDocument);
            }

            remainingSlots = GetRemainingCurrentRoundSlots();
            if (remainingSlots > 0)
            {
                added += QueueMatchedDocuments("Disaster", remainingSlots, MatchesCurrentDisasterStage);
            }

            if (added > 0)
            {
                DocumentsChanged?.Invoke();
            }

            return added;
        }

        private void HandleRoundChanged()
        {
            GenerateCurrentRoundDocumentQueue();
        }

        public DocumentResolutionResult ResolveDocument(RuntimeDocumentQueueEntry entry, DocumentOptionType optionType)
        {
            return ResolveDocument(entry, optionType, false);
        }

        public DocumentResolutionResult ResolveDocument(
            RuntimeDocumentQueueEntry entry,
            DocumentOptionType optionType,
            bool requiredItemAlreadySubmitted)
        {
            LoadConfigs();
            if (entry == null)
            {
                return Fail("No document is selected.");
            }

            if (!TryGetDefinition(entry.DocumentId, out var document))
            {
                return Fail($"DocumentConfig missing document id {entry.DocumentId}.");
            }

            var option = document.GetOption(optionType);
            if (!CanAffordOption(option, requiredItemAlreadySubmitted, out var failReason))
            {
                return Fail(failReason);
            }

            var feedbackFactionId = GetFeedbackFactionId(option);
            ApplyResourceChange(InventoryItemType.Money, option.MoneyChange, requiredItemAlreadySubmitted && option.MoneyChange < 0);
            ApplyResourceChange(InventoryItemType.Material, option.MaterialChange, requiredItemAlreadySubmitted && option.MaterialChange < 0);
            ApplyResourceChange(InventoryItemType.Food, option.FoodChange, requiredItemAlreadySubmitted && option.FoodChange < 0);
            ApplyRequiredItem(option, requiredItemAlreadySubmitted);
            ApplyAddedItem(option);
            ApplySuspicion(option);
            ApplyTaskScore(document, option);
            ApplyBuildingUnlock(option);
            QueueNextDocument(entry, option);
            RecordDocumentSettlement(document, option);
            runtimeDataService.Data.RemoveDocumentQueueEntry(entry);
            DocumentsChanged?.Invoke();

            return new DocumentResolutionResult(true, option.ResultText, option.ProposerFeedbackText, option.FactionFeedbackText, feedbackFactionId);
        }

        public RuntimeDocumentQueueEntry QueueDocument(string documentId, string taskId = "", string taskStageId = "", string beforeDocumentCharacterId = "", int delayRound = 0)
        {
            if (runtimeDataService == null || string.IsNullOrEmpty(documentId))
            {
                return null;
            }

            var entry = runtimeDataService.Data.QueueDocument(documentId, taskId, taskStageId, beforeDocumentCharacterId, delayRound);
            DocumentsChanged?.Invoke();
            return entry;
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

            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<InventoryService>();
            }

            if (factionService == null)
            {
                factionService = FindFirstObjectByType<FactionService>();
            }

            if (taskService == null)
            {
                taskService = FindFirstObjectByType<TaskService>();
            }

            if (roundService == null)
            {
                roundService = FindFirstObjectByType<RoundService>();
            }
        }

        private void LoadConfigs()
        {
#if UNITY_EDITOR
            GeneratedGameDataSynchronizer.GenerateDocumentAndCharacterAssets(configManager);
#endif
            definitions.Clear();
            definitionsById.Clear();
            charactersById.Clear();

            if (configManager == null)
            {
                Debug.LogWarning("DocumentService missing ConfigManager.", this);
                return;
            }

            LoadDocuments();
            LoadCharacters();
        }

        private void LoadDocuments()
        {
            ConfigTable table;
            try
            {
                table = configManager.LoadTable("DocumentConfig");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"DocumentService cannot load DocumentConfig. {exception.Message}", this);
                return;
            }

            foreach (var row in table.Rows)
            {
                var definition = new DocumentDefinition(row);
                if (string.IsNullOrEmpty(definition.DocumentId))
                {
                    continue;
                }

                definitions.Add(definition);
                definitionsById[definition.DocumentId] = definition;
            }
        }

        private void LoadCharacters()
        {
            ConfigTable table;
            try
            {
                table = configManager.LoadTable("CharacterConfig");
            }
            catch
            {
                return;
            }

            foreach (var row in table.Rows)
            {
                var definition = new CharacterDefinition(row);
                if (!string.IsNullOrEmpty(definition.CharacterId))
                {
                    charactersById[definition.CharacterId] = definition;
                }
            }
        }

        private bool CanAffordOption(DocumentOptionDefinition option, bool requiredItemAlreadySubmitted, out string failReason)
        {
            failReason = string.Empty;
            if (inventoryService == null)
            {
                failReason = "DocumentService missing InventoryService.";
                return false;
            }

            if (!CanAffordResource(InventoryItemType.Money, option.MoneyChange, requiredItemAlreadySubmitted, out failReason) ||
                !CanAffordResource(InventoryItemType.Material, option.MaterialChange, requiredItemAlreadySubmitted, out failReason) ||
                !CanAffordResource(InventoryItemType.Food, option.FoodChange, requiredItemAlreadySubmitted, out failReason))
            {
                return false;
            }

            if (!requiredItemAlreadySubmitted &&
                !string.IsNullOrEmpty(option.RequiredItemId) &&
                option.RequiredItemCount > 0 &&
                !inventoryService.HasItem(option.RequiredItemId, option.RequiredItemCount))
            {
                failReason = $"Required item is missing: {option.RequiredItemId} x{option.RequiredItemCount}.";
                return false;
            }

            return true;
        }

        private bool CanAffordResource(InventoryItemType itemType, int delta, bool alreadySubmitted, out string failReason)
        {
            failReason = string.Empty;
            if (delta >= 0)
            {
                return true;
            }

            if (alreadySubmitted)
            {
                return true;
            }

            var definition = FindItemDefinition(itemType);
            if (definition != null && inventoryService.HasItem(definition.ItemId, -delta))
            {
                return true;
            }

            failReason = $"Not enough {itemType}: need {-delta}.";
            return false;
        }

        private void ApplyResourceChange(InventoryItemType itemType, int delta, bool alreadySubmitted)
        {
            if (delta == 0 || inventoryService == null)
            {
                return;
            }

            if (alreadySubmitted)
            {
                return;
            }

            if (delta > 0)
            {
                inventoryService.AddByType(itemType, delta);
                return;
            }

            inventoryService.TryRemoveByType(itemType, -delta);
        }

        private void ApplyRequiredItem(DocumentOptionDefinition option, bool requiredItemAlreadySubmitted)
        {
            if (!requiredItemAlreadySubmitted &&
                option.ConsumeItem &&
                !string.IsNullOrEmpty(option.RequiredItemId) &&
                option.RequiredItemCount > 0)
            {
                inventoryService.TryRemoveItem(option.RequiredItemId, option.RequiredItemCount);
            }
        }

        private void ApplyAddedItem(DocumentOptionDefinition option)
        {
            if (!string.IsNullOrEmpty(option.AddItemId) && option.AddItemCount > 0)
            {
                inventoryService.AddItem(option.AddItemId, option.AddItemCount);
            }
        }

        private void ApplySuspicion(DocumentOptionDefinition option)
        {
            ChangeSuspicion("noble", option.NobleSuspicionChange);
            ChangeSuspicion("academy", option.AcademySuspicionChange);
            ChangeSuspicion("church", option.ChurchSuspicionChange);
            ChangeSuspicion("civilian", option.CivilianSuspicionChange);
        }

        private static string GetFeedbackFactionId(DocumentOptionDefinition option)
        {
            return !string.IsNullOrEmpty(option.FeedbackFactionId)
                ? option.FeedbackFactionId
                : GetMostAffectedFactionId(option);
        }

        private static string GetMostAffectedFactionId(DocumentOptionDefinition option)
        {
            var factionId = string.Empty;
            var maxAbsDelta = 0;
            SetIfGreater("noble", option.NobleSuspicionChange, ref factionId, ref maxAbsDelta);
            SetIfGreater("academy", option.AcademySuspicionChange, ref factionId, ref maxAbsDelta);
            SetIfGreater("church", option.ChurchSuspicionChange, ref factionId, ref maxAbsDelta);
            SetIfGreater("civilian", option.CivilianSuspicionChange, ref factionId, ref maxAbsDelta);
            return factionId;
        }

        private static void SetIfGreater(string candidateFactionId, int delta, ref string factionId, ref int maxAbsDelta)
        {
            var absDelta = Mathf.Abs(delta);
            if (absDelta > maxAbsDelta)
            {
                maxAbsDelta = absDelta;
                factionId = candidateFactionId;
            }
        }

        private void ChangeSuspicion(string factionId, int delta)
        {
            if (delta != 0)
            {
                factionService?.ChangeSuspicion(factionId, delta);
            }
        }

        private void ApplyTaskScore(DocumentDefinition document, DocumentOptionDefinition option)
        {
            var taskId = string.IsNullOrEmpty(document.TaskId) ? string.Empty : document.TaskId;
            if (!string.IsNullOrEmpty(taskId) && option.TaskScoreChange != 0)
            {
                taskService?.AddTaskScore(taskId, option.TaskScoreChange);
            }
        }

        private void ApplyBuildingUnlock(DocumentOptionDefinition option)
        {
            if (!string.IsNullOrEmpty(option.UnlockBuildingId))
            {
                runtimeDataService?.UnlockBuilding(option.UnlockBuildingId);
                RefreshCityBuildingViews();
            }
        }

        private static void RefreshCityBuildingViews()
        {
            foreach (var service in FindObjectsByType<CityBuildingService>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                service?.Refresh();
            }

            foreach (var registry in FindObjectsByType<CityBuildingRegistry>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                registry?.RefreshAndBind();
            }
        }

        private void QueueNextDocument(RuntimeDocumentQueueEntry entry, DocumentOptionDefinition option)
        {
            if (string.IsNullOrEmpty(option.NextDocumentId))
            {
                return;
            }

            if (!TryGetDefinition(option.NextDocumentId, out _))
            {
                Debug.LogWarning($"DocumentConfig missing follow-up document id {option.NextDocumentId}.", this);
                return;
            }

            runtimeDataService.Data.RecordFollowUpDocument(
                option.NextDocumentId,
                entry.DocumentId,
                entry.TaskId,
                entry.TaskStageId,
                entry.BeforeDocumentCharacterId,
                option.NextDocumentDelayRound);
        }

        private void RecordDocumentSettlement(DocumentDefinition document, DocumentOptionDefinition option)
        {
            if (runtimeDataService == null || document == null || option == null)
            {
                return;
            }

            var title = string.IsNullOrEmpty(document.Title) ? document.DocumentId : document.Title;
            var resultText = string.IsNullOrEmpty(option.ResultText) ? option.Text : option.ResultText;
            runtimeDataService.Data.EnsureNewspaperEntry(
                runtimeDataService.Data.CurrentRound,
                $"公文处理：{title} - {resultText}");
        }

        private int QueueCurrentTaskDocuments()
        {
            if (taskService == null || runtimeDataService == null)
            {
                return 0;
            }

            var added = 0;
            foreach (var taskState in runtimeDataService.Data.Tasks)
            {
                if (taskState.Status != TaskRuntimeStatus.Active)
                {
                    continue;
                }

                var stage = taskService.GetCurrentStage(taskState);
                if (stage == null)
                {
                    continue;
                }

                foreach (var documentId in stage.LinkedDocumentIds)
                {
                    if (TryGetDefinition(documentId, out var definition) &&
                        QueueDocumentIfEligible(
                            definition,
                            "Task",
                            taskState.TaskId,
                            stage.TaskStageId,
                            stage.BeforeDocumentCharacterId))
                    {
                        added++;
                    }
                }
            }

            return added;
        }

        private int QueueDueFollowUpDocuments()
        {
            return runtimeDataService != null
                ? runtimeDataService.Data.ActivateDueFollowUpDocuments()
                : 0;
        }

        private int QueueMatchedDocuments(
            string drawSource,
            int maxCount,
            Predicate<DocumentDefinition> predicate)
        {
            if (maxCount <= 0 || predicate == null)
            {
                return 0;
            }

            var added = 0;
            var candidates = new List<DocumentDefinition>();
            foreach (var definition in definitions)
            {
                if (predicate(definition))
                {
                    candidates.Add(definition);
                }
            }

            if (candidates.Count == 0)
            {
                return 0;
            }

            var startIndex = GetRoundDrawStartIndex(drawSource, candidates.Count);
            for (var offset = 0; offset < candidates.Count && added < maxCount; offset++)
            {
                var definition = candidates[(startIndex + offset) % candidates.Count];

                if (QueueDocumentIfEligible(
                    definition,
                    drawSource,
                    definition.TaskId,
                    definition.TaskStageId,
                    string.Empty))
                {
                    added++;
                }
            }

            return added;
        }

        private int GetRemainingCurrentRoundSlots()
        {
            if (runtimeDataService == null)
            {
                return 0;
            }

            var currentRound = runtimeDataService.Data.CurrentRound;
            var dueCount = 0;
            foreach (var entry in runtimeDataService.Data.DocumentQueue)
            {
                if (entry.QueuedRound <= currentRound)
                {
                    dueCount++;
                }
            }

            return Mathf.Max(0, maxDocumentsPerRound - dueCount);
        }

        private int GetRoundDrawStartIndex(string drawSource, int candidateCount)
        {
            if (runtimeDataService == null || candidateCount <= 0)
            {
                return 0;
            }

            var seed = runtimeDataService.Data.CurrentRound;
            if (!string.IsNullOrEmpty(drawSource))
            {
                foreach (var character in drawSource)
                {
                    seed = (seed * 31) + character;
                }
            }

            return Mathf.Abs(seed) % candidateCount;
        }

        private bool QueueDocumentIfEligible(
            DocumentDefinition definition,
            string drawSource,
            string taskId,
            string taskStageId,
            string beforeDocumentCharacterId)
        {
            if (definition == null || runtimeDataService == null)
            {
                return false;
            }

            var drawKey = MakeDrawKey(definition, drawSource);
            if (runtimeDataService.Data.HasProcessedDocumentDraw(drawKey))
            {
                return false;
            }

            runtimeDataService.Data.MarkDocumentDrawProcessed(drawKey);
            if (HasQueuedDocument(definition.DocumentId, taskId, taskStageId))
            {
                return false;
            }

            runtimeDataService.Data.QueueDocument(
                definition.DocumentId,
                taskId,
                taskStageId,
                beforeDocumentCharacterId);
            return true;
        }

        private bool HasQueuedDocument(string documentId, string taskId, string taskStageId)
        {
            foreach (var entry in runtimeDataService.Data.DocumentQueue)
            {
                if (entry.DocumentId == documentId &&
                    entry.TaskId == (taskId ?? string.Empty) &&
                    entry.TaskStageId == (taskStageId ?? string.Empty))
                {
                    return true;
                }
            }

            return false;
        }

        private bool MatchesCurrentDisasterStage(DocumentDefinition definition)
        {
            if (!IsDocumentType(definition, "Disaster") || runtimeDataService == null)
            {
                return false;
            }

            var data = runtimeDataService.Data;
            if (!string.IsNullOrEmpty(definition.DisasterId) &&
                definition.DisasterId != data.DisasterId)
            {
                return false;
            }

            if (string.IsNullOrEmpty(definition.DisasterStageId))
            {
                return true;
            }

            var stage = roundService != null
                ? roundService.ResolveDisasterStage(data.CurrentRound)
                : null;
            return stage != null && stage.StageId == definition.DisasterStageId;
        }

        private static bool IsGlobalDocument(DocumentDefinition definition)
        {
            return IsDocumentType(definition, "Global");
        }

        private static bool IsDocumentType(DocumentDefinition definition, string documentType)
        {
            return definition != null &&
                string.Equals(definition.DocumentType, documentType, StringComparison.OrdinalIgnoreCase);
        }

        private string MakeDrawKey(DocumentDefinition definition, string drawSource)
        {
            if (definition.IsRepeatable)
            {
                var round = runtimeDataService != null ? runtimeDataService.Data.CurrentRound : 0;
                return $"{drawSource}:{round}:{definition.DocumentId}";
            }

            return $"{drawSource}:{definition.DocumentId}";
        }

        private ItemDefinition FindItemDefinition(InventoryItemType itemType)
        {
            if (inventoryService == null)
            {
                return null;
            }

            foreach (var definition in inventoryService.Definitions)
            {
                if (definition.ItemType == itemType)
                {
                    return definition;
                }
            }

            return null;
        }

        private static DocumentResolutionResult Fail(string message)
        {
            return new DocumentResolutionResult(false, message, message, message);
        }
    }
}
