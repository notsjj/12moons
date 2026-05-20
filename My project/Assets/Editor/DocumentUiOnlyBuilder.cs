using TMPro;
using TwelveMoons.Core;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools
{
    public static class DocumentUiOnlyBuilder
    {
        [MenuItem("Twelve Moons/Setup/Create Document UI Only")]
        public static void CreateDocumentUiOnly()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Fail("Canvas not found. Create Document UI Only requires the existing desk Canvas and will not create one.");
                return;
            }

            var deskPanel = canvas.transform.Find("DeskPanel");
            if (deskPanel == null)
            {
                Fail("DeskPanel not found under the existing Canvas. Create Document UI Only will not rebuild the desk UI.");
                return;
            }

            if (deskPanel.Find("DocumentPopupPanel") != null)
            {
                Fail("DeskPanel already has DocumentPopupPanel. Delete only that object first, then run Create Document UI Only.");
                return;
            }

            var gameEntry = Object.FindFirstObjectByType<GameEntry>();
            if (gameEntry == null)
            {
                Fail("GameEntry not found. Create Document UI Only will not create or rebuild core scene objects.");
                return;
            }

            var sharedActorSlot = FindSharedActorSlot(deskPanel);
            if (sharedActorSlot == null)
            {
                Fail("SharedActorSlot not found under DeskPanel. Create Document UI Only will not create it because it belongs to another UI stage.");
                return;
            }

            if (!TryResolveDocumentService(gameEntry, out var documentService))
            {
                return;
            }

            var documentPopupPanel = BuildDocumentPopupPanel(deskPanel, documentService, sharedActorSlot);
            UpdateDeskPanelDocumentReference(deskPanel, documentPopupPanel.GetComponent<DocumentPopupPanelView>());

            documentPopupPanel.SetActive(false);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = documentPopupPanel;
            Debug.Log("Document UI only setup completed. Only DeskPanel/DocumentPopupPanel was created and bound.");
        }

        private static bool TryResolveDocumentService(GameEntry gameEntry, out DocumentService documentService)
        {
            documentService = EnsureComponent<DocumentService>(gameEntry.gameObject);

            var configManager = FindRequired<ConfigManager>("ConfigManager");
            var runtimeDataService = FindRequired<RuntimeDataService>("RuntimeDataService");
            var inventoryService = FindRequired<InventoryService>("InventoryService");
            var factionService = FindRequired<FactionService>("FactionService");
            var taskService = FindRequired<TaskService>("TaskService");
            var roundService = FindRequired<RoundService>("RoundService");
            if (configManager == null ||
                runtimeDataService == null ||
                inventoryService == null ||
                factionService == null ||
                taskService == null ||
                roundService == null)
            {
                return false;
            }

            var serializedObject = new SerializedObject(documentService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.FindProperty("factionService").objectReferenceValue = factionService;
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.FindProperty("roundService").objectReferenceValue = roundService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static T FindRequired<T>(string label) where T : Object
        {
            var target = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (target == null)
            {
                Fail($"{label} not found. Create Document UI Only only binds existing stage services and will not rebuild other systems.");
            }

            return target;
        }

        private static SharedActorSlotView FindSharedActorSlot(Transform deskPanel)
        {
            var slot = deskPanel.Find("SharedActorSlot");
            return slot == null ? null : slot.GetComponent<SharedActorSlotView>();
        }

        private static GameObject BuildDocumentPopupPanel(
            Transform parent,
            DocumentService documentService,
            SharedActorSlotView sharedActorSlot)
        {
            var panel = CreateUiChild(parent, "DocumentPopupPanel");
            SetFixedRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 560f), new Vector2(0.5f, 0.5f));
            ConfigurePanelImage(panel, new Color(0f, 0f, 0f, 0f));

            var rightScrollEnd = CreateImage(panel.transform, "RightScrollEndImage", new Color(0.33f, 0.21f, 0.1f, 1f));
            SetFixedRect(rightScrollEnd.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(340f, 0f), new Vector2(80f, 520f), new Vector2(0.5f, 0.5f));

            var contentViewport = CreateUiChild(panel.transform, "ContentViewport");
            SetFixedRect(contentViewport.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-50f, 0f), new Vector2(700f, 500f), new Vector2(0.5f, 0.5f));
            contentViewport.AddComponent<RectMask2D>();

            var contentRoot = CreateUiChild(contentViewport.transform, "ContentRoot");
            var contentGroup = contentRoot.AddComponent<CanvasGroup>();
            SetFixedRect(contentRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 500f), new Vector2(0.5f, 0.5f));

            var background = CreateImage(contentRoot.transform, "ContentBackgroundImage", new Color(0.76f, 0.68f, 0.48f, 1f));
            SetFixedRect(background.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 500f), new Vector2(0.5f, 0.5f));

            var titleText = CreateText(contentRoot.transform, "TitleText", "公文", 26, FontStyles.Bold, TextAlignmentOptions.Center);
            titleText.color = new Color(0.16f, 0.09f, 0.04f, 1f);
            SetFixedRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(560f, 42f), new Vector2(0.5f, 1f));

            var bodyText = CreateText(contentRoot.transform, "BodyText", "", 18, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            bodyText.color = new Color(0.16f, 0.09f, 0.04f, 1f);
            bodyText.overflowMode = TextOverflowModes.Overflow;
            SetFixedRect(bodyText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(580f, 250f), new Vector2(0.5f, 1f));

            var submitPanel = CreateUiChild(contentRoot.transform, "SubmitCardSlot");
            SetFixedRect(submitPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 132f), new Vector2(300f, 56f), new Vector2(0.5f, 0f));
            ConfigurePanelImage(submitPanel, new Color(0.22f, 0.14f, 0.08f, 0.78f));
            var submitStatus = CreateText(submitPanel.transform, "StatusText", "", 14, FontStyles.Normal, TextAlignmentOptions.Center);
            SetFixedRect(submitStatus.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 30f), new Vector2(0.5f, 0.5f));
            var submitSlot = EnsureComponent<DocumentSubmitSlot>(submitPanel);
            var submitSerializedObject = new SerializedObject(submitSlot);
            submitSerializedObject.FindProperty("statusText").objectReferenceValue = submitStatus;
            submitSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            var optionAButton = CreateButton(contentRoot.transform, "OptionAButton", "选项一");
            SetFixedRect(optionAButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(70f, 62f), new Vector2(260f, 54f));
            var optionAStamp = CreateImage(optionAButton.transform, "StampImage", new Color(0.65f, 0.08f, 0.04f, 0.72f));
            SetFixedRect(optionAStamp.rectTransform, new Vector2(1f, 0.5f), new Vector2(-34f, 0f), new Vector2(64f, 64f), new Vector2(0.5f, 0.5f));
            optionAStamp.enabled = false;

            var optionBButton = CreateButton(contentRoot.transform, "OptionBButton", "选项二");
            SetFixedRect(optionBButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-70f, 62f), new Vector2(260f, 54f), new Vector2(1f, 0f));
            var optionBStamp = CreateImage(optionBButton.transform, "StampImage", new Color(0.65f, 0.08f, 0.04f, 0.72f));
            SetFixedRect(optionBStamp.rectTransform, new Vector2(1f, 0.5f), new Vector2(-34f, 0f), new Vector2(64f, 64f), new Vector2(0.5f, 0.5f));
            optionBStamp.enabled = false;

            var feedback = CreateText(contentRoot.transform, "ProposerFeedbackText", "", 14, FontStyles.Normal, TextAlignmentOptions.Left);
            feedback.color = new Color(0.16f, 0.09f, 0.04f, 1f);
            SetFixedRect(feedback.rectTransform, new Vector2(0f, 0f), new Vector2(70f, 18f), new Vector2(420f, 34f));

            var flowStatus = CreateText(contentRoot.transform, "FlowStatusText", "", 13, FontStyles.Normal, TextAlignmentOptions.Right);
            flowStatus.color = new Color(0.16f, 0.09f, 0.04f, 1f);
            SetFixedRect(flowStatus.rectTransform, new Vector2(1f, 0f), new Vector2(-70f, 18f), new Vector2(260f, 28f), new Vector2(1f, 0f));

            var leftScrollEnd = CreateImage(panel.transform, "LeftScrollEndImage", new Color(0.33f, 0.21f, 0.1f, 1f));
            SetFixedRect(leftScrollEnd.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-340f, 0f), new Vector2(80f, 520f), new Vector2(0.5f, 0.5f));

            var panelView = EnsureComponent<DocumentPopupPanelView>(panel);
            var serializedObject = new SerializedObject(panelView);
            serializedObject.FindProperty("documentService").objectReferenceValue = documentService;
            serializedObject.FindProperty("sharedActorSlot").objectReferenceValue = sharedActorSlot;
            serializedObject.FindProperty("suspicionPanel").objectReferenceValue = FindSuspicionPanel(parent);
            serializedObject.FindProperty("leftScrollEnd").objectReferenceValue = leftScrollEnd.rectTransform;
            serializedObject.FindProperty("rightScrollEnd").objectReferenceValue = rightScrollEnd.rectTransform;
            serializedObject.FindProperty("contentRoot").objectReferenceValue = contentRoot.GetComponent<RectTransform>();
            serializedObject.FindProperty("contentGroup").objectReferenceValue = contentGroup;
            serializedObject.FindProperty("scrollMoveLeftDistance").floatValue = 700f;
            serializedObject.FindProperty("scrollTweenDuration").floatValue = 0.8f;
            serializedObject.FindProperty("contentBackgroundImage").objectReferenceValue = background;
            serializedObject.FindProperty("titleText").objectReferenceValue = titleText;
            serializedObject.FindProperty("bodyText").objectReferenceValue = bodyText;
            serializedObject.FindProperty("optionAText").objectReferenceValue = optionAButton.transform.Find("Label").GetComponent<TMP_Text>();
            serializedObject.FindProperty("optionBText").objectReferenceValue = optionBButton.transform.Find("Label").GetComponent<TMP_Text>();
            serializedObject.FindProperty("proposerFeedbackText").objectReferenceValue = feedback;
            serializedObject.FindProperty("flowStatusText").objectReferenceValue = flowStatus;
            serializedObject.FindProperty("optionAStampImage").objectReferenceValue = optionAStamp;
            serializedObject.FindProperty("optionBStampImage").objectReferenceValue = optionBStamp;
            serializedObject.FindProperty("optionAButton").objectReferenceValue = optionAButton;
            serializedObject.FindProperty("optionBButton").objectReferenceValue = optionBButton;
            serializedObject.FindProperty("submitPanel").objectReferenceValue = submitPanel;
            serializedObject.FindProperty("submitSlot").objectReferenceValue = submitSlot;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AddPersistentListenerIfMissing(optionAButton, panelView, nameof(DocumentPopupPanelView.OnOptionAClicked), panelView.OnOptionAClicked);
            AddPersistentListenerIfMissing(optionBButton, panelView, nameof(DocumentPopupPanelView.OnOptionBClicked), panelView.OnOptionBClicked);
            return panel;
        }

        private static SuspicionPanelView FindSuspicionPanel(Transform deskPanel)
        {
            var panel = deskPanel.Find("SuspicionPanel");
            return panel == null ? null : panel.GetComponent<SuspicionPanelView>();
        }

        private static void UpdateDeskPanelDocumentReference(Transform deskPanel, DocumentPopupPanelView documentPopupPanel)
        {
            var deskPanelView = deskPanel.GetComponent<DeskPanelView>();
            if (deskPanelView == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(deskPanelView);
            serializedObject.FindProperty("documentPopupPanel").objectReferenceValue = documentPopupPanel;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateUiChild(Transform parent, string childName)
        {
            var childObject = new GameObject(childName, typeof(RectTransform));
            childObject.transform.SetParent(parent, false);
            return childObject;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            var textObject = CreateUiChild(parent, name);
            var text = textObject.AddComponent<TextMeshProUGUI>();
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

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var imageObject = CreateUiChild(parent, name);
            var image = imageObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            var buttonObject = CreateUiChild(parent, name);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.24f, 0.24f, 0.21f, 1f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var labelText = CreateText(buttonObject.transform, "Label", label, 14, FontStyles.Bold, TextAlignmentOptions.Center);
            SetFixedRect(labelText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 28f), new Vector2(0.5f, 0.5f));
            return button;
        }

        private static void SetFixedRect(RectTransform rectTransform, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            SetFixedRect(rectTransform, anchor, anchoredPosition, size, anchor);
        }

        private static void SetFixedRect(RectTransform rectTransform, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        }

        private static void ConfigurePanelImage(GameObject target, Color color)
        {
            var image = target.GetComponent<Image>();
            if (image == null)
            {
                image = target.AddComponent<Image>();
            }

            image.color = color;
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

        private static void Fail(string message)
        {
            Debug.LogError(message);
            EditorUtility.DisplayDialog("Create Document UI Only", message, "OK");
        }
    }
}
