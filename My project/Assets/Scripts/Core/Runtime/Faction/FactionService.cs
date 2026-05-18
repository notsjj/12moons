using System;
using System.Collections.Generic;
using TwelveMoons.Core.Config;
using UnityEngine;

namespace TwelveMoons.Core.Runtime
{
    public sealed class FactionService : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private ConfigManager configManager;
        [SerializeField] private RuntimeDataService runtimeDataService;

        [Header("Thresholds")]
        [SerializeField] private int highSuspicionReduceValue = 30;

        private readonly List<FactionDefinition> definitions = new List<FactionDefinition>();
        private readonly Dictionary<string, FactionDefinition> definitionsById =
            new Dictionary<string, FactionDefinition>(StringComparer.Ordinal);

        public event Action FactionsChanged;

        public event Action<FactionThresholdResult> ThresholdTriggered;

        public IReadOnlyList<FactionDefinition> Definitions => definitions;

        private void Awake()
        {
            ResolveDependencies();
            LoadFactionConfig();
        }

        private void Start()
        {
            EnsureConfiguredRuntimeFactions();
            CheckAllThresholds();
            NotifyFactionsChanged();
        }

        public void Refresh()
        {
            LoadFactionConfig();
            EnsureConfiguredRuntimeFactions();
            CheckAllThresholds();
            NotifyFactionsChanged();
        }

        public bool TryGetDefinition(string factionId, out FactionDefinition definition)
        {
            return definitionsById.TryGetValue(factionId, out definition);
        }

        public int GetSuspicion(string factionId)
        {
            return runtimeDataService == null
                ? 0
                : runtimeDataService.Data.GetOrCreateFaction(factionId, 0).Suspicion;
        }

        public RuntimeFactionState ChangeSuspicion(string factionId, int delta)
        {
            if (!TryGetUsableFaction(factionId, out var definition))
            {
                return null;
            }

            var state = runtimeDataService.Data.GetOrCreateFaction(factionId, definition.InitSuspicion);
            state.AddSuspicion(delta, definition.MaxSuspicion);
            CheckThresholds(definition, state);
            NotifyFactionsChanged();
            return state;
        }

        public RuntimeFactionState SetSuspicion(string factionId, int value)
        {
            if (!TryGetUsableFaction(factionId, out var definition))
            {
                return null;
            }

            var state = runtimeDataService.Data.GetOrCreateFaction(factionId, definition.InitSuspicion);
            state.SetSuspicion(value, definition.MaxSuspicion);
            CheckThresholds(definition, state);
            NotifyFactionsChanged();
            return state;
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

        private void LoadFactionConfig()
        {
            definitions.Clear();
            definitionsById.Clear();

            if (configManager == null || !configManager.TryGetTable("FactionConfig", out var factionTable))
            {
                Debug.LogWarning("FactionService cannot load FactionConfig.", this);
                return;
            }

            foreach (var row in factionTable.Rows)
            {
                var definition = new FactionDefinition(row);
                if (string.IsNullOrEmpty(definition.FactionId))
                {
                    continue;
                }

                definitions.Add(definition);
                definitionsById[definition.FactionId] = definition;
            }
        }

        private void EnsureConfiguredRuntimeFactions()
        {
            if (runtimeDataService == null)
            {
                Debug.LogWarning("FactionService missing RuntimeDataService.", this);
                return;
            }

            foreach (var definition in definitions)
            {
                var state = runtimeDataService.Data.GetOrCreateFaction(definition.FactionId, definition.InitSuspicion);
                state.SetSuspicion(state.Suspicion, definition.MaxSuspicion);
            }
        }

        private bool TryGetUsableFaction(string factionId, out FactionDefinition definition)
        {
            definition = null;
            if (runtimeDataService == null)
            {
                Debug.LogWarning("FactionService missing RuntimeDataService.", this);
                return false;
            }

            if (TryGetDefinition(factionId, out definition))
            {
                return true;
            }

            Debug.LogWarning($"FactionId {factionId} is not configured in FactionConfig.", this);
            return false;
        }

        private void CheckAllThresholds()
        {
            if (runtimeDataService == null)
            {
                return;
            }

            foreach (var definition in definitions)
            {
                var state = runtimeDataService.Data.GetOrCreateFaction(definition.FactionId, definition.InitSuspicion);
                CheckThresholds(definition, state);
            }
        }

        private void CheckThresholds(FactionDefinition definition, RuntimeFactionState state)
        {
            var lowLetterId = TryGrantLowSuspicionLetter(definition, state);
            var punishTaskId = TryActivateHighSuspicionTask(definition, state);

            if (!string.IsNullOrEmpty(lowLetterId) || !string.IsNullOrEmpty(punishTaskId))
            {
                ThresholdTriggered?.Invoke(new FactionThresholdResult(definition.FactionId, lowLetterId, punishTaskId));
            }
        }

        private string TryGrantLowSuspicionLetter(FactionDefinition definition, RuntimeFactionState state)
        {
            if (state.LowSuspicionLetterGranted ||
                state.Suspicion > definition.LowSuspicionThreshold ||
                string.IsNullOrEmpty(definition.LowSuspicionLetterId))
            {
                return string.Empty;
            }

            runtimeDataService.ReceiveLetter(definition.LowSuspicionLetterId);
            state.MarkLowSuspicionLetterGranted();
            Debug.Log(
                $"Low suspicion letter granted. Faction={definition.FactionId}, Letter={definition.LowSuspicionLetterId}, Suspicion={state.Suspicion}.",
                this);
            return definition.LowSuspicionLetterId;
        }

        private string TryActivateHighSuspicionTask(FactionDefinition definition, RuntimeFactionState state)
        {
            if (state.Suspicion < definition.HighSuspicionThreshold ||
                string.IsNullOrEmpty(definition.PunishTaskId))
            {
                return string.Empty;
            }

            runtimeDataService.ActivateTask(definition.PunishTaskId);
            state.AddSuspicion(-Mathf.Max(0, highSuspicionReduceValue), definition.MaxSuspicion);
            Debug.Log(
                $"High suspicion punishment triggered. Faction={definition.FactionId}, Task={definition.PunishTaskId}, SuspicionReducedTo={state.Suspicion}.",
                this);
            return definition.PunishTaskId;
        }

        private void NotifyFactionsChanged()
        {
            FactionsChanged?.Invoke();
        }
    }
}
