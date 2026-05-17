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

        public string DisasterId { get; private set; }

        public int CurrentRound { get; private set; }

        public int TotalRound { get; private set; }

        public IReadOnlyList<RuntimeItemState> Items => items;

        public IReadOnlyList<RuntimeTaskState> Tasks => tasks;

        public IReadOnlyList<RuntimeBuildingState> Buildings => buildings;

        public IReadOnlyList<RuntimeLetterState> Letters => letters;

        public void Reset(string disasterId, int totalRound)
        {
            DisasterId = disasterId;
            CurrentRound = 1;
            TotalRound = Math.Max(1, totalRound);
            items.Clear();
            tasks.Clear();
            buildings.Clear();
            letters.Clear();
        }

        public void SetCurrentRound(int currentRound)
        {
            CurrentRound = Math.Max(1, Math.Min(currentRound, TotalRound));
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
    }
}
