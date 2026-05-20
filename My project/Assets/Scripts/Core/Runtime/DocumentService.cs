using System;
using System.Collections.Generic;
using TwelveMoons.Core.Config;
using UnityEngine;

namespace TwelveMoons.Core.Runtime
{
    public sealed class DocumentService : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private ConfigManager configManager;
        [SerializeField] private RuntimeDataService runtimeDataService;
        [SerializeField] private InventoryService inventoryService;
        [SerializeField] private FactionService factionService;
        [SerializeField] private TaskService taskService;

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

        public void Refresh()
        {
            LoadConfigs();
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

        public DocumentResolutionResult ResolveDocument(RuntimeDocumentQueueEntry entry, DocumentOptionType optionType)
        {
            if (entry == null)
            {
                return Fail("No document is selected.");
            }

            if (!TryGetDefinition(entry.DocumentId, out var document))
            {
                return Fail($"DocumentConfig missing document id {entry.DocumentId}.");
            }

            var option = document.GetOption(optionType);
            if (!CanAffordOption(option, out var failReason))
            {
                return Fail(failReason);
            }

            ApplyResourceChange(InventoryItemType.Money, option.MoneyChange);
            ApplyResourceChange(InventoryItemType.Material, option.MaterialChange);
            ApplyResourceChange(InventoryItemType.Food, option.FoodChange);
            ApplyRequiredItem(option);
            ApplyAddedItem(option);
            ApplySuspicion(option);
            ApplyTaskScore(document, option);
            ApplyBuildingUnlock(option);
            QueueNextDocument(entry, option);
            runtimeDataService.Data.RemoveDocumentQueueEntry(entry);
            DocumentsChanged?.Invoke();

            var feedback = string.IsNullOrEmpty(option.FactionFeedbackText)
                ? option.ProposerFeedbackText
                : $"{option.ProposerFeedbackText}\n{option.FactionFeedbackText}";
            return new DocumentResolutionResult(true, option.ResultText, option.ProposerFeedbackText, feedback);
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
        }

        private void LoadConfigs()
        {
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
            if (!configManager.TryGetTable("DocumentConfig", out var table))
            {
                Debug.LogWarning("DocumentService cannot load DocumentConfig.", this);
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
            if (!configManager.TryGetTable("CharacterConfig", out var table))
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

        private bool CanAffordOption(DocumentOptionDefinition option, out string failReason)
        {
            failReason = string.Empty;
            if (inventoryService == null)
            {
                failReason = "DocumentService missing InventoryService.";
                return false;
            }

            if (!CanAffordResource(InventoryItemType.Money, option.MoneyChange, out failReason) ||
                !CanAffordResource(InventoryItemType.Material, option.MaterialChange, out failReason) ||
                !CanAffordResource(InventoryItemType.Food, option.FoodChange, out failReason))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(option.RequiredItemId) &&
                option.RequiredItemCount > 0 &&
                !inventoryService.HasItem(option.RequiredItemId, option.RequiredItemCount))
            {
                failReason = $"Required item is missing: {option.RequiredItemId} x{option.RequiredItemCount}.";
                return false;
            }

            return true;
        }

        private bool CanAffordResource(InventoryItemType itemType, int delta, out string failReason)
        {
            failReason = string.Empty;
            if (delta >= 0)
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

        private void ApplyResourceChange(InventoryItemType itemType, int delta)
        {
            if (delta == 0 || inventoryService == null)
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

        private void ApplyRequiredItem(DocumentOptionDefinition option)
        {
            if (option.ConsumeItem &&
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
            }
        }

        private void QueueNextDocument(RuntimeDocumentQueueEntry entry, DocumentOptionDefinition option)
        {
            if (!string.IsNullOrEmpty(option.NextDocumentId))
            {
                runtimeDataService.Data.QueueDocument(
                    option.NextDocumentId,
                    entry.TaskId,
                    entry.TaskStageId,
                    entry.BeforeDocumentCharacterId,
                    option.NextDocumentDelayRound);
            }
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
