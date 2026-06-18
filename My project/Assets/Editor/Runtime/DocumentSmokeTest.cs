using System.IO;
using System.Linq;
using System.Reflection;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
            RunPointerTargetUsesMostAffectedFactionFlow();
            RunCurrentRoundDrawFlow();
            RunCurrentRoundQueueExhaustionFlow();
            ValidateActorTransitionApi();
            ValidatePopupExitDragPrefab();
            ValidatePopupExitDragDoesNotReverseSource();
            ValidateCloseVisualGuardApi();
            ValidateItemSubmitPanelApi();
            ValidateDocumentFactionLogoApi();
            Debug.Log("Document flow smoke test passed. Demo document queues, opens, resolves proposer, settles option A and B, removes the current queue entry, records delayed follow-up documents, activates them on their due round, and keeps drag-exit popup wiring.");
        }

        private static void ValidateDocumentFactionLogoApi()
        {
            var logoProperty = typeof(DocumentDefinition).GetProperty(
                "FactionLogoName",
                BindingFlags.Instance | BindingFlags.Public);
            if (logoProperty == null)
            {
                throw new InvalidDataException("DocumentDefinition must expose FactionLogoName from DocumentConfig so the popup can choose one of the eight faction logo sprites.");
            }

            var logoImageField = typeof(DocumentPopupPanelView).GetField(
                "factionLogoImage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var logoPathField = typeof(DocumentPopupPanelView).GetField(
                "factionLogoResourceRoot",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (logoImageField == null || logoPathField == null)
            {
                throw new InvalidDataException("DocumentPopupPanelView must bind the faction logo Image and the Resources root used to load Chinese-named logo sprites.");
            }

            var row = new ConfigRow(new System.Collections.Generic.Dictionary<string, string>
            {
                ["DocumentId"] = "document_logo_test",
                ["Title"] = "Logo Test",
                ["FactionLogoName"] = "贵族",
            });
            var definition = new DocumentDefinition(row);
            if (definition.FactionLogoName != "贵族")
            {
                throw new InvalidDataException("DocumentDefinition must read FactionLogoName exactly as the Chinese sprite key from DocumentConfig.");
            }
        }

        private static void ValidateItemSubmitPanelApi()
        {
            var showMethod = typeof(InventoryPanelView).GetMethod(
                "ShowForDocumentSubmission",
                BindingFlags.Instance | BindingFlags.Public);
            var hideMethod = typeof(InventoryPanelView).GetMethod(
                "HideForDocumentSubmission",
                BindingFlags.Instance | BindingFlags.Public);
            if (showMethod == null || hideMethod == null)
            {
                throw new InvalidDataException("InventoryPanelView must expose document-submission pop-up methods.");
            }

            var popupInventoryProperty = typeof(DocumentPopupPanelView).GetProperty(
                "InventoryPanelObject",
                BindingFlags.Instance | BindingFlags.Public);
            if (popupInventoryProperty == null)
            {
                throw new InvalidDataException("DocumentPopupPanelView must expose its bound inventory panel object for smoke-test inspection.");
            }

            var submittedPreviewProperty = typeof(DocumentSubmitSlot).GetProperty(
                "SubmittedCardPreviewObject",
                BindingFlags.Instance | BindingFlags.Public);
            if (submittedPreviewProperty == null)
            {
                throw new InvalidDataException("DocumentSubmitSlot must expose the submitted-card preview object for smoke-test inspection.");
            }

            var tryAcceptMethod = typeof(DocumentSubmitSlot).GetMethod(
                "TryAcceptCard",
                BindingFlags.Instance | BindingFlags.Public);
            var releaseOverlapMethod = typeof(DocumentSubmitSlot).GetMethod(
                "CanReceiveReleasedCard",
                BindingFlags.Instance | BindingFlags.Public);
            if (tryAcceptMethod == null || releaseOverlapMethod == null)
            {
                throw new InvalidDataException("DocumentSubmitSlot must expose release-overlap acceptance methods so card release can submit even when normal OnDrop is blocked by panel layering.");
            }

            var prefabField = typeof(DocumentPopupPanelView).GetField(
                "inventoryPanelPrefab",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var resourcePathField = typeof(DocumentPopupPanelView).GetField(
                "inventoryPanelResourcePath",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var ensureMethod = typeof(DocumentPopupPanelView).GetMethod(
                "EnsureInventoryPanelInstance",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (prefabField == null || resourcePathField == null || ensureMethod == null)
            {
                throw new InvalidDataException("DocumentPopupPanelView must create the inventory panel from prefab when item submission starts.");
            }

            var inventoryPanelPrefab = AssetDatabase.LoadAssetAtPath<InventoryPanelView>("Assets/Resources/Prefabs/UI/物品面板.prefab");
            if (inventoryPanelPrefab == null)
            {
                throw new InvalidDataException("Inventory panel prefab must exist at Assets/Resources/Prefabs/UI/物品面板.prefab.");
            }
        }

        private static void ValidateCloseVisualGuardApi()
        {
            var method = typeof(DocumentPopupPanelView).GetMethod(
                "HideClosedDocumentVisuals",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidDataException("DocumentPopupPanelView must hide closed document visuals before ending the flow to prevent a one-frame flash.");
            }

            var rootVisibilityMethod = typeof(DocumentPopupPanelView).GetMethod(
                "SetRootCanvasVisible",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (rootVisibilityMethod == null)
            {
                throw new InvalidDataException("DocumentPopupPanelView must hide its root CanvasGroup before ending the flow to prevent closed-panel flashing.");
            }
        }

        private static void ValidateActorTransitionApi()
        {
            var root = new GameObject("DocumentActorTransitionSmokeTest");
            try
            {
                var actor = root.AddComponent<SharedActorSlotView>();
                actor.ShowActor("Next proposer", "Document proposer", null, null);
                actor.HideAlongEntryPath(null);
                if (actor.SlideDuration > 0.35f)
                {
                    throw new InvalidDataException($"Document actor movement must be faster after completion, got slide duration {actor.SlideDuration}.");
                }

                if (actor.HiddenMoveLeftDistance < 560f)
                {
                    throw new InvalidDataException($"Document actor hidden-left position must be farther left, got distance {actor.HiddenMoveLeftDistance}.");
                }

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
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/公文弹窗面板.prefab");
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

            if (!popup.AllowsMainInterfaceMaskAutoBinding)
            {
                throw new InvalidDataException("DocumentPopupPanelView must support binding an existing main interface mask.");
            }

            var deskPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/桌面面板.prefab");
            var mainMask = FindChild(deskPanelPrefab != null ? deskPanelPrefab.transform : null, "主界面遮罩");
            if (mainMask == null)
            {
                throw new InvalidDataException("DeskPanel prefab must contain the existing main interface mask.");
            }

            if (mainMask.gameObject.activeSelf)
            {
                throw new InvalidDataException("DeskPanel main interface mask must be inactive by default.");
            }

            if (popup.MainInterfaceMaskTargetAlpha < 0.69f || popup.MainInterfaceMaskTargetAlpha > 0.71f)
            {
                throw new InvalidDataException($"Document main interface mask target alpha must be 0.7, got {popup.MainInterfaceMaskTargetAlpha}.");
            }

            ValidateSharedHudRoundPanelVisibilitySource();

            if (popup.ExitHintImageObject.activeSelf)
            {
                throw new InvalidDataException("Document exit hint image must be inactive by default.");
            }

            if (!popup.ExitHintStartsImmediatelyAfterAllDocuments)
            {
                throw new InvalidDataException("Document exit hint must not depend on the actor exit tween callback.");
            }

            AssertExitHintOnlyShowsAfterAllDocuments(popup);

            if (popup.RightSideOffscreenOffset <= 0f)
            {
                throw new InvalidDataException("DocumentPopupPanel must slide in from a positive right-side offset.");
            }

            if (popup is not IBeginDragHandler || popup is not IDragHandler || popup is not IEndDragHandler)
            {
                throw new InvalidDataException("DocumentPopupPanelView must handle drag events for right-side exit.");
            }

            if (popup.BodyTypewriterCharactersPerSecond <= 0f ||
                popup.FeedbackTypewriterCharactersPerSecond <= 0f ||
                !popup.HidesOptionsUntilBodyTypewriterFinished ||
                popup.FeedbackHoldAfterTypewriterDuration < 1f)
            {
                throw new InvalidDataException("DocumentPopupPanelView must configure body and feedback typewriter gating.");
            }

            AssertMaskDoesNotFadeInAgainWhileAlreadyVisible(popup);
        }

        private static void ValidatePopupExitDragDoesNotReverseSource()
        {
            var sourcePath = "Assets/Scripts/UI/DocumentPopupPanelView.cs";
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("缺少 DocumentPopupPanelView 脚本。");
            }

            var source = File.ReadAllText(sourcePath);
            if (!source.Contains("BuildNonReversingCloseTarget") ||
                !source.Contains("Mathf.Max(closedPosition.x, currentPosition.x)"))
            {
                throw new InvalidDataException("DocumentPopupPanelView must avoid tweening the document popup backward when the player drags past the close position.");
            }
        }

        private static void ValidateSharedHudRoundPanelVisibilitySource()
        {
            var sourcePath = "Assets/Scripts/UI/BaseSceneUIBootstrap.cs";
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("缺少 BaseSceneUIBootstrap 脚本。");
            }

            var source = File.ReadAllText(sourcePath);
            if (!source.Contains("RegisterDocumentPopupStateListener") ||
                !source.Contains("popup.DocumentFlowStateChanged += HandleDocumentPopupStateChanged") ||
                !source.Contains("SetSharedHudRoundPanelVisibility(!isDocumentFlowActive)") ||
                !source.Contains("roundPanel.gameObject.SetActive(showRoundPanel)") ||
                !source.Contains("公文打开时隐藏共享 HUD 回合面板"))
            {
                throw new InvalidDataException("公文打开时必须隐藏共享 HUD 下的回合面板，公文关闭后必须恢复显示。");
            }
        }

        private static void AssertExitHintOnlyShowsAfterAllDocuments(DocumentPopupPanelView popup)
        {
            var showMethod = typeof(DocumentPopupPanelView).GetMethod(
                "ShowExitHint",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var beginWaitMethod = typeof(DocumentPopupPanelView).GetMethod(
                "BeginDragExitWait",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (showMethod == null || beginWaitMethod == null)
            {
                throw new InvalidDataException("Document exit hint flow methods are missing.");
            }

            showMethod.Invoke(popup, null);
            if (popup.ExitHintImageObject.activeSelf)
            {
                throw new InvalidDataException("Document exit hint must remain hidden before all documents finish.");
            }

            beginWaitMethod.Invoke(popup, null);
            if (!popup.ExitHintImageObject.activeSelf)
            {
                throw new InvalidDataException("Document exit hint must show after all documents finish.");
            }
        }

        private static void AssertMaskDoesNotFadeInAgainWhileAlreadyVisible(DocumentPopupPanelView popup)
        {
            var maskObject = new GameObject("主界面遮罩_测试", typeof(RectTransform), typeof(Image));
            try
            {
                var maskImage = maskObject.GetComponent<Image>();
                maskImage.color = new Color(1f, 1f, 1f, popup.MainInterfaceMaskTargetAlpha);
                maskObject.SetActive(true);

                typeof(DocumentPopupPanelView)
                    .GetField("mainInterfaceMaskImage", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(popup, maskImage);

                var method = typeof(DocumentPopupPanelView).GetMethod(
                    "ShowMainInterfaceMask",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (method == null)
                {
                    throw new InvalidDataException("Document main interface mask show method not found.");
                }

                method.Invoke(popup, null);
                if (maskImage.color.a < popup.MainInterfaceMaskTargetAlpha - 0.01f)
                {
                    throw new InvalidDataException("Document main interface mask must not reset alpha when switching documents.");
                }
            }
            finally
            {
                Object.DestroyImmediate(maskObject);
            }
        }

        private static Transform FindChild(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                var result = FindChild(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
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
                    context.FactionService.GetSuspicion("civilian") != 49 ||
                    context.FactionService.GetSuspicion("noble") != 52 ||
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
                    context.FactionService.GetSuspicion("civilian") != 51 ||
                    context.FactionService.GetSuspicion("academy") != 52 ||
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

        private static void RunPointerTargetUsesMostAffectedFactionFlow()
        {
            var context = CreateContext("DocumentSmokeTest_PointerTargetFaction");
            try
            {
                PrepareFlow(context);
                foreach (var entry in context.RuntimeDataService.Data.DocumentQueue.ToArray())
                {
                    context.RuntimeDataService.Data.RemoveDocumentQueueEntry(entry);
                }

                var queued = context.DocumentService.QueueDocument("document_flood_levee");
                var result = context.DocumentService.ResolveDocument(queued, DocumentOptionType.A);
                if (!result.Success)
                {
                    throw new InvalidDataException($"Document pointer target option failed: {result.Message}");
                }

                if (result.FeedbackFactionId != "civilian")
                {
                    throw new InvalidDataException($"Document feedback faction should remain civilian, got {result.FeedbackFactionId}.");
                }

                if (result.MostAffectedFactionId != "academy")
                {
                    throw new InvalidDataException($"Document pointer target should use most affected faction academy, got {result.MostAffectedFactionId}.");
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

        private static void RunCurrentRoundQueueExhaustionFlow()
        {
            var context = CreateContext("DocumentSmokeTest_QueueExhaustion");
            try
            {
                PrepareFlow(context);

                var data = context.RuntimeDataService.Data;
                foreach (var entry in data.DocumentQueue.ToArray())
                {
                    data.RemoveDocumentQueueEntry(entry);
                }

                if (context.DocumentService.TryGetNextPendingDocument(out _, out _))
                {
                    throw new InvalidDataException(
                        "Querying the next document refilled additional documents after the current-round queue was exhausted.");
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

            foreach (var item in context.RuntimeDataService.Data.Items)
            {
                item.SetCount(0);
            }

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
