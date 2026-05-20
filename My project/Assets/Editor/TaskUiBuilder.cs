using TMPro;
using TwelveMoons.Core;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools
{
    public static class TaskUiBuilder
    {
        [MenuItem("Twelve Moons/Setup/Create Task UI")]
        public static void CreateTaskUi()
        {
            var canvas = FindOrCreateCanvas();
            FindOrCreateEventSystem();

            var gameEntry = FindOrCreateGameEntry();
            var configManager = EnsureComponent<ConfigManager>(gameEntry.gameObject);
            var runtimeDataService = EnsureComponent<RuntimeDataService>(gameEntry.gameObject);
            var roundService = EnsureComponent<RoundService>(gameEntry.gameObject);
            var taskService = EnsureComponent<TaskService>(gameEntry.gameObject);

            ConfigureConfigManager(configManager);
            ConfigureRuntimeDataService(runtimeDataService, configManager);
            ConfigureRoundService(roundService, configManager, runtimeDataService);
            ConfigureTaskService(taskService, configManager, runtimeDataService, roundService);

            var taskPanel = FindOrCreateUiChild(canvas.transform, "TaskPanel");
            ConfigurePanelRect(taskPanel.GetComponent<RectTransform>());

            var titleText = FindOrCreateText(taskPanel.transform, "TitleText", "Tasks", 20, FontStyles.Bold);
            ConfigureTopText(titleText.rectTransform, new Vector2(16f, -12f), new Vector2(-16f, -44f));

            var emptyText = FindOrCreateText(taskPanel.transform, "EmptyText", "No active task.", 14, FontStyles.Normal);
            ConfigureTopText(emptyText.rectTransform, new Vector2(16f, -48f), new Vector2(-16f, -78f));

            var content = FindOrCreateTaskScrollContent(taskPanel.transform);

            var feedbackText = FindOrCreateText(taskPanel.transform, "FeedbackText", "", 13, FontStyles.Normal);
            ConfigureBottomText(feedbackText.rectTransform);

            var panelView = EnsureComponent<TaskPanelView>(taskPanel);
            ConfigureTaskPanelView(panelView, taskService, runtimeDataService, content, emptyText);

            var debugControls = EnsureComponent<TaskDebugControls>(taskPanel);
            ConfigureTaskDebugControls(debugControls, taskService, roundService, runtimeDataService, feedbackText);
            CreateDebugButtons(taskPanel.transform, debugControls);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = taskPanel;
            Debug.Log("Task UI setup completed. Created or updated TaskPanel, TaskContent, TaskService, and phase task debug buttons.");
        }

        private static Canvas FindOrCreateCanvas()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                return canvas;
            }

            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return canvas;
        }

        private static void FindOrCreateEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static GameEntry FindOrCreateGameEntry()
        {
            var gameEntry = Object.FindFirstObjectByType<GameEntry>();
            if (gameEntry != null)
            {
                return gameEntry;
            }

            return new GameObject("GameEntry").AddComponent<GameEntry>();
        }

        private static GameObject FindOrCreateUiChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                return child.gameObject;
            }

            var childObject = new GameObject(childName, typeof(RectTransform));
            childObject.transform.SetParent(parent, false);
            return childObject;
        }

        private static TextMeshProUGUI FindOrCreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            FontStyles style)
        {
            var textObject = FindOrCreateUiChild(parent, name);
            RemoveLegacyText(textObject);
            var text = EnsureComponent<TextMeshProUGUI>(textObject);
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Left;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void ConfigurePanelRect(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0.2f, 1f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var image = rectTransform.GetComponent<Image>();
            if (image == null)
            {
                image = rectTransform.gameObject.AddComponent<Image>();
            }

            image.color = new Color(0.11f, 0.11f, 0.1f, 0.92f);
        }

        private static void ConfigureTopText(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static void ConfigureBottomText(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.offsetMin = new Vector2(16f, 108f);
            rectTransform.offsetMax = new Vector2(-16f, 136f);
        }

        private static GameObject FindOrCreateTaskScrollContent(Transform panelTransform)
        {
            var oldContent = panelTransform.Find("TaskContent");
            if (oldContent != null)
            {
                Object.DestroyImmediate(oldContent.gameObject);
            }

            var scrollView = FindOrCreateUiChild(panelTransform, "TaskScrollView");
            var scrollRectTransform = scrollView.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(16f, 150f);
            scrollRectTransform.offsetMax = new Vector2(-16f, -84f);

            var scrollRect = EnsureComponent<ScrollRect>(scrollView);
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            var viewport = FindOrCreateUiChild(scrollView.transform, "Viewport");
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var viewportImage = EnsureComponent<Image>(viewport);
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            var mask = EnsureComponent<Mask>(viewport);
            mask.showMaskGraphic = false;
            scrollRect.viewport = viewportRect;

            var contentObject = FindOrCreateUiChild(viewport.transform, "TaskContent");
            ConfigureContent(contentObject);
            scrollRect.content = contentObject.GetComponent<RectTransform>();
            return contentObject;
        }

        private static void ConfigureContent(GameObject contentObject)
        {
            var rectTransform = contentObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0f, 0f);

            var layout = EnsureComponent<VerticalLayoutGroup>(contentObject);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 8f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = EnsureComponent<ContentSizeFitter>(contentObject);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void CreateDebugButtons(Transform panelTransform, TaskDebugControls debugControls)
        {
            var debugRoot = FindOrCreateUiChild(panelTransform, "TaskDebugButtons");
            var rectTransform = debugRoot.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, 12f);
            rectTransform.sizeDelta = new Vector2(-32f, 88f);

            var grid = EnsureComponent<GridLayoutGroup>(debugRoot);
            grid.cellSize = new Vector2(182f, 36f);
            grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.MiddleCenter;

            CreateButton(debugRoot.transform, "ActivateTaskButton", "Activate Task", debugControls, nameof(TaskDebugControls.ActivateDemoTask));
            CreateButton(debugRoot.transform, "AddScoreButton", "+ Task Score", debugControls, nameof(TaskDebugControls.AddDemoTaskScore));
            CreateButton(debugRoot.transform, "NextRoundButton", "Next Round", debugControls, nameof(TaskDebugControls.NextRound));
            CreateButton(debugRoot.transform, "RefreshTaskButton", "Refresh Tasks", debugControls, nameof(TaskDebugControls.RefreshTasks));
        }

        private static void CreateButton(Transform parent, string name, string label, TaskDebugControls target, string methodName)
        {
            var buttonObject = FindOrCreateUiChild(parent, name);
            var image = EnsureComponent<Image>(buttonObject);
            image.color = new Color(0.23f, 0.23f, 0.2f, 1f);

            var button = EnsureComponent<Button>(buttonObject);
            button.targetGraphic = image;
            AddPersistentListenerIfMissing(button, target, methodName, CreateAction(target, methodName));

            var labelText = FindOrCreateText(buttonObject.transform, "Label", label, 14, FontStyles.Normal);
            labelText.alignment = TextAlignmentOptions.Center;
            var rectTransform = labelText.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void RemoveLegacyText(GameObject gameObject)
        {
            foreach (var component in gameObject.GetComponents<Component>())
            {
                var componentType = component.GetType();
                if (componentType.FullName == "UnityEngine.UI.Text")
                {
                    Object.DestroyImmediate(component);
                }
            }
        }

        private static void AddPersistentListenerIfMissing(Button button, Object target, string methodName, UnityAction action)
        {
            for (var index = 0; index < button.onClick.GetPersistentEventCount(); index++)
            {
                if (button.onClick.GetPersistentTarget(index) == target &&
                    button.onClick.GetPersistentMethodName(index) == methodName)
                {
                    return;
                }
            }

            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static UnityAction CreateAction(TaskDebugControls target, string methodName)
        {
            switch (methodName)
            {
                case nameof(TaskDebugControls.ActivateDemoTask):
                    return target.ActivateDemoTask;
                case nameof(TaskDebugControls.AddDemoTaskScore):
                    return target.AddDemoTaskScore;
                case nameof(TaskDebugControls.NextRound):
                    return target.NextRound;
                case nameof(TaskDebugControls.RefreshTasks):
                    return target.RefreshTasks;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(methodName), methodName, "Unsupported task debug method.");
            }
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void ConfigureConfigManager(ConfigManager configManager)
        {
            var serializedObject = new SerializedObject(configManager);
            serializedObject.FindProperty("relativeConfigDirectory").stringValue = "Configs/Demo";
            serializedObject.FindProperty("loadOnAwake").boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRuntimeDataService(RuntimeDataService runtimeDataService, ConfigManager configManager)
        {
            var serializedObject = new SerializedObject(runtimeDataService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("createNewGameOnAwake").boolValue = true;
            serializedObject.FindProperty("initialDisasterId").stringValue = "disaster_flood_01";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRoundService(
            RoundService roundService,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(roundService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTaskService(
            TaskService taskService,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService,
            RoundService roundService)
        {
            var serializedObject = new SerializedObject(taskService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("roundService").objectReferenceValue = roundService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTaskPanelView(
            TaskPanelView panelView,
            TaskService taskService,
            RuntimeDataService runtimeDataService,
            GameObject contentRoot,
            TMP_Text emptyText)
        {
            var serializedObject = new SerializedObject(panelView);
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("contentRoot").objectReferenceValue = contentRoot.GetComponent<RectTransform>();
            serializedObject.FindProperty("emptyText").objectReferenceValue = emptyText;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTaskDebugControls(
            TaskDebugControls debugControls,
            TaskService taskService,
            RoundService roundService,
            RuntimeDataService runtimeDataService,
            TMP_Text feedbackText)
        {
            var serializedObject = new SerializedObject(debugControls);
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.FindProperty("roundService").objectReferenceValue = roundService;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("demoTaskId").stringValue = "task_demo_relief_01";
            serializedObject.FindProperty("scoreStep").intValue = 1;
            serializedObject.FindProperty("feedbackText").objectReferenceValue = feedbackText;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
