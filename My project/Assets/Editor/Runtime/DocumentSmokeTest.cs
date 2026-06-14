using System.IO;
using System.Linq;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class DocumentSmokeTest
    {
        private const string DemoDocumentId = "document_relief_prepare";
        private const string DemoTaskId = "task_demo_relief_01";
        private const string DemoStageId = "task_stage_relief_prepare";

        [MenuItem("Twelve Moons/Tests/Run Document Smoke Test")]
        public static void Run()
        {
            RunOptionAFlow();
            RunOptionBFlow();
            RunCurrentRoundDrawFlow();
            ValidateActorTransitionApi();
            ValidatePopupExitDragPrefab();
            Debug.Log("Document flow smoke test passed. Demo document queues, opens, resolves proposer, settles option A and B, removes the current queue entry, records delayed follow-up documents, activates them on their due round, and keeps drag-exit popup wiring.");
        }

        private static void ValidateActorTransitionApi()
        {
            var root = new GameObject("DocumentActorTransitionSmokeTest");
            try
            {
                var actor = root.AddComponent<SharedActorSlotView>();
                actor.ShowActor("Next proposer", "Document proposer", null, null);
                actor.HideAlongEntryPath(null);

                var popup = root.AddComponent<DocumentPopupPanelView>();
                _ = popup.IsActorTransitioning;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ValidatePopupExitDragPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/DocumentPopupPanel.prefab");
            if (prefab == null)
            {
                throw new InvalidDataException("DocumentPopupPanel prefab not found.");
            }

            var popup = prefab.GetComponent<DocumentPopupPanelView>();
            if (popup == null)
            {
                throw new InvalidDataException("DocumentPopupPanel prefab is missing DocumentPopupPanelView.");
            }

            if (popup.ExitHintImageObject == null)
            {
                throw new InvalidDataException("DocumentPopupPanel prefab must bind the exit hint image.");
            }

            if (popup.ExitHintImageObject.activeSelf)
            {
                throw new InvalidDataException("Document exit hint image must be inactive by default.");
            }

            if (popup.RightSideOffscreenOffset <= 0f)
            {
                throw new InvalidDataException("DocumentPopupPanel must slide in from a positive right-side offset.");
            }

            if (popup is not IBeginDragHandler || popup is not IDragHandler || popup is not IEndDragHandler)
            {
                throw new InvalidDataException("DocumentPopupPanelView must handle drag events for right-side exit.");
            }
        }

        private static void RunOptionAFlow()
        {
            var context = CreateContext("DocumentSmokeTest_OptionA");
            try
            {
                PrepareFlow(context);
                AssertPendingDocument(context, out var entry, out _);

                var result = context.DocumentService.ResolveDocument(entry, DocumentOptionType.A);
                if (!result.Success)
                {
                    throw new InvalidDataException($"Document option A failed: {result.Message}");
                }

                var data = context.RuntimeDataService.Data;
                var task = data.Tasks.FirstOrDefault(candidate => candidate.TaskId == DemoTaskId);
                var building = data.Buildings.FirstOrDefault(candidate => candidate.BuildingId == "building_relief_depot");
                var nextDocument = data.FollowUpDocuments.FirstOrDefault(candidate => candidate.DocumentId == "document_relief_followup");

                if (context.InventoryService.GetCount("item_money") != 5 ||
                    context.InventoryService.GetCount("item_material") != 10 ||
                    context.InventoryService.GetCount("item_food") != 5 ||
                    context.InventoryService.GetCount("item_drainage_map") != 1 ||
                    task == null ||
                    task.Score != 1 ||
                    building == null ||
                    !building.IsUnlocked ||
                    nextDocument == null ||
                    nextDocument.ActivateRound != data.CurrentRound + 1 ||
                    context.FactionService.GetSuspicion("civilian") != 48 ||
                    context.FactionService.GetSuspicion("noble") != 51 ||
                    string.IsNullOrEmpty(result.Message) ||
                    string.IsNullOrEmpty(result.FactionFeedbackText))
                {
                    throw new InvalidDataException("Document option A flow failed after settlement.");
                }

                if (data.DocumentQueue.Any(candidate => candidate.DocumentId == DemoDocumentId))
                {
                    throw new InvalidDataException("Document option A did not remove the current document queue entry.");
                }

                if (data.DocumentQueue.Any(candidate => candidate.DocumentId == "document_relief_followup"))
                {
                    throw new InvalidDataException("Document option A added the delayed follow-up to the active queue too early.");
                }
            }
            finally
            {
                Object.DestroyImmediate(context.Root);
            }
        }

        private static void RunOptionBFlow()
        {
            var context = CreateContext("DocumentSmokeTest_OptionB");
            try
            {
                PrepareFlow(context);
                AssertPendingDocument(context, out var entry, out var definition);

                var result = context.DocumentService.ResolveDocument(entry, DocumentOptionType.B);
                if (!result.Success)
                {
                    throw new InvalidDataException($"Document option B failed: {result.Message}");
                }

                var data = context.RuntimeDataService.Data;
                var task = data.Tasks.FirstOrDefault(candidate => candidate.TaskId == DemoTaskId);
                if (context.InventoryService.GetCount("item_money") != 10 ||
                    context.InventoryService.GetCount("item_material") != 10 ||
                    context.InventoryService.GetCount("item_food") != 5 ||
                    context.InventoryService.GetCount("item_drainage_map") != 0 ||
                    task == null ||
                    task.Score != -1 ||
                    data.Buildings.Any(candidate => candidate.BuildingId == "building_relief_depot" && candidate.IsUnlocked) ||
                    data.DocumentQueue.Any(candidate => candidate.DocumentId == DemoDocumentId) ||
                    context.FactionService.GetSuspicion("civilian") != 53 ||
                    context.FactionService.GetSuspicion("academy") != 51 ||
                    result.Message == definition.OptionA.ResultText ||
                    result.FactionFeedbackText == definition.OptionA.FactionFeedbackText)
                {
                    throw new InvalidDataException("Document option B flow failed after settlement.");
                }
            }
            finally
            {
                Object.DestroyImmediate(context.Root);
            }
        }

        private static void RunCurrentRoundDrawFlow()
        {
            var context = CreateContext("DocumentSmokeTest_DrawQueue");
            try
            {
                PrepareFlow(context);

                var data = context.RuntimeDataService.Data;
                AssertQueued(data, DemoDocumentId, "Task", "current task stage");
                if (CountQueuedDocumentsByType(context, "Global") != 2 ||
                    CountQueuedDocumentsByType(context, "Disaster") != 3 ||
                    data.DocumentQueue.Count(candidate => candidate.QueuedRound <= data.CurrentRound) != 6)
                {
                    throw new InvalidDataException("Document draw did not build a six-document current-round queue with two global documents and disaster documents filling the remaining slots.");
                }

                var firstRoundCount = data.DocumentQueue.Count;
                var addedOnRepeat = context.DocumentService.GenerateCurrentRoundDocumentQueue();
                if (addedOnRepeat != 0 || data.DocumentQueue.Count != firstRoundCount)
                {
                    throw new InvalidDataException("Document draw generated duplicate non-repeatable documents in the same round.");
                }

                var taskEntry = data.DocumentQueue.First(candidate => candidate.DocumentId == DemoDocumentId);
                var result = context.DocumentService.ResolveDocument(taskEntry, DocumentOptionType.A);
                if (!result.Success)
                {
                    throw new InvalidDataException($"Document draw option A failed: {result.Message}");
                }

                if (data.DocumentQueue.Any(candidate => candidate.DocumentId == "document_relief_followup") ||
                    !data.FollowUpDocuments.Any(candidate => candidate.DocumentId == "document_relief_followup" && candidate.ActivateRound == data.CurrentRound + 1))
                {
                    throw new InvalidDataException("Delayed follow-up document was not recorded for the next round.");
                }

                context.RoundService.NextRound();
                context.TaskService.ProcessCurrentRound();
                context.DocumentService.GenerateCurrentRoundDocumentQueue();

                if (!data.DocumentQueue.Any(candidate => candidate.DocumentId == "document_relief_followup" && candidate.QueuedRound <= data.CurrentRound))
                {
                    throw new InvalidDataException("Delayed follow-up document was not available on its due round.");
                }

                if (data.FollowUpDocuments.Any(candidate => candidate.DocumentId == "document_relief_followup"))
                {
                    throw new InvalidDataException("Delayed follow-up document remained in follow-up state after activation.");
                }
            }
            finally
            {
                Object.DestroyImmediate(context.Root);
            }
        }

        private static TestContext CreateContext(string name)
        {
            var root = new GameObject(name);
            var configManager = root.AddComponent<ConfigManager>();
            var runtimeDataService = root.AddComponent<RuntimeDataService>();
            var inventoryService = root.AddComponent<InventoryService>();
            var factionService = root.AddComponent<FactionService>();
            var roundService = root.AddComponent<RoundService>();
            var taskService = root.AddComponent<TaskService>();
            var documentService = root.AddComponent<DocumentService>();

            ConfigureConfigManager(configManager);
            ConfigureRuntimeDataService(runtimeDataService, configManager);
            ConfigureInventoryService(inventoryService, configManager, runtimeDataService);
            ConfigureFactionService(factionService, configManager, runtimeDataService);
            ConfigureRoundService(roundService, configManager, runtimeDataService);
            ConfigureTaskService(taskService, configManager, runtimeDataService, roundService);
            ConfigureDocumentService(documentService, configManager, runtimeDataService, inventoryService, factionService, taskService, roundService);

            return new TestContext(
                root,
                configManager,
                runtimeDataService,
                inventoryService,
                factionService,
                roundService,
                taskService,
                documentService);
        }

        private static void PrepareFlow(TestContext context)
        {
            context.ConfigManager.BuildDefaultProviders();
            AssertDocumentConfigLoads(context.ConfigManager);
            context.RuntimeDataService.CreateNewGame("disaster_flood_01");
            context.InventoryService.Refresh();
            context.FactionService.Refresh();
            context.RoundService.Refresh();
            context.TaskService.Refresh();
            context.DocumentService.Refresh();

            context.InventoryService.AddMoney(10);
            context.InventoryService.AddMaterial(10);
            context.InventoryService.AddFood(5);
        }

        private static void AssertPendingDocument(
            TestContext context,
            out RuntimeDocumentQueueEntry entry,
            out DocumentDefinition definition)
        {
            if (!context.DocumentService.TryGetNextPendingDocument(out entry, out definition) ||
                entry.DocumentId != DemoDocumentId ||
                entry.TaskId != DemoTaskId ||
                entry.TaskStageId != DemoStageId ||
                string.IsNullOrEmpty(definition.Title) ||
                string.IsNullOrEmpty(definition.BodyText) ||
                string.IsNullOrEmpty(definition.OptionA.Text) ||
                string.IsNullOrEmpty(definition.OptionB.Text))
            {
                throw new InvalidDataException("Document flow failed to read the first pending document.");
            }

            if (!context.DocumentService.TryGetCharacter(definition.ProposerCharacterId, out var proposer) ||
                string.IsNullOrEmpty(proposer.CharacterName))
            {
                throw new InvalidDataException("Document flow failed to resolve the proposer character.");
            }
        }

        private static void AssertDocumentConfigLoads(ConfigManager configManager)
        {
            if (!configManager.TryGetTable("DocumentConfig", out var table) ||
                !table.TryFindById("DocumentId", DemoDocumentId, out _) ||
                !table.TryFindById("DocumentId", "document_flood_watch", out _) ||
                !table.TryFindById("DocumentId", "document_market_notice", out _) ||
                !table.TryFindById("DocumentId", "document_market_roster", out _))
            {
                throw new InvalidDataException("DocumentConfig missing document smoke test demo documents.");
            }
        }

        private static int CountQueuedDocumentsByType(TestContext context, string documentType)
        {
            var count = 0;
            foreach (var entry in context.RuntimeDataService.Data.DocumentQueue)
            {
                if (entry.QueuedRound <= context.RuntimeDataService.Data.CurrentRound &&
                    context.DocumentService.TryGetDefinition(entry.DocumentId, out var definition) &&
                    definition.DocumentType == documentType)
                {
                    count++;
                }
            }

            return count;
        }

        private static void AssertQueued(GameRuntimeData data, string documentId, string documentType, string label)
        {
            if (!data.DocumentQueue.Any(candidate => candidate.DocumentId == documentId))
            {
                throw new InvalidDataException($"Document draw did not queue {label}: {documentId} ({documentType}).");
            }
        }

        private static void ConfigureConfigManager(ConfigManager configManager)
        {
            var serializedObject = new SerializedObject(configManager);
            serializedObject.FindProperty("relativeConfigDirectory").stringValue = "Configs/Demo";
            serializedObject.FindProperty("loadOnAwake").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRuntimeDataService(RuntimeDataService runtimeDataService, ConfigManager configManager)
        {
            var serializedObject = new SerializedObject(runtimeDataService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("createNewGameOnAwake").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureInventoryService(InventoryService inventoryService, ConfigManager configManager, RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(inventoryService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureFactionService(FactionService factionService, ConfigManager configManager, RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(factionService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRoundService(RoundService roundService, ConfigManager configManager, RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(roundService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTaskService(TaskService taskService, ConfigManager configManager, RuntimeDataService runtimeDataService, RoundService roundService)
        {
            var serializedObject = new SerializedObject(taskService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("roundService").objectReferenceValue = roundService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDocumentService(
            DocumentService documentService,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService,
            InventoryService inventoryService,
            FactionService factionService,
            TaskService taskService,
            RoundService roundService)
        {
            var serializedObject = new SerializedObject(documentService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.FindProperty("factionService").objectReferenceValue = factionService;
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.FindProperty("roundService").objectReferenceValue = roundService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class TestContext
        {
            public TestContext(
                GameObject root,
                ConfigManager configManager,
                RuntimeDataService runtimeDataService,
                InventoryService inventoryService,
                FactionService factionService,
                RoundService roundService,
                TaskService taskService,
                DocumentService documentService)
            {
                Root = root;
                ConfigManager = configManager;
                RuntimeDataService = runtimeDataService;
                InventoryService = inventoryService;
                FactionService = factionService;
                RoundService = roundService;
                TaskService = taskService;
                DocumentService = documentService;
            }

            public GameObject Root { get; }

            public ConfigManager ConfigManager { get; }

            public RuntimeDataService RuntimeDataService { get; }

            public InventoryService InventoryService { get; }

            public FactionService FactionService { get; }

            public RoundService RoundService { get; }

            public TaskService TaskService { get; }

            public DocumentService DocumentService { get; }
        }
    }
}
