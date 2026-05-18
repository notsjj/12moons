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
    public static class SuspicionUiBuilder
    {
        private const string FactionRowPrefabPath = "Assets/Prefabs/UI/FactionSuspicionRow.prefab";

        [MenuItem("Twelve Moons/Setup/Create Suspicion UI")]
        public static void CreateSuspicionUi()
        {
            var canvas = FindOrCreateCanvas();
            FindOrCreateEventSystem();

            var gameEntry = FindOrCreateGameEntry();
            var configManager = EnsureComponent<ConfigManager>(gameEntry.gameObject);
            var runtimeDataService = EnsureComponent<RuntimeDataService>(gameEntry.gameObject);
            var factionService = EnsureComponent<FactionService>(gameEntry.gameObject);

            ConfigureConfigManager(configManager);
            ConfigureRuntimeDataService(runtimeDataService, configManager);
            ConfigureFactionService(factionService, configManager, runtimeDataService);

            var suspicionPanel = FindOrCreateUiChild(canvas.transform, "SuspicionPanel");
            ConfigurePanelRect(suspicionPanel.GetComponent<RectTransform>());
            ConfigurePanelBackground(suspicionPanel);

            var titleText = CreateOrConfigureText(suspicionPanel.transform, "TitleText", "Faction Suspicion", 18, FontStyles.Bold, TextAlignmentOptions.Left);
            SetStretchTopRect(titleText.rectTransform, new Vector2(16f, -44f), new Vector2(-16f, -14f));

            var contentRoot = FindOrCreateUiChild(suspicionPanel.transform, "SuspicionContent");
            ConfigureContentRoot(contentRoot);

            var feedbackText = CreateOrConfigureText(suspicionPanel.transform, "FactionFeedbackText", "", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            SetStretchBottomRect(feedbackText.rectTransform, new Vector2(16f, 64f), new Vector2(-16f, 112f));

            var panelView = EnsureComponent<SuspicionPanelView>(suspicionPanel);
            ConfigureSuspicionPanelView(
                panelView,
                factionService,
                runtimeDataService,
                contentRoot,
                feedbackText,
                LoadOrCreateFactionRowPrefab());

            var debugControls = EnsureComponent<FactionDebugControls>(suspicionPanel);
            ConfigureFactionDebugControls(debugControls, factionService);
            CreateDebugButtons(suspicionPanel.transform, debugControls);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = suspicionPanel;
            Debug.Log("Suspicion UI setup completed. Created or updated SuspicionPanel, SuspicionContent, feedback text, FactionService, and debug buttons.");
        }

        private static FactionSuspicionRow LoadOrCreateFactionRowPrefab()
        {
            var rowPrefab = AssetDatabase.LoadAssetAtPath<FactionSuspicionRow>(FactionRowPrefabPath);
            if (rowPrefab != null)
            {
                return rowPrefab;
            }

            FactionSuspicionRowPrefabBuilder.CreateFactionSuspicionRowPrefab();
            AssetDatabase.Refresh();
            rowPrefab = AssetDatabase.LoadAssetAtPath<FactionSuspicionRow>(FactionRowPrefabPath);
            if (rowPrefab == null)
            {
                throw new System.IO.FileNotFoundException($"Failed to create faction suspicion row prefab at {FactionRowPrefabPath}.");
            }

            return rowPrefab;
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

        private static void ConfigurePanelRect(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-24f, -24f);
            rectTransform.sizeDelta = new Vector2(420f, 420f);
        }

        private static void ConfigurePanelBackground(GameObject panelObject)
        {
            var image = EnsureComponent<Image>(panelObject);
            image.color = new Color(0.1f, 0.1f, 0.09f, 0.94f);
        }

        private static void ConfigureContentRoot(GameObject contentRoot)
        {
            var rectTransform = contentRoot.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.offsetMin = new Vector2(16f, 154f);
            rectTransform.offsetMax = new Vector2(-16f, -54f);

            var layoutGroup = EnsureComponent<VerticalLayoutGroup>(contentRoot);
            layoutGroup.padding = new RectOffset(0, 0, 0, 0);
            layoutGroup.spacing = 8f;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
        }

        private static TextMeshProUGUI CreateOrConfigureText(
            Transform parent,
            string name,
            string textValue,
            int fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment)
        {
            var textObject = FindOrCreateUiChild(parent, name);
            RemoveLegacyText(textObject);

            var text = EnsureComponent<TextMeshProUGUI>(textObject);
            text.text = textValue;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateDebugButtons(Transform panelTransform, FactionDebugControls debugControls)
        {
            var debugRoot = FindOrCreateUiChild(panelTransform, "SuspicionDebugButtons");
            var rectTransform = debugRoot.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, 12f);
            rectTransform.sizeDelta = new Vector2(-32f, 42f);

            var grid = EnsureComponent<GridLayoutGroup>(debugRoot);
            grid.cellSize = new Vector2(122f, 34f);
            grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.MiddleCenter;

            CreateButton(debugRoot.transform, "LowerSuspicionButton", "Lower", debugControls, nameof(FactionDebugControls.LowerTestFactionSuspicion));
            CreateButton(debugRoot.transform, "RaiseSuspicionButton", "Raise", debugControls, nameof(FactionDebugControls.RaiseTestFactionSuspicion));
            CreateButton(debugRoot.transform, "RefreshFactionsButton", "Refresh", debugControls, nameof(FactionDebugControls.RefreshFactions));
        }

        private static void CreateButton(Transform parent, string name, string label, FactionDebugControls target, string methodName)
        {
            var buttonTransform = parent.Find(name);
            var buttonObject = buttonTransform != null
                ? buttonTransform.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));

            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.23f, 0.23f, 0.2f, 1f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            AddPersistentListenerIfMissing(button, target, methodName, CreateAction(target, methodName));

            var labelText = CreateOrConfigureText(buttonObject.transform, "Label", label, 14, FontStyles.Bold, TextAlignmentOptions.Center);
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            SetStretchRect(labelText.rectTransform, Vector2.zero, Vector2.zero);
        }

        private static void RemoveLegacyText(GameObject gameObject)
        {
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (component != null && component.GetType().FullName == "UnityEngine.UI.Text")
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

        private static UnityAction CreateAction(FactionDebugControls target, string methodName)
        {
            switch (methodName)
            {
                case nameof(FactionDebugControls.LowerTestFactionSuspicion):
                    return target.LowerTestFactionSuspicion;
                case nameof(FactionDebugControls.RaiseTestFactionSuspicion):
                    return target.RaiseTestFactionSuspicion;
                case nameof(FactionDebugControls.RefreshFactions):
                    return target.RefreshFactions;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(methodName), methodName, "Unsupported faction debug method.");
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

        private static void ConfigureFactionService(
            FactionService factionService,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(factionService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("highSuspicionReduceValue").intValue = 30;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSuspicionPanelView(
            SuspicionPanelView panelView,
            FactionService factionService,
            RuntimeDataService runtimeDataService,
            GameObject contentRoot,
            TMP_Text feedbackText,
            FactionSuspicionRow rowPrefab)
        {
            var serializedObject = new SerializedObject(panelView);
            serializedObject.FindProperty("factionService").objectReferenceValue = factionService;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("contentRoot").objectReferenceValue = contentRoot.GetComponent<RectTransform>();
            serializedObject.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
            serializedObject.FindProperty("feedbackText").objectReferenceValue = feedbackText;
            ConfigureFactionIconBindings(serializedObject.FindProperty("factionIcons"));
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureFactionIconBindings(SerializedProperty factionIconsProperty)
        {
            var factionIds = new[] { "noble", "academy", "church", "civilian" };
            factionIconsProperty.arraySize = factionIds.Length;
            for (var index = 0; index < factionIds.Length; index++)
            {
                var binding = factionIconsProperty.GetArrayElementAtIndex(index);
                binding.FindPropertyRelative("factionId").stringValue = factionIds[index];
            }
        }

        private static void ConfigureFactionDebugControls(FactionDebugControls debugControls, FactionService factionService)
        {
            var serializedObject = new SerializedObject(debugControls);
            serializedObject.FindProperty("factionService").objectReferenceValue = factionService;
            serializedObject.FindProperty("lowTestFactionId").stringValue = "civilian";
            serializedObject.FindProperty("highTestFactionId").stringValue = "noble";
            serializedObject.FindProperty("lowTestDelta").intValue = -35;
            serializedObject.FindProperty("highTestDelta").intValue = 45;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStretchTopRect(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static void SetStretchBottomRect(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static void SetStretchRect(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }
}
