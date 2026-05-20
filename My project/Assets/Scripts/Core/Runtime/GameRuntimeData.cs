using System;
using System.Collections.Generic;
using System.Linq;

namespace TwelveMoons.Core.Runtime
{
    [Serializable]
    public sealed class GameRuntimeData
    {
        private readonly List<RuntimeItemState> items = new List<RuntimeItemState>();
        private readonly List<RuntimeTaskState> tasks = new List<RuntimeTaskState>();
        private readonly List<RuntimeBuildingState> buildings = new List<RuntimeBuildingState>();
        private readonly List<RuntimeLetterState> letters = new List<RuntimeLetterState>();
        private readonly List<RuntimeFactionState> factions = new List<RuntimeFactionState>();
        private readonly List<RuntimeStoryQueueEntry> storyQueue = new List<RuntimeStoryQueueEntry>();
        private readonly List<RuntimeStoryProgressState> storyProgress = new List<RuntimeStoryProgressState>();
        private readonly List<RuntimeDocumentQueueEntry> documentQueue = new List<RuntimeDocumentQueueEntry>();
        private readonly List<RuntimeFollowUpDocumentState> followUpDocuments = new List<RuntimeFollowUpDocumentState>();
        private readonly List<string> processedDocumentDrawKeys = new List<string>();

        public string DisasterId { get; private set; }

        public int CurrentRound { get; private set; }

        public int TotalRound { get; private set; }

        public IReadOnlyList<RuntimeItemState> Items => items;

        public IReadOnlyList<RuntimeTaskState> Tasks => tasks;

        public IReadOnlyList<RuntimeBuildingState> Buildings => buildings;

        public IReadOnlyList<RuntimeLetterState> Letters => letters;

        public IReadOnlyList<RuntimeFactionState> Factions => factions;

        public IReadOnlyList<RuntimeStoryQueueEntry> StoryQueue => storyQueue;

        public IReadOnlyList<RuntimeStoryProgressState> StoryProgress => storyProgress;

        public IReadOnlyList<RuntimeDocumentQueueEntry> DocumentQueue => documentQueue;

        public IReadOnlyList<RuntimeFollowUpDocumentState> FollowUpDocuments => followUpDocuments;

        public IReadOnlyList<string> ProcessedDocumentDrawKeys => processedDocumentDrawKeys;

        public void Reset(string disasterId, int totalRound)
        {
            DisasterId = disasterId;
            CurrentRound = 1;
            TotalRound = Math.Max(1, totalRound);
            items.Clear();
            tasks.Clear();
            buildings.Clear();
            letters.Clear();
            factions.Clear();
            storyQueue.Clear();
            storyProgress.Clear();
            documentQueue.Clear();
            followUpDocuments.Clear();
            processedDocumentDrawKeys.Clear();
        }

        public void SetCurrentRound(int currentRound)
        {
            CurrentRound = Math.Max(1, Math.Min(currentRound, TotalRound));
        }

        public bool TryAdvanceRound()
        {
            if (CurrentRound >= TotalRound)
            {
                return false;
            }

            CurrentRound++;
            return true;
        }

        public RuntimeItemState GetOrCreateItem(string itemId)
        {
            var item = items.FirstOrDefault(candidate => candidate.ItemId == itemId);
            if (item != null)
            {
                return item;
            }

            item = new RuntimeItemState(itemId);
            items.Add(item);
            return item;
        }

        public RuntimeTaskState GetOrCreateTask(string taskId)
        {
            var task = tasks.FirstOrDefault(candidate => candidate.TaskId == taskId);
            if (task != null)
            {
                return task;
            }

            task = new RuntimeTaskState(taskId);
            tasks.Add(task);
            return task;
        }

        public RuntimeBuildingState GetOrCreateBuilding(string buildingId)
        {
            var building = buildings.FirstOrDefault(candidate => candidate.BuildingId == buildingId);
            if (building != null)
            {
                return building;
            }

            building = new RuntimeBuildingState(buildingId);
            buildings.Add(building);
            return building;
        }

        public RuntimeLetterState AddLetter(string letterId)
        {
            var letter = letters.FirstOrDefault(candidate => candidate.LetterId == letterId);
            if (letter != null)
            {
                return letter;
            }

            letter = new RuntimeLetterState(letterId, CurrentRound);
            letters.Add(letter);
            return letter;
        }

        public bool RemoveLetter(string letterId)
        {
            var letter = letters.FirstOrDefault(candidate => candidate.LetterId == letterId);
            return letter != null && letters.Remove(letter);
        }

        public RuntimeFactionState GetOrCreateFaction(string factionId, int initSuspicion)
        {
            var faction = factions.FirstOrDefault(candidate => candidate.FactionId == factionId);
            if (faction != null)
            {
                return faction;
            }

            faction = new RuntimeFactionState(factionId, initSuspicion);
            factions.Add(faction);
            return faction;
        }

        public RuntimeStoryQueueEntry QueueStory(
            string storyId,
            string taskId,
            string taskStageId,
            RuntimeStoryQueueTiming timing)
        {
            var entry = new RuntimeStoryQueueEntry(storyId, taskId, taskStageId, CurrentRound, timing);
            storyQueue.Add(entry);
            return entry;
        }

        public bool RemoveStoryQueueEntry(RuntimeStoryQueueEntry entry)
        {
            return entry != null && storyQueue.Remove(entry);
        }

        public RuntimeStoryProgressState SaveStoryProgress(string storyId, string lineId, bool waitingForSubmission)
        {
            var progress = storyProgress.FirstOrDefault(candidate => candidate.StoryId == storyId);
            if (progress != null)
            {
                progress.SetProgress(lineId, waitingForSubmission);
                return progress;
            }

            progress = new RuntimeStoryProgressState(storyId, lineId, waitingForSubmission);
            storyProgress.Add(progress);
            return progress;
        }

        public bool TryGetStoryProgress(string storyId, out RuntimeStoryProgressState progress)
        {
            progress = storyProgress.FirstOrDefault(candidate => candidate.StoryId == storyId);
            return progress != null;
        }

        public bool ClearStoryProgress(string storyId)
        {
            var progress = storyProgress.FirstOrDefault(candidate => candidate.StoryId == storyId);
            return progress != null && storyProgress.Remove(progress);
        }

        public RuntimeDocumentQueueEntry QueueDocument(
            string documentId,
            string taskId,
            string taskStageId,
            string beforeDocumentCharacterId)
        {
            return QueueDocument(documentId, taskId, taskStageId, beforeDocumentCharacterId, 0);
        }

        public RuntimeDocumentQueueEntry QueueDocument(
            string documentId,
            string taskId,
            string taskStageId,
            string beforeDocumentCharacterId,
            int delayRound)
        {
            var entry = new RuntimeDocumentQueueEntry(
                documentId,
                taskId,
                taskStageId,
                beforeDocumentCharacterId,
                CurrentRound + Math.Max(0, delayRound));
            documentQueue.Add(entry);
            return entry;
        }

        public bool RemoveDocumentQueueEntry(RuntimeDocumentQueueEntry entry)
        {
            return entry != null && documentQueue.Remove(entry);
        }

        public RuntimeFollowUpDocumentState RecordFollowUpDocument(
            string documentId,
            string sourceDocumentId,
            string taskId,
            string taskStageId,
            string beforeDocumentCharacterId,
            int delayRound)
        {
            if (string.IsNullOrEmpty(documentId))
            {
                return null;
            }

            var activateRound = CurrentRound + Math.Max(0, delayRound);
            var existing = followUpDocuments.FirstOrDefault(candidate =>
                candidate.DocumentId == documentId &&
                candidate.SourceDocumentId == (sourceDocumentId ?? string.Empty) &&
                candidate.TaskId == (taskId ?? string.Empty) &&
                candidate.TaskStageId == (taskStageId ?? string.Empty) &&
                candidate.ActivateRound == activateRound);
            if (existing != null)
            {
                return existing;
            }

            var state = new RuntimeFollowUpDocumentState(
                documentId,
                sourceDocumentId,
                taskId,
                taskStageId,
                beforeDocumentCharacterId,
                activateRound);
            followUpDocuments.Add(state);
            return state;
        }

        public int ActivateDueFollowUpDocuments()
        {
            var activated = 0;
            var dueStates = followUpDocuments
                .Where(candidate => candidate.ActivateRound <= CurrentRound)
                .ToList();

            foreach (var state in dueStates)
            {
                if (!documentQueue.Any(candidate =>
                    candidate.DocumentId == state.DocumentId &&
                    candidate.TaskId == state.TaskId &&
                    candidate.TaskStageId == state.TaskStageId))
                {
                    QueueDocument(
                        state.DocumentId,
                        state.TaskId,
                        state.TaskStageId,
                        state.BeforeDocumentCharacterId);
                    activated++;
                }

                followUpDocuments.Remove(state);
            }

            return activated;
        }

        public bool HasProcessedDocumentDraw(string key)
        {
            return !string.IsNullOrEmpty(key) && processedDocumentDrawKeys.Contains(key);
        }

        public void MarkDocumentDrawProcessed(string key)
        {
            if (!string.IsNullOrEmpty(key) && !processedDocumentDrawKeys.Contains(key))
            {
                processedDocumentDrawKeys.Add(key);
            }
        }
    }
}
