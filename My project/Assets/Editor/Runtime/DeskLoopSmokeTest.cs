using System.IO;
using System.Linq;
using System.Reflection;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class DeskLoopSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run Desk Loop Smoke Test")]
        public static void Run()
        {
            var context = CreateContext("DeskLoopSmokeTest");
            try
            {
                PrepareFlow(context);
                LogPlannedFlow(context);

                if (!context.RuntimeDataService.Data.StoryQueue.Any(candidate => candidate.QueuedRound == 1) ||
                    !context.RuntimeDataService.Data.Letters.Any(candidate => candidate.LetterId == "letter_relief_start") ||
                    !context.DocumentService.TryGetNextPendingDocument(out var firstEntry, out var firstDocument) ||
                    firstEntry.DocumentId != "document_relief_prepare")
                {
                    throw new InvalidDataException("第 1 回合没有排入开始剧情、开始信件和第一份任务公文。");
                }

                AssertDocumentRequirements(firstDocument);
                AssertDocumentRequirementText(context, firstDocument);
                DrainStories(context);

                var result = context.DocumentService.ResolveDocument(firstEntry, DocumentOptionType.A);
                if (!result.Success)
                {
                    throw new InvalidDataException($"第 1 回合公文甲选项处理失败：{result.Message}");
                }

                ResolveAllPendingDocuments(context, DocumentOptionType.B);
                context.LoopController.EndCurrentRound();
                DrainStories(context);
                context.LoopController.EndCurrentRound();

                if (context.RuntimeDataService.Data.CurrentRound != 2 ||
                    !context.RuntimeDataService.Data.TryGetNewspaper(1, out var newspaper) ||
                    !newspaper.BuildBodyText().Contains("公文处理") ||
                    !context.RuntimeDataService.Data.DocumentQueue.Any(candidate => candidate.DocumentId == "document_relief_followup" && candidate.QueuedRound <= 2))
                {
                    throw new InvalidDataException("第 1 回合没有结算到第 2 回合，或缺少报纸与到期后续公文。");
                }

                ResolveAllPendingDocuments(context, DocumentOptionType.B);
                context.LoopController.EndCurrentRound();
                DrainStories(context);
                context.LoopController.EndCurrentRound();

                if (context.RuntimeDataService.Data.CurrentRound != 3 ||
                    !context.RuntimeDataService.Data.TryGetNewspaper(2, out _))
                {
                    throw new InvalidDataException("第 2 回合没有带着报纸结算到第 3 回合。");
                }

                DrainStories(context);
                ResolveAllPendingDocuments(context, DocumentOptionType.B);
                context.LoopController.EndCurrentRound();
                DrainStories(context);
                context.LoopController.EndCurrentRound();

                if (context.RuntimeDataService.Data.CurrentRound != 4 ||
                    !context.RuntimeDataService.Data.TryGetNewspaper(3, out _))
                {
                    throw new InvalidDataException("桌面最小循环没有跑完三个完整回合。");
                }

                Debug.Log("桌面最小循环冒烟测试通过：三个非城区回合可以播放队列剧情、处理公文、生成报纸并推进回合。");
            }
            finally
            {
                Object.DestroyImmediate(context.Root);
            }
        }

        private static void PrepareFlow(TestContext context)
        {
            context.ConfigManager.BuildDefaultProviders();
            context.RuntimeDataService.CreateNewGame("disaster_flood_01");
            context.InventoryService.Refresh();
            context.FactionService.Refresh();
            context.RoundService.Refresh();
            context.TaskService.Refresh();
            context.DocumentService.Refresh();
            context.StoryService.Refresh();
            context.InventoryService.AddMoney(20);
            context.InventoryService.AddMaterial(20);
            context.InventoryService.AddFood(20);
            context.LoopController.BeginDocumentFlow();
        }

        private static void DrainStories(TestContext context)
        {
            var guard = 0;
            while ((context.StoryService.CurrentPlayback != null ||
                    context.RuntimeDataService.Data.StoryQueue.Any(candidate => candidate.QueuedRound <= context.RuntimeDataService.Data.CurrentRound)) &&
                   guard < 80)
            {
                context.LoopController.StartOrContinueStoryQueue();
                guard++;
            }

            if (guard >= 80)
            {
                throw new InvalidDataException("剧情队列没有正常清空。");
            }
        }

        private static void ResolveAllPendingDocuments(TestContext context, DocumentOptionType optionType)
        {
            var guard = 0;
            while (context.DocumentService.TryGetNextPendingDocument(out var entry, out _) && guard < 20)
            {
                var result = context.DocumentService.ResolveDocument(entry, optionType);
                if (!result.Success)
                {
                    result = context.DocumentService.ResolveDocument(entry, DocumentOptionType.A);
                }

                if (!result.Success)
                {
                    throw new InvalidDataException($"待处理公文无法结算：{entry.DocumentId}。");
                }

                guard++;
            }

            if (guard >= 20)
            {
                throw new InvalidDataException("公文队列没有正常清空。");
            }
        }

        private static void AssertDocumentRequirements(DocumentDefinition document)
        {
            if (document == null ||
                document.OptionA.MoneyChange >= 0 ||
                string.IsNullOrEmpty(document.OptionA.AddItemId))
            {
                throw new InvalidDataException("测试公文不再覆盖正文中的资源需求显示。");
            }
        }

        private static void AssertDocumentRequirementText(TestContext context, DocumentDefinition document)
        {
            var viewObject = new GameObject("DocumentRequirementTextProbe");
            try
            {
                viewObject.transform.SetParent(context.Root.transform);
                var view = viewObject.AddComponent<DocumentPopupPanelView>();
                var serializedObject = new SerializedObject(view);
                serializedObject.FindProperty("inventoryService").objectReferenceValue = context.InventoryService;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                var method = typeof(DocumentPopupPanelView).GetMethod(
                    "BuildBodyTextWithRequirements",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var text = method?.Invoke(view, new object[] { document }) as string;
                if (string.IsNullOrEmpty(text) ||
                    !text.Contains("所需物品") ||
                    !text.Contains("金币 x5") ||
                    text.Contains("建材 x3"))
                {
                    throw new InvalidDataException("公文正文没有显示配置中的资源需求。");
                }
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
            }
        }

        private static void LogPlannedFlow(TestContext context)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("桌面最小循环测试内容：");
            foreach (var task in context.TaskService.Definitions)
            {
                if (!task.ShowInTaskPanel && string.IsNullOrEmpty(task.StartRound.ToString()))
                {
                    continue;
                }

                builder.AppendLine($"任务：{task.TaskName}（{task.TaskId}），回合 {task.StartRound}-{task.EndRound}，成功分 {task.SuccessScore}，失败分 {task.FailScore}");
                foreach (var stage in context.TaskService.GetStages(task.TaskId))
                {
                    builder.AppendLine($"  阶段：{stage.StageDescription}（{stage.TaskStageId}），相对回合 {stage.StartOffsetRound}-{stage.EndOffsetRound}");
                    AppendStory(builder, context, "开始剧情", stage.StartStoryId);
                    AppendStory(builder, context, "公文前剧情", stage.BeforeDocumentStoryId);
                    AppendStory(builder, context, "结束剧情", stage.EndStoryId);
                    if (!string.IsNullOrEmpty(stage.BeforeDocumentCharacterId))
                    {
                        builder.AppendLine($"    公文前角色：{stage.BeforeDocumentCharacterId}");
                    }

                    foreach (var documentId in stage.LinkedDocumentIds)
                    {
                        if (context.DocumentService.TryGetDefinition(documentId, out var document))
                        {
                            builder.AppendLine($"    阶段公文：{document.Title}（{document.DocumentId}），甲：{document.OptionA.Text}，乙：{document.OptionB.Text}");
                        }
                    }
                }
            }

            foreach (var document in context.DocumentService.Definitions)
            {
                if (document.DocumentType == "Global" || document.DocumentType == "Disaster" || document.DocumentType == "FollowUp")
                {
                    builder.AppendLine($"额外公文：{document.Title}（{document.DocumentId}，{document.DocumentType}），甲：{document.OptionA.Text}，乙：{document.OptionB.Text}");
                }
            }

            Debug.Log(builder.ToString());
        }

        private static void AppendStory(System.Text.StringBuilder builder, TestContext context, string label, string storyId)
        {
            if (string.IsNullOrEmpty(storyId))
            {
                return;
            }

            if (context.StoryService.TryGetStory(storyId, out var story))
            {
                builder.AppendLine($"    {label}：{story.StoryName}（{story.StoryId}，{story.StoryType}）");
            }
            else
            {
                builder.AppendLine($"    {label}：未找到配置 {storyId}");
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
            var storyService = root.AddComponent<StoryService>();
            var controller = root.AddComponent<DeskLoopController>();

            ConfigureConfigManager(configManager);
            ConfigureRuntimeDataService(runtimeDataService, configManager);
            ConfigureInventoryService(inventoryService, configManager, runtimeDataService);
            ConfigureFactionService(factionService, configManager, runtimeDataService);
            ConfigureRoundService(roundService, configManager, runtimeDataService);
            ConfigureTaskService(taskService, configManager, runtimeDataService, roundService);
            ConfigureDocumentService(documentService, configManager, runtimeDataService, inventoryService, factionService, taskService, roundService);
            ConfigureStoryService(storyService, configManager, runtimeDataService, inventoryService, taskService);
            ConfigureLoopController(controller, runtimeDataService, roundService, taskService, storyService, documentService);

            return new TestContext(root, configManager, runtimeDataService, inventoryService, factionService, roundService, taskService, documentService, storyService, controller);
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

        private static void ConfigureStoryService(
            StoryService storyService,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService,
            InventoryService inventoryService,
            TaskService taskService)
        {
            var serializedObject = new SerializedObject(storyService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureLoopController(
            DeskLoopController controller,
            RuntimeDataService runtimeDataService,
            RoundService roundService,
            TaskService taskService,
            StoryService storyService,
            DocumentService documentService)
        {
            var serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("roundService").objectReferenceValue = roundService;
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.FindProperty("storyService").objectReferenceValue = storyService;
            serializedObject.FindProperty("documentService").objectReferenceValue = documentService;
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
                DocumentService documentService,
                StoryService storyService,
                DeskLoopController loopController)
            {
                Root = root;
                ConfigManager = configManager;
                RuntimeDataService = runtimeDataService;
                InventoryService = inventoryService;
                FactionService = factionService;
                RoundService = roundService;
                TaskService = taskService;
                DocumentService = documentService;
                StoryService = storyService;
                LoopController = loopController;
            }

            public GameObject Root { get; }

            public ConfigManager ConfigManager { get; }

            public RuntimeDataService RuntimeDataService { get; }

            public InventoryService InventoryService { get; }

            public FactionService FactionService { get; }

            public RoundService RoundService { get; }

            public TaskService TaskService { get; }

            public DocumentService DocumentService { get; }

            public StoryService StoryService { get; }

            public DeskLoopController LoopController { get; }
        }
    }
}
