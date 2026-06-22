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
    public static class LetterAreaUiBuilder
    {
        [MenuItem("Twelve Moons/Setup/Create Letter Area UI")]
        public static void CreateLetterAreaUi()
        {
            var canvas = FindOrCreateCanvas();
            FindOrCreateEventSystem();

            var gameEntry = FindOrCreateGameEntry();
            var configManager = EnsureComponent<ConfigManager>(gameEntry.gameObject);
            var runtimeDataService = EnsureComponent<RuntimeDataService>(gameEntry.gameObject);
            var letterService = EnsureComponent<LetterService>(gameEntry.gameObject);

            ConfigureConfigManager(configManager);
            ConfigureRuntimeDataService(runtimeDataService, configManager);
            ConfigureLetterService(letterService, configManager, runtimeDataService);

            var letterArea = FindOrCreateUiChild(canvas.transform, "LetterArea");
            ConfigureRootPanel(letterArea.GetComponent<RectTransform>());

            var headerText = FindOrCreateText(letterArea.transform, "HeaderText", "Letters", 22, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(headerText.rectTransform, new Vector2(24f, -18f), new Vector2(-24f, -54f), new Vector2(0f, 1f), new Vector2(1f, 1f));

            var listPanel = FindOrCreateUiChild(letterArea.transform, "LetterListPanel");
            SetRect(listPanel.GetComponent<RectTransform>(), new Vector2(24f, 96f), new Vector2(370f, -72f), Vector2.zero, Vector2.one);
            ConfigurePanelImage(listPanel, new Color(0.13f, 0.13f, 0.12f, 0.96f));

            var emptyText = FindOrCreateText(listPanel.transform, "EmptyText", "No letters received.", 15, FontStyles.Normal, TextAlignmentOptions.Center);
            SetRect(emptyText.rectTransform, new Vector2(18f, 18f), new Vector2(-18f, -18f), Vector2.zero, Vector2.one);

            var listRoot = FindOrCreateUiChild(listPanel.transform, "ListRoot");
            var listRootRect = listRoot.GetComponent<RectTransform>();
            SetRect(listRootRect, new Vector2(22f, 22f), new Vector2(-22f, -22f), Vector2.zero, Vector2.one);
            ConfigureLetterIconGrid(listRoot);

            var rowTemplate = FindOrCreateUiChild(letterArea.transform, "LetterRowTemplate");
            ConfigureRowTemplate(rowTemplate);
            rowTemplate.SetActive(false);

            var readerPanel = FindOrCreateUiChild(letterArea.transform, "LetterReaderPanel");
            SetRect(readerPanel.GetComponent<RectTransform>(), new Vector2(394f, 96f), new Vector2(-24f, -72f), Vector2.zero, Vector2.one);
            ConfigurePanelImage(readerPanel, new Color(0.15f, 0.145f, 0.13f, 0.96f));

            var titleText = FindOrCreateText(readerPanel.transform, "TitleText", "Select a letter", 24, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(titleText.rectTransform, new Vector2(24f, -24f), new Vector2(-96f, -64f), new Vector2(0f, 1f), new Vector2(1f, 1f));
            var closeButton = CreateButton(readerPanel.transform, "CloseButton", "Exit");
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(-82f, -26f), new Vector2(-24f, -62f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            var senderText = FindOrCreateText(readerPanel.transform, "SenderText", "", 15, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(senderText.rectTransform, new Vector2(24f, -70f), new Vector2(-24f, -98f), new Vector2(0f, 1f), new Vector2(1f, 1f));
            var roundText = FindOrCreateText(readerPanel.transform, "RoundText", "", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(roundText.rectTransform, new Vector2(24f, -100f), new Vector2(-24f, -126f), new Vector2(0f, 1f), new Vector2(1f, 1f));
            var bodyText = FindOrCreateText(readerPanel.transform, "BodyText", "", 18, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            bodyText.overflowMode = TextOverflowModes.Overflow;
            SetRect(bodyText.rectTransform, new Vector2(24f, 68f), new Vector2(-24f, -146f), Vector2.zero, Vector2.one);
            var feedbackText = FindOrCreateText(readerPanel.transform, "FeedbackText", "", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(feedbackText.rectTransform, new Vector2(24f, 24f), new Vector2(-24f, 54f), new Vector2(0f, 0f), new Vector2(1f, 0f));

            var debugRoot = FindOrCreateUiChild(letterArea.transform, "LetterDebugButtons");
            ConfigureDebugRoot(debugRoot.GetComponent<RectTransform>());

            var areaView = EnsureComponent<LetterAreaView>(letterArea);
            ConfigureLetterAreaView(areaView, letterService, listRoot.transform, rowTemplate.GetComponent<LetterRowView>(), emptyText, readerPanel, titleText, senderText, roundText, bodyText, feedbackText);
            AddPersistentListenerIfMissing(closeButton, areaView, nameof(LetterAreaView.CloseSelectedLetter), areaView.CloseSelectedLetter);
            readerPanel.SetActive(false);

            var debugControls = EnsureComponent<LetterDebugControls>(letterArea);
            ConfigureDebugControls(debugControls, letterService, feedbackText);
            CreateDebugButton(debugRoot.transform, "ReceiveLetterAButton", "Receive A", debugControls, nameof(LetterDebugControls.ReceiveDemoLetterA));
            CreateDebugButton(debugRoot.transform, "ReceiveLetterBButton", "Receive B", debugControls, nameof(LetterDebugControls.ReceiveDemoLetterB));
            CreateDebugButton(debugRoot.transform, "ReceiveLetterCButton", "Receive C", debugControls, nameof(LetterDebugControls.ReceiveDemoLetterC));
            CreateDebugButton(debugRoot.transform, "RefreshLettersButton", "Refresh", debugControls, nameof(LetterDebugControls.RefreshLetters));

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = letterArea;
            Debug.Log("Letter Area UI setup completed. Created or updated LetterArea, 3x3 letter icon grid, hidden reader panel, exit button, and letter debug buttons.");
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
            return gameEntry != null ? gameEntry : new GameObject("GameEntry").AddComponent<GameEntry>();
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
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            var textObject = FindOrCreateUiChild(parent, name);
            RemoveLegacyText(textObject);
            var text = EnsureComponent<TextMeshProUGUI>(textObject);
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            var buttonObject = FindOrCreateUiChild(parent, name);
            var image = EnsureComponent<Image>(buttonObject);
            image.color = new Color(0.24f, 0.24f, 0.22f, 1f);

            var button = EnsureComponent<Button>(buttonObject);
            button.targetGraphic = image;

            var labelText = FindOrCreateText(buttonObject.transform, "Label", label, 14, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(labelText.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            return button;
        }

        private static void CreateDebugButton(Transform parent, string name, string label, LetterDebugControls target, string methodName)
        {
            var button = CreateButton(parent, name, label);
            AddPersistentListenerIfMissing(button, target, methodName, CreateDebugAction(target, methodName));
        }

        private static UnityAction CreateDebugAction(LetterDebugControls target, string methodName)
        {
            switch (methodName)
            {
                case nameof(LetterDebugControls.ReceiveDemoLetterA):
                    return target.ReceiveDemoLetterA;
                case nameof(LetterDebugControls.ReceiveDemoLetterB):
                    return target.ReceiveDemoLetterB;
                case nameof(LetterDebugControls.ReceiveDemoLetterC):
                    return target.ReceiveDemoLetterC;
                case nameof(LetterDebugControls.RefreshLetters):
                    return target.RefreshLetters;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(methodName), methodName, "Unsupported letter debug method.");
            }
        }

        private static void ConfigureRootPanel(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            ConfigurePanelImage(rectTransform.gameObject, new Color(0.1f, 0.1f, 0.095f, 0.94f));
        }

        private static void ConfigurePanelImage(GameObject target, Color color)
        {
            var image = EnsureComponent<Image>(target);
            image.color = color;
        }

        private static void ConfigureRowTemplate(GameObject rowTemplate)
        {
            var rectTransform = rowTemplate.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(82f, 82f);

            var image = EnsureComponent<Image>(rowTemplate);
            image.color = new Color(0.78f, 0.69f, 0.46f, 1f);
            var button = EnsureComponent<Button>(rowTemplate);
            button.targetGraphic = image;

            DestroyChildIfExists(rowTemplate.transform, "TitleText");
            DestroyChildIfExists(rowTemplate.transform, "SenderText");
            DestroyChildIfExists(rowTemplate.transform, "StatusText");

            var iconText = FindOrCreateText(rowTemplate.transform, "IconText", "信", 30, FontStyles.Bold, TextAlignmentOptions.Center);
            iconText.color = new Color(0.16f, 0.11f, 0.08f, 1f);
            SetRect(iconText.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);

            var rowView = EnsureComponent<LetterRowView>(rowTemplate);
            rowView.Configure(iconText, button);
            AddPersistentListenerIfMissing(button, rowView, nameof(LetterRowView.OnClicked), rowView.OnClicked);
        }

        private static void ConfigureLetterIconGrid(GameObject listRoot)
        {
            foreach (var layoutGroup in listRoot.GetComponents<LayoutGroup>())
            {
                Object.DestroyImmediate(layoutGroup);
            }

            var grid = EnsureComponent<GridLayoutGroup>(listRoot);
            grid.cellSize = new Vector2(82f, 82f);
            grid.spacing = new Vector2(14f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperLeft;
        }

        private static void ConfigureDebugRoot(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            rectTransform.anchoredPosition = new Vector2(-24f, 24f);
            rectTransform.sizeDelta = new Vector2(170f, 176f);

            var grid = EnsureComponent<GridLayoutGroup>(rectTransform.gameObject);
            grid.cellSize = new Vector2(170f, 36f);
            grid.spacing = new Vector2(0f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 1;
        }

        private static void SetRect(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static void RemoveLegacyText(GameObject gameObject)
        {
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (component.GetType().FullName == "UnityEngine.UI.Text")
                {
                    Object.DestroyImmediate(component);
                }
            }
        }

        private static void DestroyChildIfExists(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
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
            serializedObject.FindProperty("initialDisasterId").stringValue = "DI0001";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureLetterService(LetterService letterService, ConfigManager configManager, RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(letterService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureLetterAreaView(
            LetterAreaView areaView,
            LetterService letterService,
            Transform listRoot,
            LetterRowView rowPrefab,
            TMP_Text emptyText,
            GameObject readerPanel,
            TMP_Text titleText,
            TMP_Text senderText,
            TMP_Text roundText,
            TMP_Text bodyText,
            TMP_Text feedbackText)
        {
            var serializedObject = new SerializedObject(areaView);
            serializedObject.FindProperty("letterService").objectReferenceValue = letterService;
            serializedObject.FindProperty("listRoot").objectReferenceValue = listRoot;
            serializedObject.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
            serializedObject.FindProperty("emptyText").objectReferenceValue = emptyText;
            serializedObject.FindProperty("maxVisibleLetters").intValue = 9;
            serializedObject.FindProperty("readerPanel").objectReferenceValue = readerPanel;
            serializedObject.FindProperty("titleText").objectReferenceValue = titleText;
            serializedObject.FindProperty("senderText").objectReferenceValue = senderText;
            serializedObject.FindProperty("roundText").objectReferenceValue = roundText;
            serializedObject.FindProperty("bodyText").objectReferenceValue = bodyText;
            serializedObject.FindProperty("feedbackText").objectReferenceValue = feedbackText;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDebugControls(LetterDebugControls debugControls, LetterService letterService, TMP_Text feedbackText)
        {
            var serializedObject = new SerializedObject(debugControls);
            serializedObject.FindProperty("letterService").objectReferenceValue = letterService;
            serializedObject.FindProperty("feedbackText").objectReferenceValue = feedbackText;
            serializedObject.FindProperty("demoLetterIdA").stringValue = "letter_relief_start";
            serializedObject.FindProperty("demoLetterIdB").stringValue = "letter_relief_prepare_end";
            serializedObject.FindProperty("demoLetterIdC").stringValue = "letter_relief_deliver_start";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
