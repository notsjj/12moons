using System;
using System.Collections.Generic;
using System.Linq;
using TwelveMoons.Core.Config;
using UnityEngine;

namespace TwelveMoons.Core.Runtime
{
    public sealed class StoryService : MonoBehaviour
    {
        [Header("依赖服务：配置、运行时、背包和任务")]
        [Tooltip("配置管理器；用于读取 StoryConfig、DialogueConfig 和 CharacterConfig。")]
        [SerializeField] private ConfigManager configManager;
        [Tooltip("运行时数据服务；用于读取剧情队列、保存剧情提交进度和触发任务。")]
        [SerializeField] private RuntimeDataService runtimeDataService;
        [Tooltip("背包服务；用于处理剧情选项和提交道具的消耗或奖励。")]
        [SerializeField] private InventoryService inventoryService;
        [Tooltip("任务服务；用于在剧情结束后按 StoryConfig.TriggerTaskId 激活任务。")]
        [SerializeField] private TaskService taskService;

        [Header("Inspector调试：当前正在播放的剧情类型")]
        [Tooltip("只读观察字段；显示当前剧情类型，没有剧情时显示“无”。")]
        [SerializeField] private string inspectorCurrentStoryType = "无";

        private readonly List<StoryDefinition> stories = new List<StoryDefinition>();
        private readonly Dictionary<string, StoryDefinition> storiesById =
            new Dictionary<string, StoryDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<DialogueLineDefinition>> dialogueLinesByStoryId =
            new Dictionary<string, List<DialogueLineDefinition>>(StringComparer.Ordinal);
        private readonly Dictionary<string, DialogueLineDefinition> dialogueLinesById =
            new Dictionary<string, DialogueLineDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, CharacterDefinition> charactersById =
            new Dictionary<string, CharacterDefinition>(StringComparer.Ordinal);

        public event Action StoryChanged;

        public IReadOnlyList<StoryDefinition> Stories => stories;

        public StoryPlaybackState CurrentPlayback { get; private set; }

        private void Awake()
        {
            ResolveDependencies();
            LoadStoryConfig();
        }

        public void Refresh()
        {
            LoadStoryConfig();
            NotifyStoryChanged();
        }

        public bool TryGetStory(string storyId, out StoryDefinition story)
        {
            return storiesById.TryGetValue(storyId, out story);
        }

        public bool TryGetCharacter(string characterId, out CharacterDefinition character)
        {
            return charactersById.TryGetValue(characterId, out character);
        }

        public bool TryGetItemDefinition(string itemId, out ItemDefinition definition)
        {
            definition = null;
            return inventoryService != null && inventoryService.TryGetDefinition(itemId, out definition);
        }

        public int GetItemCount(string itemId)
        {
            return inventoryService == null ? 0 : inventoryService.GetCount(itemId);
        }

        public bool StartStory(string storyId)
        {
            if (!TryGetStory(storyId, out var story))
            {
                Debug.LogWarning($"StoryId {storyId} is not configured in StoryConfig.", this);
                return false;
            }

            var firstLine = GetStartDialogueLine(story.StoryId);
            if (story.StoryType == StoryType.Dialogue && firstLine == null)
            {
                Debug.LogWarning($"Dialogue story {story.StoryId} has no DialogueConfig lines.", this);
                return false;
            }

            CurrentPlayback = new StoryPlaybackState(story, firstLine);
            RestoreWaitingSubmission(story.StoryId);
            NotifyStoryChanged();
            return true;
        }

        public bool StartNextQueuedStory()
        {
            return StartNextQueuedStory(null);
        }

        public bool StartNextQueuedStory(RuntimeStoryQueueTiming timing)
        {
            return StartNextQueuedStory((RuntimeStoryQueueTiming?)timing);
        }

        private bool StartNextQueuedStory(RuntimeStoryQueueTiming? timing)
        {
            if (runtimeDataService == null)
            {
                Debug.LogWarning("StoryService missing RuntimeDataService.", this);
                return false;
            }

            var currentRound = runtimeDataService.Data.CurrentRound;
            var entry = runtimeDataService.Data.StoryQueue
                .Where(candidate => candidate.QueuedRound <= currentRound &&
                    (!timing.HasValue || candidate.Timing == timing.Value))
                .OrderBy(candidate => GetStoryTimingPriority(candidate.Timing))
                .FirstOrDefault();
            if (entry == null)
            {
                CurrentPlayback = null;
                NotifyStoryChanged();
                return false;
            }

            if (!StartStory(entry.StoryId))
            {
                runtimeDataService.Data.RemoveStoryQueueEntry(entry);
                return StartNextQueuedStory(timing);
            }

            runtimeDataService.Data.RemoveStoryQueueEntry(entry);
            return true;
        }

        public void Continue()
        {
            if (CurrentPlayback == null)
            {
                StartNextQueuedStory();
                return;
            }

            if (CurrentPlayback.IsCompleted)
            {
                CurrentPlayback = null;
                NotifyStoryChanged();
                return;
            }

            var story = CurrentPlayback.Story;
            if (story.StoryType != StoryType.Dialogue)
            {
                ContinuePresentationStory(story);
                return;
            }

            var line = CurrentPlayback.CurrentLine;
            if (line == null)
            {
                CompleteCurrentStory();
                return;
            }

            if (line.IsChoice)
            {
                CurrentPlayback.SetFeedback("Choose an option to continue.");
                NotifyStoryChanged();
                return;
            }

            if (CurrentPlayback.IsWaitingForSubmission || line.IsItemSubmissionLine())
            {
                CurrentPlayback.SetWaitingForSubmission(true);
                CurrentPlayback.SetFeedback("Submit the required items to continue.");
                NotifyStoryChanged();
                return;
            }

            MoveToLineOrComplete(line.GetNextLineId(0));
        }

        public void ChooseOption(int optionIndex)
        {
            if (CurrentPlayback == null ||
                CurrentPlayback.IsCompleted ||
                CurrentPlayback.CurrentLine == null ||
                !CurrentPlayback.CurrentLine.IsChoice)
            {
                return;
            }

            var line = CurrentPlayback.CurrentLine;
            if (!TryApplyChoiceCostAndReward(line, optionIndex))
            {
                NotifyStoryChanged();
                return;
            }

            MoveToLineOrComplete(line.GetNextLineId(optionIndex));
        }

        public void SubmitCurrentItems()
        {
            if (CurrentPlayback == null ||
                CurrentPlayback.IsCompleted ||
                CurrentPlayback.CurrentLine == null ||
                !CurrentPlayback.CurrentLine.IsItemSubmissionLine())
            {
                return;
            }

            var line = CurrentPlayback.CurrentLine;
            if (!HasRequiredItems(line))
            {
                CurrentPlayback.SetFeedback("Missing required items.");
                NotifyStoryChanged();
                return;
            }

            ConsumeRequiredItems(line);
            runtimeDataService?.Data.ClearStoryProgress(CurrentPlayback.Story.StoryId);
            CurrentPlayback.SetWaitingForSubmission(false);
            MoveToLineOrComplete(line.GetNextLineId(0));
        }

        public void ExitItemSubmission()
        {
            if (CurrentPlayback == null ||
                CurrentPlayback.CurrentLine == null ||
                !CurrentPlayback.CurrentLine.IsItemSubmissionLine())
            {
                return;
            }

            runtimeDataService?.Data.SaveStoryProgress(
                CurrentPlayback.Story.StoryId,
                CurrentPlayback.CurrentLine.LineId,
                true);
            CurrentPlayback = null;
            NotifyStoryChanged();
        }

        public void EndCurrentStory()
        {
            if (CurrentPlayback == null)
            {
                return;
            }

            CompleteCurrentStory();
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

            if (taskService == null)
            {
                taskService = FindFirstObjectByType<TaskService>();
            }
        }

        private void LoadStoryConfig()
        {
            stories.Clear();
            storiesById.Clear();
            dialogueLinesByStoryId.Clear();
            dialogueLinesById.Clear();
            charactersById.Clear();

            if (configManager == null)
            {
                Debug.LogWarning("StoryService missing ConfigManager.", this);
                return;
            }

            LoadStoryDefinitions();
            LoadDialogueDefinitions();
            LoadCharacterDefinitions();
        }

        private void LoadStoryDefinitions()
        {
            if (!configManager.TryGetTable("StoryConfig", out var table))
            {
                Debug.LogWarning("StoryService cannot load StoryConfig.", this);
                return;
            }

            foreach (var row in table.Rows)
            {
                var story = new StoryDefinition(row);
                if (string.IsNullOrEmpty(story.StoryId))
                {
                    continue;
                }

                stories.Add(story);
                storiesById[story.StoryId] = story;
            }
        }

        private void LoadDialogueDefinitions()
        {
            if (!configManager.TryGetTable("DialogueConfig", out var table))
            {
                Debug.LogWarning("StoryService cannot load DialogueConfig.", this);
                return;
            }

            foreach (var row in table.Rows)
            {
                var line = new DialogueLineDefinition(row);
                if (string.IsNullOrEmpty(line.LineId) || string.IsNullOrEmpty(line.StoryId))
                {
                    continue;
                }

                dialogueLinesById[line.LineId] = line;
                if (!dialogueLinesByStoryId.TryGetValue(line.StoryId, out var lines))
                {
                    lines = new List<DialogueLineDefinition>();
                    dialogueLinesByStoryId[line.StoryId] = lines;
                }

                lines.Add(line);
            }
        }

        private void LoadCharacterDefinitions()
        {
            if (!configManager.TryGetTable("CharacterConfig", out var table))
            {
                return;
            }

            foreach (var row in table.Rows)
            {
                var character = new CharacterDefinition(row);
                if (!string.IsNullOrEmpty(character.CharacterId))
                {
                    charactersById[character.CharacterId] = character;
                }
            }
        }

        private DialogueLineDefinition GetFirstDialogueLine(string storyId)
        {
            return dialogueLinesByStoryId.TryGetValue(storyId, out var lines) && lines.Count > 0
                ? lines[0]
                : null;
        }

        private DialogueLineDefinition GetStartDialogueLine(string storyId)
        {
            if (runtimeDataService != null &&
                runtimeDataService.Data.TryGetStoryProgress(storyId, out var progress) &&
                progress.WaitingForSubmission &&
                dialogueLinesById.TryGetValue(progress.LineId, out var savedLine))
            {
                return savedLine;
            }

            return GetFirstDialogueLine(storyId);
        }

        private void RestoreWaitingSubmission(string storyId)
        {
            if (CurrentPlayback == null ||
                runtimeDataService == null ||
                !runtimeDataService.Data.TryGetStoryProgress(storyId, out var progress))
            {
                return;
            }

            CurrentPlayback.SetWaitingForSubmission(progress.WaitingForSubmission);
        }

        private void ContinuePresentationStory(StoryDefinition story)
        {
            if (story.StoryType == StoryType.Image)
            {
                var imageCount = Mathf.Max(1, story.ImageIds.Count);
                if (CurrentPlayback.PresentationIndex < imageCount - 1)
                {
                    CurrentPlayback.SetPresentationIndex(CurrentPlayback.PresentationIndex + 1);
                    NotifyStoryChanged();
                    return;
                }

                CompleteCurrentStory();
                return;
            }

            var textCount = Mathf.Max(1, story.TextSegments.Count);
            if (CurrentPlayback.PresentationIndex < textCount - 1)
            {
                CurrentPlayback.SetPresentationIndex(CurrentPlayback.PresentationIndex + 1);
                NotifyStoryChanged();
                return;
            }

            CompleteCurrentStory();
        }

        private void MoveToLineOrComplete(string nextLineId)
        {
            if (IsEndLineId(nextLineId))
            {
                CompleteCurrentStory();
                return;
            }

            if (dialogueLinesById.TryGetValue(nextLineId, out var nextLine))
            {
                CurrentPlayback.SetLine(nextLine);
                NotifyStoryChanged();
                return;
            }

            CurrentPlayback.SetFeedback($"Next dialogue line {nextLineId} is missing.");
            NotifyStoryChanged();
        }

        private static bool IsEndLineId(string lineId)
        {
            return string.IsNullOrWhiteSpace(lineId) ||
                   string.Equals(lineId.Trim(), "END", StringComparison.OrdinalIgnoreCase);
        }

        private bool TryApplyChoiceCostAndReward(DialogueLineDefinition line, int optionIndex)
        {
            var requiredItemId = line.GetRequiredItemId(optionIndex);
            var requiredItemCount = line.GetRequiredItemCount(optionIndex);
            if (!string.IsNullOrEmpty(requiredItemId) &&
                requiredItemCount > 0 &&
                (inventoryService == null || !inventoryService.HasItem(requiredItemId, requiredItemCount)))
            {
                CurrentPlayback.SetFeedback($"Missing required item: {requiredItemId} x{requiredItemCount}.");
                return false;
            }

            if (!string.IsNullOrEmpty(requiredItemId) &&
                requiredItemCount > 0 &&
                line.ShouldConsumeItem(optionIndex))
            {
                inventoryService?.TryRemoveItem(requiredItemId, requiredItemCount);
            }

            var addItemId = line.GetAddItemId(optionIndex);
            var addItemCount = line.GetAddItemCount(optionIndex);
            if (!string.IsNullOrEmpty(addItemId) && addItemCount > 0)
            {
                inventoryService?.AddItem(addItemId, addItemCount);
            }

            return true;
        }

        private bool HasRequiredItems(DialogueLineDefinition line)
        {
            for (var index = 0; index < line.RequiredItemIds.Count; index++)
            {
                var itemId = line.GetRequiredItemId(index);
                var count = line.GetRequiredItemCount(index);
                if (!string.IsNullOrEmpty(itemId) &&
                    count > 0 &&
                    (inventoryService == null || !inventoryService.HasItem(itemId, count)))
                {
                    return false;
                }
            }

            return true;
        }

        private void ConsumeRequiredItems(DialogueLineDefinition line)
        {
            for (var index = 0; index < line.RequiredItemIds.Count; index++)
            {
                var itemId = line.GetRequiredItemId(index);
                var count = line.GetRequiredItemCount(index);
                if (!string.IsNullOrEmpty(itemId) && count > 0 && line.ShouldConsumeItem(index))
                {
                    inventoryService?.TryRemoveItem(itemId, count);
                }
            }
        }

        private void CompleteCurrentStory()
        {
            if (CurrentPlayback == null)
            {
                return;
            }

            var story = CurrentPlayback.Story;
            runtimeDataService?.Data.ClearStoryProgress(story.StoryId);

            if (!string.IsNullOrEmpty(story.AddItemId) && story.AddItemCount > 0)
            {
                if (inventoryService != null && inventoryService.AddItem(story.AddItemId, story.AddItemCount))
                {
                    RecordStorySettlement(story, $"剧情奖励：{GetStoryDisplayName(story)} 获得 {story.AddItemId} x{story.AddItemCount}");
                }
            }

            if (story.TriggerTaskOnEnd && !string.IsNullOrEmpty(story.TriggerTaskId))
            {
                var task = taskService != null ? taskService.ActivateTask(story.TriggerTaskId) : null;
                if (task != null)
                {
                    RecordStorySettlement(story, $"剧情触发任务：{GetStoryDisplayName(story)} -> {story.TriggerTaskId}");
                }
            }

            CurrentPlayback = null;
            NotifyStoryChanged();
        }

        private void NotifyStoryChanged()
        {
            inspectorCurrentStoryType = CurrentPlayback != null
                ? CurrentPlayback.Story.StoryType.ToString()
                : "无";
            StoryChanged?.Invoke();
        }

        private static int GetStoryTimingPriority(RuntimeStoryQueueTiming timing)
        {
            switch (timing)
            {
                case RuntimeStoryQueueTiming.StageEnd:
                    return 0;
                case RuntimeStoryQueueTiming.StageStart:
                    return 1;
                case RuntimeStoryQueueTiming.BeforeDocument:
                    return 2;
                default:
                    return 99;
            }
        }

        private void RecordStorySettlement(StoryDefinition story, string message)
        {
            if (runtimeDataService == null || story == null)
            {
                return;
            }

            runtimeDataService.Data.EnsureNewspaperEntry(runtimeDataService.Data.CurrentRound, message);
        }

        private static string GetStoryDisplayName(StoryDefinition story)
        {
            return string.IsNullOrEmpty(story.StoryName) ? story.StoryId : story.StoryName;
        }
    }
}
