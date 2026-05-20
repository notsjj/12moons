using System;
using TwelveMoons.Core.Config;
using UnityEngine;

namespace TwelveMoons.Core.Runtime
{
    public sealed class RuntimeDataService : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private ConfigManager configManager;

        [Header("Startup")]
        [SerializeField] private bool createNewGameOnAwake;
        [SerializeField] private string initialDisasterId = "disaster_flood_01";

        public GameRuntimeData Data { get; } = new GameRuntimeData();

        public event Action<RuntimeLetterState> LetterReceived;

        public event Action<string> LetterRemoved;

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

        public void CreateNewGame(string disasterId)
        {
            var totalRound = GetDisasterTotalRound(disasterId);
            Data.Reset(disasterId, totalRound);
            InitializeConfiguredItems();
            InitializeConfiguredFactions();

            Debug.Log(
                $"Runtime data initialized. Disaster={Data.DisasterId}, Round={Data.CurrentRound}/{Data.TotalRound}, Items={Data.Items.Count}, Factions={Data.Factions.Count}.",
                this);
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
            building.Unlock();
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
    }
}
