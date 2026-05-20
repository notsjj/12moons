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
    }
}
