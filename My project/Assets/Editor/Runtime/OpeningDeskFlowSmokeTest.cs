using System.IO;
using System.Reflection;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class OpeningDeskFlowSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run Opening Desk Flow Smoke Test")]
        public static void Run()
        {
            ValidateS0001DoesNotAutoAdvanceToS0002();
            ValidateS0001EndsBackAtDeskAndShowsBeforeDocumentActor();
            ValidateOpeningTutorialHandlesBeforeGenericBlackFade();

            Debug.Log("Opening desk flow smoke test passed. S0001 returns to the desk, waits for the before-document actor click, then proceeds to S0002, document flow, and city entry in order.");
        }

        private static void ValidateS0001DoesNotAutoAdvanceToS0002()
        {
            var root = new GameObject("OpeningDeskFlow_AutoAdvanceTest");
            try
            {
                var storyService = root.AddComponent<StoryService>();
                var bootstrap = root.AddComponent<BaseSceneUIBootstrap>();
                var controller = root.AddComponent<DeskLoopController>();
                SetPrivateField(controller, "storyService", storyService);
                SetPrivateField(controller, "uiBootstrap", bootstrap);
                SetPrivateField(controller, "openingTutorialSawS0001", true);
                SetPrivateField(controller, "openingTutorialS0002Started", false);

                var method = typeof(DeskLoopController).GetMethod(
                    "TryHandleOpeningTutorialStoryChanged",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (method == null)
                {
                    throw new InvalidDataException("??? DeskLoopController.TryHandleOpeningTutorialStoryChanged????? S0001 ?????????");
                }

                var handled = (bool)method.Invoke(controller, null);
                if (!handled)
                {
                    throw new InvalidDataException("S0001 ????????????????????????????? S0002?");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ValidateS0001EndsBackAtDeskAndShowsBeforeDocumentActor()
        {
            var root = new GameObject("OpeningDeskFlow_NormalOrderTest");
            try
            {
                var context = CreateContext(root);
                context.RuntimeDataService.Data.QueueStory("S0002", "T0001", "TS0001", RuntimeStoryQueueTiming.BeforeDocument);
                context.RuntimeDataService.Data.QueueDocument("D0001", "T0001", "TS0001", "C0028");
                SetPrivateField(context.Controller, "openingTutorialSawS0001", true);
                SetPrivateField(context.Controller, "waitingForBeforeDocumentActorClick", false);
                SetPrivateField(context.Controller, "pendingBeforeDocumentStory", null);

                InvokePrivate(context.Controller, "HandleStoryChanged");

                if (!(bool)GetPrivateField(context.Controller, "waitingForBeforeDocumentActorClick"))
                {
                    throw new InvalidDataException("S0001 ????????????????????????????????????");
                }

                if (context.DocumentButton.interactable)
                {
                    throw new InvalidDataException("????????????????????");
                }

                if (context.CityButton.interactable)
                {
                    throw new InvalidDataException("?????????????????????????????? S0002???????????");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ValidateOpeningTutorialHandlesBeforeGenericBlackFade()
        {
            const string sourcePath = "Assets/Scripts/UI/DeskLoopController.cs";
            var source = File.ReadAllText(sourcePath);
            var openingIndex = source.IndexOf("if (TryHandleOpeningTutorialStoryChanged())", System.StringComparison.Ordinal);
            var genericFadeIndex = source.IndexOf("TryPlayStoryBlackFadeTransition();", System.StringComparison.Ordinal);
            if (openingIndex < 0 || genericFadeIndex < 0 || openingIndex > genericFadeIndex)
            {
                throw new InvalidDataException("S0001 结束时必须先由开场教学流程接管，再进入普通剧情黑场判断，否则剧情人物不会弹出。");
            }
        }

        private static TestContext CreateContext(GameObject root)
        {
            var configManager = root.AddComponent<ConfigManager>();
            var runtimeDataService = root.AddComponent<RuntimeDataService>();
            var documentService = root.AddComponent<DocumentService>();
            var taskService = root.AddComponent<TaskService>();
            var storyService = root.AddComponent<StoryService>();
            var bootstrap = root.AddComponent<BaseSceneUIBootstrap>();
            var controller = root.AddComponent<DeskLoopController>();
            var documentButton = CreateButton(root.transform, "????");
            var cityButton = CreateButton(root.transform, "????");
            var actorSlot = root.AddComponent<SharedActorSlotView>();

            ConfigureConfigManager(configManager);
            ConfigureRuntimeDataService(runtimeDataService, configManager);
            ConfigureDocumentService(documentService, configManager, runtimeDataService, taskService);
            ConfigureTaskService(taskService, configManager, runtimeDataService);
            ConfigureStoryService(storyService, configManager, runtimeDataService, taskService);
            ConfigureController(controller, runtimeDataService, storyService, documentService, taskService, bootstrap, actorSlot, documentButton, cityButton);

            configManager.BuildDefaultProviders();
            runtimeDataService.CreateNewGame("DI0001");
            taskService.Refresh();
            storyService.Refresh();
            documentService.Refresh();

            return new TestContext(runtimeDataService, controller, documentButton, cityButton);
        }

        private static Button CreateButton(Transform parent, string objectName)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            return buttonObject.GetComponent<Button>();
        }

        private static void ConfigureConfigManager(ConfigManager configManager)
        {
            var serializedObject = new SerializedObject(configManager);
            serializedObject.FindProperty("relativeConfigDirectory").stringValue = "Configs/Plot";
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

        private static void ConfigureDocumentService(DocumentService documentService, ConfigManager configManager, RuntimeDataService runtimeDataService, TaskService taskService)
        {
            var serializedObject = new SerializedObject(documentService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTaskService(TaskService taskService, ConfigManager configManager, RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(taskService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureStoryService(StoryService storyService, ConfigManager configManager, RuntimeDataService runtimeDataService, TaskService taskService)
        {
            var serializedObject = new SerializedObject(storyService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureController(
            DeskLoopController controller,
            RuntimeDataService runtimeDataService,
            StoryService storyService,
            DocumentService documentService,
            TaskService taskService,
            BaseSceneUIBootstrap bootstrap,
            SharedActorSlotView actorSlot,
            Button documentButton,
            Button cityButton)
        {
            var serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("storyService").objectReferenceValue = storyService;
            serializedObject.FindProperty("documentService").objectReferenceValue = documentService;
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.FindProperty("uiBootstrap").objectReferenceValue = bootstrap;
            serializedObject.FindProperty("sharedActorSlot").objectReferenceValue = actorSlot;
            serializedObject.FindProperty("documentButton").objectReferenceValue = documentButton;
            serializedObject.FindProperty("cityButton").objectReferenceValue = cityButton;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidDataException($"??? {target.GetType().Name}.{methodName}??????????????");
            }

            method.Invoke(target, null);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidDataException($"??? {target.GetType().Name}.{fieldName} ????????????");
            }

            field.SetValue(target, value);
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidDataException($"??? {target.GetType().Name}.{fieldName} ????????????");
            }

            return field.GetValue(target);
        }

        private sealed class TestContext
        {
            public TestContext(RuntimeDataService runtimeDataService, DeskLoopController controller, Button documentButton, Button cityButton)
            {
                RuntimeDataService = runtimeDataService;
                Controller = controller;
                DocumentButton = documentButton;
                CityButton = cityButton;
            }

            public RuntimeDataService RuntimeDataService { get; }
            public DeskLoopController Controller { get; }
            public Button DocumentButton { get; }
            public Button CityButton { get; }
        }
    }
}
