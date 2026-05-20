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
            if (configManager == null ||
                runtimeDataService == null ||
                inventoryService == null ||
                factionService == null ||
                taskService == null)
            {
                return false;
            }

            var serializedObject = new SerializedObject(documentService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.FindProperty("factionService").objectReferenceValue = factionService;
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
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
            SetFixedRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660f, 520f), new Vector2(0.5f, 0.5f));
            ConfigurePanelImage(panel, new Color(0.13f, 0.12f, 0.105f, 0.98f));

            var titleText = CreateText(panel.transform, "TitleText", "Document", 24, FontStyles.Bold, TextAlignmentOptions.Left);
            SetFixedRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(604f, 40f), new Vector2(0f, 1f));

            var bodyText = CreateText(panel.transform, "BodyText", "", 17, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            bodyText.overflowMode = TextOverflowModes.Overflow;
            SetFixedRect(bodyText.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -84f), new Vector2(604f, 280f), new Vector2(0f, 1f));

            var optionAButton = CreateButton(panel.transform, "OptionAButton", "Option A");
            SetFixedRect(optionAButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(28f, 78f), new Vector2(288f, 48f));

            var optionBButton = CreateButton(panel.transform, "OptionBButton", "Option B");
            SetFixedRect(optionBButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-28f, 78f), new Vector2(288f, 48f), new Vector2(1f, 0f));

            var feedback = CreateText(panel.transform, "ProposerFeedbackText", "", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            SetFixedRect(feedback.rectTransform, new Vector2(0f, 0f), new Vector2(28f, 24f), new Vector2(500f, 46f));

            var stamp = CreateImage(panel.transform, "StampImage", new Color(0.65f, 0.18f, 0.14f, 0.72f));
            SetFixedRect(stamp.rectTransform, new Vector2(1f, 0f), new Vector2(-28f, 24f), new Vector2(72f, 72f), new Vector2(1f, 0f));
            stamp.enabled = false;

            var panelView = EnsureComponent<DocumentPopupPanelView>(panel);
            var serializedObject = new SerializedObject(panelView);
            serializedObject.FindProperty("documentService").objectReferenceValue = documentService;
            serializedObject.FindProperty("sharedActorSlot").objectReferenceValue = sharedActorSlot;
            serializedObject.FindProperty("titleText").objectReferenceValue = titleText;
            serializedObject.FindProperty("bodyText").objectReferenceValue = bodyText;
            serializedObject.FindProperty("optionAText").objectReferenceValue = optionAButton.transform.Find("Label").GetComponent<TMP_Text>();
            serializedObject.FindProperty("optionBText").objectReferenceValue = optionBButton.transform.Find("Label").GetComponent<TMP_Text>();
            serializedObject.FindProperty("proposerFeedbackText").objectReferenceValue = feedback;
            serializedObject.FindProperty("stampImage").objectReferenceValue = stamp;
            serializedObject.FindProperty("optionAButton").objectReferenceValue = optionAButton;
            serializedObject.FindProperty("optionBButton").objectReferenceValue = optionBButton;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AddPersistentListenerIfMissing(optionAButton, panelView, nameof(DocumentPopupPanelView.OnOptionAClicked), panelView.OnOptionAClicked);
            AddPersistentListenerIfMissing(optionBButton, panelView, nameof(DocumentPopupPanelView.OnOptionBClicked), panelView.OnOptionBClicked);
            return panel;
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
