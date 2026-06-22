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
    public static class StoryUiBuilder
    {
        [MenuItem("Twelve Moons/Setup/Create Story UI")]
        public static void CreateStoryUi()
        {
            var canvas = FindOrCreateCanvas();
            FindOrCreateEventSystem();

            var gameEntry = FindOrCreateGameEntry();
            var configManager = EnsureComponent<ConfigManager>(gameEntry.gameObject);
            var runtimeDataService = EnsureComponent<RuntimeDataService>(gameEntry.gameObject);
            var inventoryService = EnsureComponent<InventoryService>(gameEntry.gameObject);
            var roundService = EnsureComponent<RoundService>(gameEntry.gameObject);
            var taskService = EnsureComponent<TaskService>(gameEntry.gameObject);
            var storyService = EnsureComponent<StoryService>(gameEntry.gameObject);

            ConfigureConfigManager(configManager);
            ConfigureRuntimeDataService(runtimeDataService, configManager);
            ConfigureInventoryService(inventoryService, configManager, runtimeDataService);
            ConfigureRoundService(roundService, configManager, runtimeDataService);
            ConfigureTaskService(taskService, configManager, runtimeDataService, roundService);
            ConfigureStoryService(storyService, configManager, runtimeDataService, inventoryService, taskService);

            var storyPanel = FindOrCreateUiChild(canvas.transform, "StoryPanel");
            RemoveDeprecatedStoryPanelChildren(storyPanel.transform);
            ConfigurePanelRect(storyPanel.GetComponent<RectTransform>());
            var rootCanvasGroup = EnsureComponent<CanvasGroup>(storyPanel);
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.blocksRaycasts = false;
            rootCanvasGroup.interactable = false;

            var storyAreaButton = CreateTransparentButton(storyPanel.transform, "StoryAreaButton");
            SetFullStretchRect(storyAreaButton.GetComponent<RectTransform>());
            storyAreaButton.transform.SetAsFirstSibling();

            var titleText = FindOrCreateText(storyPanel.transform, "TitleText", "Story", 22, FontStyles.Bold, TextAlignmentOptions.Left);
            SetTopStretchRect(titleText.rectTransform, 18f, 16f, 18f, 36f);

            var feedbackText = FindOrCreateText(storyPanel.transform, "FeedbackText", "", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            SetBottomStretchRect(feedbackText.rectTransform, 32f, 32f, 232f, 30f);

            var dialoguePanel = FindOrCreateUiChild(storyPanel.transform, "DialoguePanel");
            RemoveDeprecatedDialoguePanelChildren(dialoguePanel.transform);
            ConfigureDialoguePanel(dialoguePanel.GetComponent<RectTransform>());
            var leftPortrait = FindOrCreateImage(dialoguePanel.transform, "LeftPortrait");
            SetFixedRect(leftPortrait.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(150f, 236f), new Vector2(420f, 620f));
            var rightPortrait = FindOrCreateImage(dialoguePanel.transform, "RightPortrait");
            SetFixedRect(rightPortrait.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-150f, 236f), new Vector2(420f, 620f));

            var dialogueBar = FindOrCreateUiChild(dialoguePanel.transform, "DialogueBar");
            ConfigureDialogueBar(dialogueBar.GetComponent<RectTransform>());
            var speakerExpressionImage = FindOrCreateImage(dialogueBar.transform, "SpeakerExpressionImage");
            SetFixedRect(speakerExpressionImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(164f, 164f));
            var speakerNameText = FindOrCreateText(dialogueBar.transform, "SpeakerNameText", "", 16, FontStyles.Bold, TextAlignmentOptions.Left);
            SetTopStretchRect(speakerNameText.rectTransform, 220f, 22f, 360f, 30f);
            var dialogueText = FindOrCreateText(dialogueBar.transform, "DialogueText", "", 17, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            dialogueText.overflowMode = TextOverflowModes.Overflow;
            SetTopStretchRect(dialogueText.rectTransform, 220f, 60f, 360f, 128f);
            var dialogueContinueButton = CreateButton(dialogueBar.transform, "ContinueButton", "Continue");
            SetFixedRect(dialogueContinueButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-336f, 22f), new Vector2(280f, 42f));
            var choiceButtonA = CreateButton(dialogueBar.transform, "ChoiceButtonA", "Choice A");
            SetFixedRect(choiceButtonA.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 124f), new Vector2(280f, 50f));
            var choiceButtonB = CreateButton(dialogueBar.transform, "ChoiceButtonB", "Choice B");
            SetFixedRect(choiceButtonB.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 58f), new Vector2(280f, 50f));

            var submissionPanel = FindOrCreateUiChild(dialogueBar.transform, "SubmissionPanel");
            ConfigureSubmissionPanel(submissionPanel.GetComponent<RectTransform>());
            var submissionTitleText = FindOrCreateText(submissionPanel.transform, "SubmissionTitleText", "Submit Items", 18, FontStyles.Bold, TextAlignmentOptions.Left);
            SetTopStretchRect(submissionTitleText.rectTransform, 24f, 20f, 280f, 34f);
            var submissionRequirementText = FindOrCreateText(submissionPanel.transform, "SubmissionRequirementText", "", 16, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            submissionRequirementText.overflowMode = TextOverflowModes.Overflow;
            SetTopStretchRect(submissionRequirementText.rectTransform, 24f, 70f, 300f, 110f);
            var submitButton = CreateButton(submissionPanel.transform, "SubmitButton", "Submit");
            SetFixedRect(submitButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 72f), new Vector2(236f, 48f));
            var exitSubmitButton = CreateButton(submissionPanel.transform, "ExitButton", "Exit");
            SetFixedRect(exitSubmitButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(100f, 38f));

            var imageStoryPanel = FindOrCreateUiChild(storyPanel.transform, "ImageStoryPanel");
            ConfigureContentPanel(imageStoryPanel.GetComponent<RectTransform>());
            var storyImage = FindOrCreateImage(imageStoryPanel.transform, "StoryImage");
            SetFixedRect(storyImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 64f), new Vector2(1280f, 620f));
            var comicPanelImages = CreateComicPanelImages(imageStoryPanel.transform);
            var imageCaptionText = FindOrCreateText(imageStoryPanel.transform, "ImageCaptionText", "", 15, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            SetBottomStretchRect(imageCaptionText.rectTransform, 96f, 88f, 96f, 52f);
            var imageContinueButton = CreateButton(imageStoryPanel.transform, "ContinueButton", "Finish");
            SetBottomStretchRect(imageContinueButton.GetComponent<RectTransform>(), 96f, 32f, 96f, 44f);

            var textStoryPanel = FindOrCreateUiChild(storyPanel.transform, "TextStoryPanel");
            ConfigureContentPanel(textStoryPanel.GetComponent<RectTransform>());
            var textContent = FindOrCreateText(textStoryPanel.transform, "TextContent", "", 18, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            textContent.overflowMode = TextOverflowModes.Overflow;
            SetFixedRect(textContent.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 48f), new Vector2(1320f, 640f));
            var textContinueButton = CreateButton(textStoryPanel.transform, "ContinueButton", "Finish");
            SetBottomStretchRect(textContinueButton.GetComponent<RectTransform>(), 160f, 32f, 160f, 44f);

            var debugRoot = FindOrCreateUiChild(storyPanel.transform, "StoryDebugButtons");
            ConfigureDebugRoot(debugRoot.GetComponent<RectTransform>());
            var debugControls = EnsureComponent<StoryDebugControls>(storyPanel);
            ConfigureStoryPanel(
                storyPanel,
                storyService,
                rootCanvasGroup,
                titleText,
                feedbackText,
                storyAreaButton,
                dialoguePanel,
                leftPortrait,
                rightPortrait,
                speakerExpressionImage,
                speakerNameText,
                dialogueText,
                dialogueContinueButton,
                choiceButtonA,
                choiceButtonB,
                submissionPanel,
                submissionTitleText,
                submissionRequirementText,
                submitButton,
                exitSubmitButton,
                imageStoryPanel,
                storyImage,
                comicPanelImages,
                imageCaptionText,
                imageContinueButton,
                textStoryPanel,
                textContent,
                textContinueButton);
            ConfigureDebugControls(debugControls, storyService, feedbackText);

            CreateDebugButton(debugRoot.transform, "StartQueuedStoryButton", "Start Queued", debugControls, nameof(StoryDebugControls.StartNextQueuedStory));
            CreateDebugButton(debugRoot.transform, "StartDialogueButton", "Demo Dialogue", debugControls, nameof(StoryDebugControls.StartDemoDialogue));
            CreateDebugButton(debugRoot.transform, "StartTextButton", "Demo Text", debugControls, nameof(StoryDebugControls.StartDemoText));
            CreateDebugButton(debugRoot.transform, "StartImageButton", "Demo Image", debugControls, nameof(StoryDebugControls.StartDemoImage));
            CreateDebugButton(debugRoot.transform, "StartComicImageButton", "Demo Comic", debugControls, nameof(StoryDebugControls.StartDemoComicImage));
            CreateDebugButton(debugRoot.transform, "StartSubmissionButton", "Demo Submit", debugControls, nameof(StoryDebugControls.StartDemoSubmission));
            CreateDebugButton(debugRoot.transform, "RefreshStoryButton", "Refresh", debugControls, nameof(StoryDebugControls.RefreshStories));

            var panelView = storyPanel.GetComponent<StoryPanelView>();
            AddPersistentListenerIfMissing(storyAreaButton, panelView, nameof(StoryPanelView.OnStoryAreaClicked), panelView.OnStoryAreaClicked);
            AddPersistentListenerIfMissing(dialogueContinueButton, panelView, nameof(StoryPanelView.OnContinueClicked), panelView.OnContinueClicked);
            AddPersistentListenerIfMissing(imageContinueButton, panelView, nameof(StoryPanelView.OnContinueClicked), panelView.OnContinueClicked);
            AddPersistentListenerIfMissing(textContinueButton, panelView, nameof(StoryPanelView.OnContinueClicked), panelView.OnContinueClicked);
            AddPersistentListenerIfMissing(choiceButtonA, panelView, nameof(StoryPanelView.OnOptionAClicked), panelView.OnOptionAClicked);
            AddPersistentListenerIfMissing(choiceButtonB, panelView, nameof(StoryPanelView.OnOptionBClicked), panelView.OnOptionBClicked);
            AddPersistentListenerIfMissing(submitButton, panelView, nameof(StoryPanelView.OnSubmitClicked), panelView.OnSubmitClicked);
            AddPersistentListenerIfMissing(exitSubmitButton, panelView, nameof(StoryPanelView.OnExitSubmitClicked), panelView.OnExitSubmitClicked);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = storyPanel;
            Debug.Log("Story UI setup completed. Created or updated StoryPanel with DialoguePanel, ImageStoryPanel, TextStoryPanel, TMP texts, and story debug buttons.");
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

        private static void RemoveDeprecatedStoryPanelChildren(Transform storyPanelTransform)
        {
            RemoveDirectChild(storyPanelTransform, "SpeakerText");
            RemoveDirectChild(storyPanelTransform, "BodyText");
            RemoveDirectChild(storyPanelTransform, "StoryImage");
            RemoveDirectChild(storyPanelTransform, "OptionAButton");
            RemoveDirectChild(storyPanelTransform, "OptionBButton");
            RemoveDirectChild(storyPanelTransform, "ContinueButton");
        }

        private static void RemoveDeprecatedDialoguePanelChildren(Transform dialoguePanelTransform)
        {
            RemoveDirectChild(dialoguePanelTransform, "SpeakerNameText");
            RemoveDirectChild(dialoguePanelTransform, "DialogueText");
            RemoveDirectChild(dialoguePanelTransform, "ContinueButton");
            RemoveDirectChild(dialoguePanelTransform, "ChoiceButtonA");
            RemoveDirectChild(dialoguePanelTransform, "ChoiceButtonB");
            RemoveDirectChild(dialoguePanelTransform, "SubmissionPanel");
        }

        private static void RemoveDirectChild(Transform parent, string childName)
        {
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index);
                if (child.name == childName)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
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

        private static Image FindOrCreateImage(Transform parent, string name)
        {
            var imageObject = FindOrCreateUiChild(parent, name);
            var image = EnsureComponent<Image>(imageObject);
            image.color = new Color(0.17f, 0.17f, 0.16f, 1f);
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.enabled = false;
            return image;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            var buttonObject = FindOrCreateUiChild(parent, name);
            var image = EnsureComponent<Image>(buttonObject);
            image.color = new Color(0.24f, 0.24f, 0.22f, 1f);

            var button = EnsureComponent<Button>(buttonObject);
            button.targetGraphic = image;

            var labelText = FindOrCreateText(buttonObject.transform, "Label", label, 14, FontStyles.Bold, TextAlignmentOptions.Center);
            SetFullStretchRect(labelText.rectTransform);
            return button;
        }

        private static Button CreateTransparentButton(Transform parent, string name)
        {
            var buttonObject = FindOrCreateUiChild(parent, name);
            var image = EnsureComponent<Image>(buttonObject);
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            var button = EnsureComponent<Button>(buttonObject);
            button.targetGraphic = image;
            return button;
        }

        private static Image[] CreateComicPanelImages(Transform parent)
        {
            var gridObject = FindOrCreateUiChild(parent, "ComicPanelGrid");
            var gridRect = gridObject.GetComponent<RectTransform>();
            SetFixedRect(gridRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 64f), new Vector2(1320f, 620f));

            var grid = EnsureComponent<GridLayoutGroup>(gridObject);
            grid.cellSize = new Vector2(400f, 190f);
            grid.spacing = new Vector2(18f, 18f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.MiddleCenter;

            var images = new Image[6];
            for (var index = 0; index < images.Length; index++)
            {
                var panelImage = FindOrCreateImage(gridObject.transform, $"ComicPanelImage{index + 1}");
                panelImage.raycastTarget = false;
                images[index] = panelImage;
            }

            return images;
        }

        private static void CreateDebugButton(Transform parent, string name, string label, StoryDebugControls target, string methodName)
        {
            var button = CreateButton(parent, name, label);
            AddPersistentListenerIfMissing(button, target, methodName, CreateDebugAction(target, methodName));
        }

        private static UnityAction CreateDebugAction(StoryDebugControls target, string methodName)
        {
            switch (methodName)
            {
                case nameof(StoryDebugControls.StartNextQueuedStory):
                    return target.StartNextQueuedStory;
                case nameof(StoryDebugControls.StartDemoDialogue):
                    return target.StartDemoDialogue;
                case nameof(StoryDebugControls.StartDemoText):
                    return target.StartDemoText;
                case nameof(StoryDebugControls.StartDemoImage):
                    return target.StartDemoImage;
                case nameof(StoryDebugControls.StartDemoComicImage):
                    return target.StartDemoComicImage;
                case nameof(StoryDebugControls.StartDemoSubmission):
                    return target.StartDemoSubmission;
                case nameof(StoryDebugControls.RefreshStories):
                    return target.RefreshStories;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(methodName), methodName, "Unsupported story debug method.");
            }
        }

        private static void ConfigurePanelRect(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var image = EnsureComponent<Image>(rectTransform.gameObject);
            image.color = new Color(0.1f, 0.1f, 0.095f, 0.94f);
            image.raycastTarget = false;
        }

        private static void ConfigureContentPanel(RectTransform rectTransform)
        {
            SetFullStretchRect(rectTransform);
        }

        private static void ConfigureDialoguePanel(RectTransform rectTransform)
        {
            SetFullStretchRect(rectTransform);

            var image = EnsureComponent<Image>(rectTransform.gameObject);
            image.color = new Color(0f, 0f, 0f, 0.86f);
            image.raycastTarget = false;
        }

        private static void ConfigureDialogueBar(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = new Vector2(0f, 216f);

            var image = EnsureComponent<Image>(rectTransform.gameObject);
            image.color = new Color(0.06f, 0.06f, 0.055f, 0.98f);
            image.raycastTarget = false;
        }

        private static void ConfigureSubmissionPanel(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(280f, 0f);
            rectTransform.offsetMax = new Vector2(-280f, 0f);

            var image = EnsureComponent<Image>(rectTransform.gameObject);
            image.color = new Color(0.09f, 0.085f, 0.075f, 0.98f);
            image.raycastTarget = false;
        }

        private static void ConfigureDebugRoot(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            rectTransform.anchoredPosition = new Vector2(-24f, 24f);
            rectTransform.sizeDelta = new Vector2(180f, 320f);

            var grid = EnsureComponent<GridLayoutGroup>(rectTransform.gameObject);
            grid.cellSize = new Vector2(180f, 36f);
            grid.spacing = new Vector2(0f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 1;
        }

        private static void SetFullStretchRect(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetFixedRect(
            RectTransform rectTransform,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        }

        private static void SetTopStretchRect(RectTransform rectTransform, float left, float top, float right, float height)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.offsetMin = new Vector2(left, -top - Mathf.Abs(height));
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static void SetBottomStretchRect(RectTransform rectTransform, float left, float bottom, float right, float height)
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, bottom + Mathf.Abs(height));
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

        private static void ConfigureInventoryService(InventoryService inventoryService, ConfigManager configManager, RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(inventoryService);
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

        private static void ConfigureStoryPanel(
            GameObject storyPanel,
            StoryService storyService,
            CanvasGroup rootCanvasGroup,
            TMP_Text titleText,
            TMP_Text feedbackText,
            Button storyAreaButton,
            GameObject dialoguePanel,
            Image leftPortrait,
            Image rightPortrait,
            Image speakerExpressionImage,
            TMP_Text speakerNameText,
            TMP_Text dialogueText,
            Button dialogueContinueButton,
            Button choiceButtonA,
            Button choiceButtonB,
            GameObject submissionPanel,
            TMP_Text submissionTitleText,
            TMP_Text submissionRequirementText,
            Button submitButton,
            Button exitSubmitButton,
            GameObject imageStoryPanel,
            Image storyImage,
            Image[] comicPanelImages,
            TMP_Text imageCaptionText,
            Button imageContinueButton,
            GameObject textStoryPanel,
            TMP_Text textContent,
            Button textContinueButton)
        {
            var panelView = EnsureComponent<StoryPanelView>(storyPanel);
            var serializedObject = new SerializedObject(panelView);
            serializedObject.FindProperty("storyService").objectReferenceValue = storyService;
            serializedObject.FindProperty("rootCanvasGroup").objectReferenceValue = rootCanvasGroup;
            serializedObject.FindProperty("titleText").objectReferenceValue = titleText;
            serializedObject.FindProperty("feedbackText").objectReferenceValue = feedbackText;
            serializedObject.FindProperty("storyAreaButton").objectReferenceValue = storyAreaButton;
            serializedObject.FindProperty("dialoguePanel").objectReferenceValue = dialoguePanel;
            serializedObject.FindProperty("leftPortrait").objectReferenceValue = leftPortrait;
            serializedObject.FindProperty("rightPortrait").objectReferenceValue = rightPortrait;
            serializedObject.FindProperty("speakerExpressionImage").objectReferenceValue = speakerExpressionImage;
            serializedObject.FindProperty("speakerNameText").objectReferenceValue = speakerNameText;
            serializedObject.FindProperty("dialogueText").objectReferenceValue = dialogueText;
            serializedObject.FindProperty("dialogueContinueButton").objectReferenceValue = dialogueContinueButton;
            serializedObject.FindProperty("dialogueContinueButtonText").objectReferenceValue = dialogueContinueButton.transform.Find("Label").GetComponent<TMP_Text>();
            serializedObject.FindProperty("choiceButtonA").objectReferenceValue = choiceButtonA;
            serializedObject.FindProperty("choiceButtonB").objectReferenceValue = choiceButtonB;
            serializedObject.FindProperty("choiceButtonAText").objectReferenceValue = choiceButtonA.transform.Find("Label").GetComponent<TMP_Text>();
            serializedObject.FindProperty("choiceButtonBText").objectReferenceValue = choiceButtonB.transform.Find("Label").GetComponent<TMP_Text>();
            serializedObject.FindProperty("submissionPanel").objectReferenceValue = submissionPanel;
            serializedObject.FindProperty("submissionTitleText").objectReferenceValue = submissionTitleText;
            serializedObject.FindProperty("submissionRequirementText").objectReferenceValue = submissionRequirementText;
            serializedObject.FindProperty("submitButton").objectReferenceValue = submitButton;
            serializedObject.FindProperty("submitButtonText").objectReferenceValue = submitButton.transform.Find("Label").GetComponent<TMP_Text>();
            serializedObject.FindProperty("exitSubmitButton").objectReferenceValue = exitSubmitButton;
            serializedObject.FindProperty("exitSubmitButtonText").objectReferenceValue = exitSubmitButton.transform.Find("Label").GetComponent<TMP_Text>();
            serializedObject.FindProperty("imageStoryPanel").objectReferenceValue = imageStoryPanel;
            serializedObject.FindProperty("storyImage").objectReferenceValue = storyImage;
            var comicImagesProperty = serializedObject.FindProperty("comicPanelImages");
            comicImagesProperty.arraySize = comicPanelImages.Length;
            for (var index = 0; index < comicPanelImages.Length; index++)
            {
                comicImagesProperty.GetArrayElementAtIndex(index).objectReferenceValue = comicPanelImages[index];
            }
            serializedObject.FindProperty("imageCaptionText").objectReferenceValue = imageCaptionText;
            serializedObject.FindProperty("imageContinueButton").objectReferenceValue = imageContinueButton;
            serializedObject.FindProperty("imageContinueButtonText").objectReferenceValue = imageContinueButton.transform.Find("Label").GetComponent<TMP_Text>();
            serializedObject.FindProperty("textStoryPanel").objectReferenceValue = textStoryPanel;
            serializedObject.FindProperty("textContent").objectReferenceValue = textContent;
            serializedObject.FindProperty("textContinueButton").objectReferenceValue = textContinueButton;
            serializedObject.FindProperty("textContinueButtonText").objectReferenceValue = textContinueButton.transform.Find("Label").GetComponent<TMP_Text>();
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDebugControls(StoryDebugControls debugControls, StoryService storyService, TMP_Text feedbackText)
        {
            var serializedObject = new SerializedObject(debugControls);
            serializedObject.FindProperty("storyService").objectReferenceValue = storyService;
            serializedObject.FindProperty("demoDialogueStoryId").stringValue = "story_relief_start";
            serializedObject.FindProperty("demoTextStoryId").stringValue = "story_demo_text";
            serializedObject.FindProperty("demoImageStoryId").stringValue = "story_demo_image";
            serializedObject.FindProperty("demoComicImageStoryId").stringValue = "story_demo_comic_image";
            serializedObject.FindProperty("demoSubmissionStoryId").stringValue = "story_demo_submission";
            serializedObject.FindProperty("feedbackText").objectReferenceValue = feedbackText;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
