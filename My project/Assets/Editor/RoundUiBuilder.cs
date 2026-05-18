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
    public static class RoundUiBuilder
    {
        [MenuItem("Twelve Moons/Setup/Create Round UI")]
        public static void CreateRoundUi()
        {
            var canvas = FindOrCreateCanvas();
            FindOrCreateEventSystem();

            var gameEntry = FindOrCreateGameEntry();
            var configManager = EnsureComponent<ConfigManager>(gameEntry.gameObject);
            var runtimeDataService = EnsureComponent<RuntimeDataService>(gameEntry.gameObject);
            var roundService = EnsureComponent<RoundService>(gameEntry.gameObject);

            ConfigureConfigManager(configManager);
            ConfigureRuntimeDataService(runtimeDataService, configManager);
            ConfigureRoundService(roundService, configManager, runtimeDataService);

            var roundPanel = FindOrCreateUiChild(canvas.transform, "RoundPanel");
            ConfigurePanel(roundPanel);

            var titleText = CreateOrConfigureText(roundPanel.transform, "TitleText", "Round", 18, FontStyles.Bold, TextAlignmentOptions.Left);
            SetStretchTopRect(titleText.rectTransform, new Vector2(16f, -42f), new Vector2(-16f, -12f));

            var roundText = CreateOrConfigureText(roundPanel.transform, "RoundText", "Round 1", 28, FontStyles.Bold, TextAlignmentOptions.Left);
            SetStretchTopRect(roundText.rectTransform, new Vector2(16f, -86f), new Vector2(-16f, -48f));

            var totalRoundText = CreateOrConfigureText(roundPanel.transform, "TotalRoundText", "Total 18", 16, FontStyles.Normal, TextAlignmentOptions.Left);
            SetStretchTopRect(totalRoundText.rectTransform, new Vector2(16f, -116f), new Vector2(-16f, -88f));

            var stageText = CreateOrConfigureText(roundPanel.transform, "DisasterStageText", "Warning", 16, FontStyles.Bold, TextAlignmentOptions.Left);
            stageText.color = new Color(0.9f, 0.82f, 0.55f, 1f);
            SetStretchTopRect(stageText.rectTransform, new Vector2(16f, -148f), new Vector2(-16f, -120f));

            var feedbackText = CreateOrConfigureText(roundPanel.transform, "RoundFeedbackText", "", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            feedbackText.color = new Color(0.82f, 0.82f, 0.78f, 1f);
            SetStretchBottomRect(feedbackText.rectTransform, new Vector2(16f, 60f), new Vector2(-16f, 94f));

            var panelView = EnsureComponent<RoundPanelView>(roundPanel);
            ConfigureRoundPanelView(panelView, roundService, roundText, totalRoundText, stageText, feedbackText);

            var debugControls = EnsureComponent<RoundDebugControls>(roundPanel);
            ConfigureRoundDebugControls(debugControls, roundService);
            CreateDebugButtons(roundPanel.transform, debugControls);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = roundPanel;
            Debug.Log("Round UI setup completed. Created or updated RoundPanel, RoundService, TMP labels, and test buttons.");
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

        private static void ConfigurePanel(GameObject panelObject)
        {
            var rectTransform = panelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -24f);
            rectTransform.sizeDelta = new Vector2(360f, 220f);

            var image = EnsureComponent<Image>(panelObject);
            image.color = new Color(0.1f, 0.1f, 0.09f, 0.94f);
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

        private static void CreateDebugButtons(Transform panelTransform, RoundDebugControls debugControls)
        {
            var debugRoot = FindOrCreateUiChild(panelTransform, "RoundDebugButtons");
            var rectTransform = debugRoot.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, 12f);
            rectTransform.sizeDelta = new Vector2(-32f, 38f);

            var grid = EnsureComponent<GridLayoutGroup>(debugRoot);
            grid.cellSize = new Vector2(150f, 34f);
            grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.MiddleCenter;

            CreateButton(debugRoot.transform, "NextRoundButton", "Next Round", debugControls, nameof(RoundDebugControls.NextRound));
            CreateButton(debugRoot.transform, "RestartRoundButton", "Restart", debugControls, nameof(RoundDebugControls.Restart));
        }

        private static void CreateButton(Transform parent, string name, string label, RoundDebugControls target, string methodName)
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

        private static UnityAction CreateAction(RoundDebugControls target, string methodName)
        {
            switch (methodName)
            {
                case nameof(RoundDebugControls.NextRound):
                    return target.NextRound;
                case nameof(RoundDebugControls.Restart):
                    return target.Restart;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(methodName), methodName, "Unsupported round debug method.");
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

        private static void ConfigureRoundPanelView(
            RoundPanelView panelView,
            RoundService roundService,
            TMP_Text roundText,
            TMP_Text totalRoundText,
            TMP_Text disasterStageText,
            TMP_Text feedbackText)
        {
            var serializedObject = new SerializedObject(panelView);
            serializedObject.FindProperty("roundService").objectReferenceValue = roundService;
            serializedObject.FindProperty("roundText").objectReferenceValue = roundText;
            serializedObject.FindProperty("totalRoundText").objectReferenceValue = totalRoundText;
            serializedObject.FindProperty("disasterStageText").objectReferenceValue = disasterStageText;
            serializedObject.FindProperty("feedbackText").objectReferenceValue = feedbackText;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRoundDebugControls(RoundDebugControls debugControls, RoundService roundService)
        {
            var serializedObject = new SerializedObject(debugControls);
            serializedObject.FindProperty("roundService").objectReferenceValue = roundService;
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
