using System.IO;
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
    public static class DeskUiBuilder
    {
        private const string InventoryCardPrefabPath = "Assets/Prefabs/UI/InventoryItemCard.prefab";

        [MenuItem("Twelve Moons/Setup/Create Desk UI Framework")]
        public static void CreateDeskUiFramework()
        {
            var canvas = FindOrCreateCanvas();
            FindOrCreateEventSystem();

            var gameEntry = FindOrCreateGameEntry();
            var configManager = EnsureComponent<ConfigManager>(gameEntry.gameObject);
            var runtimeDataService = EnsureComponent<RuntimeDataService>(gameEntry.gameObject);
            var inventoryService = EnsureComponent<InventoryService>(gameEntry.gameObject);
            var roundService = EnsureComponent<RoundService>(gameEntry.gameObject);
            var taskService = EnsureComponent<TaskService>(gameEntry.gameObject);
            var factionService = EnsureComponent<FactionService>(gameEntry.gameObject);
            var letterService = EnsureComponent<LetterService>(gameEntry.gameObject);
            var documentService = EnsureComponent<DocumentService>(gameEntry.gameObject);

            ConfigureConfigManager(configManager);
            ConfigureRuntimeDataService(runtimeDataService, configManager);
            ConfigureInventoryService(inventoryService, configManager, runtimeDataService);
            ConfigureRoundService(roundService, configManager, runtimeDataService);
            ConfigureTaskService(taskService, configManager, runtimeDataService, roundService);
            ConfigureFactionService(factionService, configManager, runtimeDataService);
            ConfigureLetterService(letterService, configManager, runtimeDataService);
            ConfigureDocumentService(documentService, configManager, runtimeDataService, inventoryService, factionService, taskService, roundService);

            var deskPanel = FindOrCreateUiChild(canvas.transform, "DeskPanel");
            ConfigureFullScreenRect(deskPanel.GetComponent<RectTransform>());
            ConfigurePanelImage(deskPanel, new Color(0.075f, 0.073f, 0.066f, 0.98f));

            var inventoryPanel = BuildInventoryPanel(deskPanel.transform, inventoryService, runtimeDataService);
            var taskPanel = BuildTaskPanel(deskPanel.transform, taskService, runtimeDataService);
            var suspicionPanel = BuildSuspicionPanel(deskPanel.transform, factionService, runtimeDataService);
            var letterArea = BuildLetterArea(deskPanel.transform, letterService);
            var sharedActorSlot = BuildSharedActorSlot(deskPanel.transform);
            var documentPopupPanel = BuildDocumentPopupPanel(
                deskPanel.transform,
                documentService,
                inventoryService,
                sharedActorSlot.GetComponent<SharedActorSlotView>(),
                suspicionPanel.GetComponent<SuspicionPanelView>());

            var deskPanelView = EnsureComponent<DeskPanelView>(deskPanel);
            ConfigureDeskPanelView(
                deskPanelView,
                taskPanel,
                suspicionPanel,
                letterArea,
                inventoryPanel,
                sharedActorSlot,
                documentPopupPanel);

            var testPanel = BuildTestPanel(
                deskPanel.transform,
                deskPanelView,
                sharedActorSlot.GetComponent<SharedActorSlotView>(),
                documentPopupPanel.GetComponent<DocumentPopupPanelView>(),
                inventoryService,
                taskService,
                roundService,
                runtimeDataService,
                factionService,
                letterService,
                documentService);

            ConfigureGameEntry(gameEntry, deskPanel);
            documentPopupPanel.SetActive(false);
            testPanel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = deskPanel;
            Debug.Log("Desk UI framework setup completed. Formal desk panels contain no test buttons; TestPanel is hidden by default.");
        }

        private static GameObject BuildInventoryPanel(
            Transform parent,
            InventoryService inventoryService,
            RuntimeDataService runtimeDataService)
        {
            var panel = FindOrCreateUiChild(parent, "InventoryPanel");
            ClearFormalDebugArtifacts(panel);
            SetFixedRect(panel.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(520f, 300f));
            ConfigurePanelImage(panel, new Color(0.11f, 0.105f, 0.09f, 0.94f));

            var title = FindOrCreateText(panel.transform, "TitleText", "Inventory", 20, FontStyles.Bold, TextAlignmentOptions.Left);
            SetFixedRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(484f, 32f), new Vector2(0f, 1f));

            var content = FindOrCreateUiChild(panel.transform, "InventoryContent");
            SetFixedRect(content.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(16f, 16f), new Vector2(488f, 224f));
            RemoveLayoutGroups(content);

            var panelView = EnsureComponent<InventoryPanelView>(panel);
            ConfigureInventoryPanelView(panelView, inventoryService, runtimeDataService, content, LoadOrCreateInventoryCardPrefab());
            return panel;
        }

        private static GameObject BuildTaskPanel(
            Transform parent,
            TaskService taskService,
            RuntimeDataService runtimeDataService)
        {
            var panel = FindOrCreateUiChild(parent, "TaskPanel");
            ClearFormalDebugArtifacts(panel);
            SetFixedRect(panel.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(420f, 610f), new Vector2(1f, 0f));
            ConfigurePanelImage(panel, new Color(0.11f, 0.11f, 0.1f, 0.94f));

            var title = FindOrCreateText(panel.transform, "TitleText", "Tasks", 20, FontStyles.Bold, TextAlignmentOptions.Left);
            SetFixedRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(388f, 32f), new Vector2(0f, 1f));

            var emptyText = FindOrCreateText(panel.transform, "EmptyText", "No active task.", 14, FontStyles.Normal, TextAlignmentOptions.Left);
            SetFixedRect(emptyText.rectTransform, new Vector2(0f, 1f), new Vector2(16f, -52f), new Vector2(388f, 28f), new Vector2(0f, 1f));

            var content = FindOrCreateUiChild(panel.transform, "TaskContent");
            SetFixedRect(content.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(16f, -90f), new Vector2(388f, 496f), new Vector2(0f, 1f));
            ConfigureVerticalList(content, 8f);

            var panelView = EnsureComponent<TaskPanelView>(panel);
            ConfigureTaskPanelView(panelView, taskService, runtimeDataService, content, emptyText);
            return panel;
        }

        private static GameObject BuildSuspicionPanel(
            Transform parent,
            FactionService factionService,
            RuntimeDataService runtimeDataService)
        {
            var panel = FindOrCreateUiChild(parent, "SuspicionPanel");
            ClearFormalDebugArtifacts(panel);
            SetFixedRect(panel.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(620f, 340f), new Vector2(1f, 1f));
            ConfigurePanelImage(panel, new Color(0.1f, 0.1f, 0.09f, 0.94f));

            var title = FindOrCreateText(panel.transform, "TitleText", "Faction Suspicion", 18, FontStyles.Bold, TextAlignmentOptions.Left);
            SetFixedRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(588f, 30f), new Vector2(0f, 1f));

            var content = FindOrCreateUiChild(panel.transform, "SuspicionContent");
            SetFixedRect(content.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(16f, -58f), new Vector2(360f, 226f), new Vector2(0f, 1f));
            ConfigureVerticalList(content, 8f);
            var factionRows = CreateHierarchyFactionRows(content.transform);

            var pointer = FindOrCreateText(panel.transform, "SuspicionPointerIcon", "☞", 28, FontStyles.Bold, TextAlignmentOptions.Center);
            pointer.color = new Color(0.95f, 0.82f, 0.38f, 1f);
            SetFixedRect(pointer.rectTransform, new Vector2(0f, 1f), new Vector2(394f, -86f), new Vector2(38f, 38f), new Vector2(0.5f, 0.5f));

            var feedback = FindOrCreateText(panel.transform, "FactionFeedbackText", "", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            SetFixedRect(feedback.rectTransform, new Vector2(1f, 1f), new Vector2(-18f, -58f), new Vector2(170f, 226f), new Vector2(1f, 1f));

            var panelView = EnsureComponent<SuspicionPanelView>(panel);
            ConfigureSuspicionPanelView(panelView, factionService, runtimeDataService, content, factionRows, pointer.rectTransform, feedback);
            return panel;
        }

        private static GameObject BuildLetterArea(Transform parent, LetterService letterService)
        {
            var panel = FindOrCreateUiChild(parent, "LetterArea");
            ClearFormalDebugArtifacts(panel);
            SetFixedRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, 42f), new Vector2(820f, 720f), new Vector2(0.5f, 0.5f));
            ConfigurePanelImage(panel, new Color(0.105f, 0.102f, 0.09f, 0.94f));

            var title = FindOrCreateText(panel.transform, "HeaderText", "Letters", 22, FontStyles.Bold, TextAlignmentOptions.Left);
            SetFixedRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -18f), new Vector2(772f, 34f), new Vector2(0f, 1f));

            var listPanel = FindOrCreateUiChild(panel.transform, "LetterListPanel");
            SetFixedRect(listPanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(24f, -70f), new Vector2(330f, 600f), new Vector2(0f, 1f));
            ConfigurePanelImage(listPanel, new Color(0.13f, 0.13f, 0.12f, 0.96f));

            var emptyText = FindOrCreateText(listPanel.transform, "EmptyText", "No letters received.", 15, FontStyles.Normal, TextAlignmentOptions.Center);
            SetFixedRect(emptyText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 60f), new Vector2(0.5f, 0.5f));

            var listRoot = FindOrCreateUiChild(listPanel.transform, "ListRoot");
            SetFixedRect(listRoot.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(294f, 564f), new Vector2(0f, 1f));
            ConfigureLetterIconGrid(listRoot);

            var rowTemplate = FindOrCreateUiChild(panel.transform, "LetterRowTemplate");
            ConfigureLetterRowTemplate(rowTemplate);
            rowTemplate.SetActive(false);

            var readerPanel = FindOrCreateUiChild(panel.transform, "LetterReaderPanel");
            SetFixedRect(readerPanel.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-24f, -70f), new Vector2(420f, 600f), new Vector2(1f, 1f));
            ConfigurePanelImage(readerPanel, new Color(0.15f, 0.145f, 0.13f, 0.96f));

            var titleText = FindOrCreateText(readerPanel.transform, "TitleText", "Select a letter", 24, FontStyles.Bold, TextAlignmentOptions.Left);
            SetFixedRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(300f, 40f), new Vector2(0f, 1f));
            var closeButton = FindOrCreateButton(readerPanel.transform, "CloseButton", "Exit");
            SetFixedRect(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(70f, 36f), new Vector2(1f, 1f));
            var senderText = FindOrCreateText(readerPanel.transform, "SenderText", "", 15, FontStyles.Normal, TextAlignmentOptions.Left);
            SetFixedRect(senderText.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -70f), new Vector2(372f, 26f), new Vector2(0f, 1f));
            var roundText = FindOrCreateText(readerPanel.transform, "RoundText", "", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            SetFixedRect(roundText.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -100f), new Vector2(372f, 24f), new Vector2(0f, 1f));
            var bodyText = FindOrCreateText(readerPanel.transform, "BodyText", "", 18, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            bodyText.overflowMode = TextOverflowModes.Overflow;
            SetFixedRect(bodyText.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -140f), new Vector2(372f, 440f), new Vector2(0f, 1f));
            var feedbackText = FindOrCreateText(readerPanel.transform, "FeedbackText", "", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            SetFixedRect(feedbackText.rectTransform, new Vector2(0f, 0f), new Vector2(24f, 20f), new Vector2(372f, 28f));

            var areaView = EnsureComponent<LetterAreaView>(panel);
            ConfigureLetterAreaView(areaView, letterService, listRoot.transform, rowTemplate.GetComponent<LetterRowView>(), emptyText, readerPanel, titleText, senderText, roundText, bodyText, feedbackText);
            AddPersistentListenerIfMissing(closeButton, areaView, nameof(LetterAreaView.CloseSelectedLetter), areaView.CloseSelectedLetter);
            readerPanel.SetActive(false);
            return panel;
        }

        private static GameObject BuildSharedActorSlot(Transform parent)
        {
            var slot = FindOrCreateUiChild(parent, "SharedActorSlot");
            SetFixedRect(slot.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(-260f, 56f), new Vector2(240f, 280f), new Vector2(0f, 0.5f));
            ConfigurePanelImage(slot, new Color(0.12f, 0.115f, 0.1f, 0.96f));

            var canvasGroup = EnsureComponent<CanvasGroup>(slot);
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            var portrait = FindOrCreateImage(slot.transform, "PortraitImage", new Color(0.18f, 0.17f, 0.15f, 1f));
            SetFixedRect(portrait.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(192f, 174f), new Vector2(0.5f, 1f));

            var nameText = FindOrCreateText(slot.transform, "NameText", "", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            SetFixedRect(nameText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(208f, 30f), new Vector2(0.5f, 0f));

            var roleText = FindOrCreateText(slot.transform, "RoleText", "", 13, FontStyles.Normal, TextAlignmentOptions.Center);
            SetFixedRect(roleText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(208f, 24f), new Vector2(0.5f, 0f));

            var proposerFeedbackBackground = FindOrCreateImage(slot.transform, "ProposerFeedbackBackground", new Color(0.08f, 0.075f, 0.065f, 0.92f));
            SetFixedRect(proposerFeedbackBackground.rectTransform, new Vector2(1f, 0.5f), new Vector2(20f, 0f), new Vector2(244f, 144f), new Vector2(0f, 0.5f));
            var proposerFeedbackText = FindOrCreateText(proposerFeedbackBackground.transform, "ProposerFeedbackText", "", 13, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            StretchRect(proposerFeedbackText.rectTransform, new Vector2(14f, 12f), new Vector2(-14f, -12f));

            var slotView = EnsureComponent<SharedActorSlotView>(slot);
            var serializedObject = new SerializedObject(slotView);
            serializedObject.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            serializedObject.FindProperty("portraitImage").objectReferenceValue = portrait;
            serializedObject.FindProperty("nameText").objectReferenceValue = nameText;
            serializedObject.FindProperty("roleText").objectReferenceValue = roleText;
            serializedObject.FindProperty("proposerFeedbackText").objectReferenceValue = proposerFeedbackText;
            serializedObject.FindProperty("actorRoot").objectReferenceValue = slot.GetComponent<RectTransform>();
            serializedObject.FindProperty("hiddenMoveLeftDistance").floatValue = 284f;
            serializedObject.FindProperty("slideDuration").floatValue = 0.8f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return slot;
        }

        private static GameObject BuildDocumentPopupPanel(
            Transform parent,
            DocumentService documentService,
            InventoryService inventoryService,
            SharedActorSlotView sharedActorSlot,
            SuspicionPanelView suspicionPanel)
        {
            var panel = FindOrCreateUiChild(parent, "DocumentPopupPanel");
            SetFixedRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 560f), new Vector2(0.5f, 0.5f));
            ConfigurePanelImage(panel, new Color(0f, 0f, 0f, 0f));
            panel.GetComponent<Image>().raycastTarget = true;

            var rightScrollEnd = FindOrCreateImage(panel.transform, "RightScrollEndImage", new Color(0.33f, 0.21f, 0.1f, 1f));
            SetFixedRect(rightScrollEnd.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(340f, 0f), new Vector2(80f, 520f), new Vector2(0.5f, 0.5f));

            var contentViewport = FindOrCreateUiChild(panel.transform, "ContentViewport");
            SetFixedRect(contentViewport.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-50f, 0f), new Vector2(700f, 500f), new Vector2(0.5f, 0.5f));
            EnsureComponent<RectMask2D>(contentViewport);

            var contentRoot = FindOrCreateUiChild(contentViewport.transform, "ContentRoot");
            var contentGroup = EnsureComponent<CanvasGroup>(contentRoot);
            SetFixedRect(contentRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 500f), new Vector2(0.5f, 0.5f));

            var background = FindOrCreateImage(contentRoot.transform, "ContentBackgroundImage", new Color(0.76f, 0.68f, 0.48f, 1f));
            SetFixedRect(background.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 500f), new Vector2(0.5f, 0.5f));

            var titleText = FindOrCreateText(contentRoot.transform, "TitleText", "公文", 26, FontStyles.Bold, TextAlignmentOptions.Center);
            titleText.color = new Color(0.16f, 0.09f, 0.04f, 1f);
            SetFixedRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(560f, 42f), new Vector2(0.5f, 1f));

            var bodyText = FindOrCreateText(contentRoot.transform, "BodyText", "", 18, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            bodyText.color = new Color(0.16f, 0.09f, 0.04f, 1f);
            bodyText.overflowMode = TextOverflowModes.Overflow;
            SetFixedRect(bodyText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(580f, 250f), new Vector2(0.5f, 1f));

            var submitPanel = FindOrCreateUiChild(panel.transform, "SubmitCardSlot");
            SetFixedRect(submitPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-430f, 0f), new Vector2(150f, 360f), new Vector2(0.5f, 0.5f));
            ConfigurePanelImage(submitPanel, new Color(0f, 0f, 0f, 0f));
            submitPanel.GetComponent<Image>().raycastTarget = false;

            var slotImage = FindOrCreateImage(submitPanel.transform, "CardSlotImage", new Color(0.32f, 0.21f, 0.11f, 0.92f));
            SetFixedRect(slotImage.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(86f, 300f), new Vector2(0.5f, 0.5f));

            var dropArea = FindOrCreateImage(submitPanel.transform, "DropCardArea", new Color(0.12f, 0.08f, 0.04f, 0.78f));
            dropArea.raycastTarget = true;
            SetFixedRect(dropArea.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 150f), new Vector2(0.5f, 0.5f));
            DestroyChildIfExists(dropArea.transform, "SubmittedCardPreview");

            var submitStatus = FindOrCreateText(submitPanel.transform, "StatusText", "", 12, FontStyles.Normal, TextAlignmentOptions.Center);
            SetFixedRect(submitStatus.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(138f, 34f), new Vector2(0.5f, 0f));
            var submitSlot = EnsureComponent<DocumentSubmitSlot>(dropArea.gameObject);
            var submitSerializedObject = new SerializedObject(submitSlot);
            submitSerializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            submitSerializedObject.FindProperty("dropAreaImage").objectReferenceValue = dropArea;
            submitSerializedObject.FindProperty("submittedCardPrefab").objectReferenceValue = LoadOrCreateInventoryCardPrefab();
            submitSerializedObject.FindProperty("submittedCardSize").vector2Value = new Vector2(96f, 118f);
            submitSerializedObject.FindProperty("statusText").objectReferenceValue = submitStatus;
            submitSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            var optionAButton = FindOrCreateButton(contentRoot.transform, "OptionAButton", "选项一");
            SetFixedRect(optionAButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(70f, 62f), new Vector2(260f, 54f));
            var optionAStamp = FindOrCreateImage(optionAButton.transform, "StampImage", new Color(0.65f, 0.08f, 0.04f, 0.72f));
            SetFixedRect(optionAStamp.rectTransform, new Vector2(1f, 0.5f), new Vector2(-34f, 0f), new Vector2(64f, 64f), new Vector2(0.5f, 0.5f));
            optionAStamp.enabled = false;

            var optionBButton = FindOrCreateButton(contentRoot.transform, "OptionBButton", "选项二");
            SetFixedRect(optionBButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-70f, 62f), new Vector2(260f, 54f), new Vector2(1f, 0f));
            var optionBStamp = FindOrCreateImage(optionBButton.transform, "StampImage", new Color(0.65f, 0.08f, 0.04f, 0.72f));
            SetFixedRect(optionBStamp.rectTransform, new Vector2(1f, 0.5f), new Vector2(-34f, 0f), new Vector2(64f, 64f), new Vector2(0.5f, 0.5f));
            optionBStamp.enabled = false;

            TMP_Text feedback = null;

            var flowStatus = FindOrCreateText(contentRoot.transform, "FlowStatusText", "", 13, FontStyles.Normal, TextAlignmentOptions.Right);
            flowStatus.color = new Color(0.16f, 0.09f, 0.04f, 1f);
            SetFixedRect(flowStatus.rectTransform, new Vector2(1f, 0f), new Vector2(-70f, 18f), new Vector2(260f, 28f), new Vector2(1f, 0f));

            var leftScrollEnd = FindOrCreateImage(panel.transform, "LeftScrollEndImage", new Color(0.33f, 0.21f, 0.1f, 1f));
            SetFixedRect(leftScrollEnd.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-340f, 0f), new Vector2(80f, 520f), new Vector2(0.5f, 0.5f));

            var panelView = EnsureComponent<DocumentPopupPanelView>(panel);
            var serializedObject = new SerializedObject(panelView);
            serializedObject.FindProperty("documentService").objectReferenceValue = documentService;
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.FindProperty("sharedActorSlot").objectReferenceValue = sharedActorSlot;
            serializedObject.FindProperty("suspicionPanel").objectReferenceValue = suspicionPanel;
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

        private static GameObject BuildTestPanel(
            Transform parent,
            DeskPanelView deskPanelView,
            SharedActorSlotView sharedActorSlot,
            DocumentPopupPanelView documentPopupPanel,
            InventoryService inventoryService,
            TaskService taskService,
            RoundService roundService,
            RuntimeDataService runtimeDataService,
            FactionService factionService,
            LetterService letterService,
            DocumentService documentService)
        {
            var panel = FindOrCreateUiChild(parent, "TestPanel");
            SetFixedRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 660f), new Vector2(0.5f, 0.5f));
            ConfigurePanelImage(panel, new Color(0.08f, 0.085f, 0.09f, 0.98f));

            var title = FindOrCreateText(panel.transform, "TitleText", "Desk Test Panel", 22, FontStyles.Bold, TextAlignmentOptions.Left);
            SetFixedRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(520f, 34f), new Vector2(0f, 1f));

            var feedback = FindOrCreateText(panel.transform, "FeedbackText", "", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            SetFixedRect(feedback.rectTransform, new Vector2(0f, 0f), new Vector2(24f, 22f), new Vector2(712f, 34f));

            var controls = EnsureComponent<DeskDebugControls>(panel);
            ConfigureDeskDebugControls(
                controls,
                deskPanelView,
                sharedActorSlot,
                documentPopupPanel,
                inventoryService,
                taskService,
                roundService,
                runtimeDataService,
                factionService,
                letterService,
                documentService,
                feedback);

            var buttonsRoot = FindOrCreateUiChild(panel.transform, "Buttons");
            SetFixedRect(buttonsRoot.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(24f, -74f), new Vector2(712f, 530f), new Vector2(0f, 1f));
            RemoveLayoutGroups(buttonsRoot);

            CreateTestButton(buttonsRoot.transform, "AddMoneyButton", "+ Money", 0, 0, controls, nameof(DeskDebugControls.AddMoney), controls.AddMoney);
            CreateTestButton(buttonsRoot.transform, "RemoveMoneyButton", "- Money", 1, 0, controls, nameof(DeskDebugControls.RemoveMoney), controls.RemoveMoney);
            CreateTestButton(buttonsRoot.transform, "AddMaterialButton", "+ Material", 2, 0, controls, nameof(DeskDebugControls.AddMaterial), controls.AddMaterial);
            CreateTestButton(buttonsRoot.transform, "RemoveMaterialButton", "- Material", 3, 0, controls, nameof(DeskDebugControls.RemoveMaterial), controls.RemoveMaterial);
            CreateTestButton(buttonsRoot.transform, "AddFoodButton", "+ Food", 0, 1, controls, nameof(DeskDebugControls.AddFood), controls.AddFood);
            CreateTestButton(buttonsRoot.transform, "RemoveFoodButton", "- Food", 1, 1, controls, nameof(DeskDebugControls.RemoveFood), controls.RemoveFood);
            CreateTestButton(buttonsRoot.transform, "ActivateTaskButton", "Activate Task", 2, 1, controls, nameof(DeskDebugControls.ActivateDemoTask), controls.ActivateDemoTask);
            CreateTestButton(buttonsRoot.transform, "AddTaskScoreButton", "+ Task Score", 3, 1, controls, nameof(DeskDebugControls.AddDemoTaskScore), controls.AddDemoTaskScore);
            CreateTestButton(buttonsRoot.transform, "NextRoundButton", "Next Round", 0, 2, controls, nameof(DeskDebugControls.NextRound), controls.NextRound);
            CreateTestButton(buttonsRoot.transform, "LowerSuspicionButton", "Lower Suspicion", 1, 2, controls, nameof(DeskDebugControls.LowerSuspicion), controls.LowerSuspicion);
            CreateTestButton(buttonsRoot.transform, "RaiseSuspicionButton", "Raise Suspicion", 2, 2, controls, nameof(DeskDebugControls.RaiseSuspicion), controls.RaiseSuspicion);
            CreateTestButton(buttonsRoot.transform, "ReceiveLetterAButton", "Receive Letter A", 3, 2, controls, nameof(DeskDebugControls.ReceiveLetterA), controls.ReceiveLetterA);
            CreateTestButton(buttonsRoot.transform, "ReceiveLetterBButton", "Receive Letter B", 0, 3, controls, nameof(DeskDebugControls.ReceiveLetterB), controls.ReceiveLetterB);
            CreateTestButton(buttonsRoot.transform, "ReceiveLetterCButton", "Receive Letter C", 1, 3, controls, nameof(DeskDebugControls.ReceiveLetterC), controls.ReceiveLetterC);
            CreateTestButton(buttonsRoot.transform, "ShowActorButton", "Show Actor", 2, 3, controls, nameof(DeskDebugControls.ShowTestActor), controls.ShowTestActor);
            CreateTestButton(buttonsRoot.transform, "HideActorButton", "Hide Actor", 3, 3, controls, nameof(DeskDebugControls.HideActor), controls.HideActor);
            CreateTestButton(buttonsRoot.transform, "ShowDocumentButton", "Show Document", 0, 4, controls, nameof(DeskDebugControls.ShowDocumentPreview), controls.ShowDocumentPreview);
            CreateTestButton(buttonsRoot.transform, "HideDocumentButton", "Hide Document", 1, 4, controls, nameof(DeskDebugControls.HideDocumentPreview), controls.HideDocumentPreview);
            CreateTestButton(buttonsRoot.transform, "RefreshDeskButton", "Refresh Desk", 2, 4, controls, nameof(DeskDebugControls.RefreshDesk), controls.RefreshDesk);
            return panel;
        }

        private static void ConfigureLetterRowTemplate(GameObject rowTemplate)
        {
            SetFixedRect(rowTemplate.GetComponent<RectTransform>(), new Vector2(0f, 1f), Vector2.zero, new Vector2(82f, 82f), new Vector2(0f, 1f));
            ConfigurePanelImage(rowTemplate, new Color(0.78f, 0.69f, 0.46f, 1f));

            var button = EnsureComponent<Button>(rowTemplate);
            button.targetGraphic = rowTemplate.GetComponent<Image>();

            DestroyChildIfExists(rowTemplate.transform, "TitleText");
            DestroyChildIfExists(rowTemplate.transform, "SenderText");
            DestroyChildIfExists(rowTemplate.transform, "StatusText");

            var iconText = FindOrCreateText(rowTemplate.transform, "IconText", "信", 30, FontStyles.Bold, TextAlignmentOptions.Center);
            iconText.color = new Color(0.16f, 0.11f, 0.08f, 1f);
            SetFixedRect(iconText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(82f, 82f), new Vector2(0.5f, 0.5f));

            var row = EnsureComponent<LetterRowView>(rowTemplate);
            row.Configure(iconText, button);
            AddPersistentListenerIfMissing(button, row, nameof(LetterRowView.OnClicked), row.OnClicked);
        }

        private static FactionSuspicionRow[] CreateHierarchyFactionRows(Transform contentTransform)
        {
            var factionIds = new[] { "noble", "academy", "church", "civilian" };
            var rows = new FactionSuspicionRow[factionIds.Length];
            for (var index = 0; index < factionIds.Length; index++)
            {
                rows[index] = CreateOrConfigureFactionRow(contentTransform, factionIds[index]);
            }

            return rows;
        }

        private static FactionSuspicionRow CreateOrConfigureFactionRow(Transform parent, string factionId)
        {
            var rowObject = FindOrCreateUiChild(parent, $"{factionId}SuspicionRow");
            SetFixedRect(rowObject.GetComponent<RectTransform>(), new Vector2(0f, 1f), Vector2.zero, new Vector2(360f, 50f), new Vector2(0f, 1f));
            ConfigurePanelImage(rowObject, new Color(0.16f, 0.16f, 0.15f, 0.92f));

            var nameText = FindOrCreateText(rowObject.transform, "NameText", factionId, 15, FontStyles.Bold, TextAlignmentOptions.Left);
            SetFixedRect(nameText.rectTransform, new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(86f, 36f), new Vector2(0f, 0.5f));

            var iconImage = FindOrCreateImage(rowObject.transform, "FactionIcon", GetFactionIconColor(factionId));
            SetFixedRect(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(104f, 0f), new Vector2(24f, 24f), new Vector2(0f, 0.5f));
            iconImage.enabled = true;

            var valueText = FindOrCreateText(rowObject.transform, "ValueText", "", 13, FontStyles.Normal, TextAlignmentOptions.Right);
            SetFixedRect(valueText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(70f, 36f), new Vector2(1f, 0.5f));

            var sliderObject = FindOrCreateUiChild(rowObject.transform, "SuspicionSlider");
            var sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0.5f);
            sliderRect.anchorMax = new Vector2(1f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.offsetMin = new Vector2(138f, -8f);
            sliderRect.offsetMax = new Vector2(-86f, 8f);

            var backgroundImage = FindOrCreateImage(sliderObject.transform, "Background", new Color(0.08f, 0.08f, 0.08f, 1f));
            StretchRect(backgroundImage.rectTransform, Vector2.zero, Vector2.zero);

            var fillArea = FindOrCreateUiChild(sliderObject.transform, "Fill Area");
            StretchRect(fillArea.GetComponent<RectTransform>(), new Vector2(2f, 2f), new Vector2(-2f, -2f));

            var fillImage = FindOrCreateImage(fillArea.transform, "Fill", new Color(0.74f, 0.22f, 0.18f, 1f));
            StretchRect(fillImage.rectTransform, Vector2.zero, Vector2.zero);

            var slider = EnsureComponent<Slider>(sliderObject);
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            slider.fillRect = fillImage.rectTransform;
            slider.targetGraphic = backgroundImage;

            var row = EnsureComponent<FactionSuspicionRow>(rowObject);
            row.SetFactionId(factionId);
            row.Configure(nameText, valueText, slider, iconImage, rowObject.GetComponent<Image>(), backgroundImage, fillImage);
            return row;
        }

        private static Color GetFactionIconColor(string factionId)
        {
            switch (factionId)
            {
                case "noble":
                    return new Color(0.92f, 0.72f, 0.28f, 1f);
                case "academy":
                    return new Color(0.38f, 0.62f, 0.95f, 1f);
                case "church":
                    return new Color(0.78f, 0.78f, 0.86f, 1f);
                case "civilian":
                    return new Color(0.52f, 0.82f, 0.48f, 1f);
                default:
                    return Color.white;
            }
        }

        private static void ConfigureLetterIconGrid(GameObject target)
        {
            foreach (var layout in target.GetComponents<LayoutGroup>())
            {
                Object.DestroyImmediate(layout);
            }

            var grid = EnsureComponent<GridLayoutGroup>(target);
            grid.cellSize = new Vector2(82f, 82f);
            grid.spacing = new Vector2(14f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperLeft;
        }

        private static void CreateTestButton(
            Transform parent,
            string name,
            string label,
            int column,
            int row,
            DeskDebugControls target,
            string methodName,
            UnityAction action)
        {
            const float buttonWidth = 166f;
            const float buttonHeight = 40f;
            const float spacingX = 16f;
            const float spacingY = 14f;

            var button = FindOrCreateButton(parent, name, label);
            var x = column * (buttonWidth + spacingX);
            var y = -row * (buttonHeight + spacingY);
            SetFixedRect(button.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(x, y), new Vector2(buttonWidth, buttonHeight), new Vector2(0f, 1f));
            AddPersistentListenerIfMissing(button, target, methodName, action);
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

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
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

        private static Image FindOrCreateImage(Transform parent, string name, Color color)
        {
            var imageObject = FindOrCreateUiChild(parent, name);
            var image = EnsureComponent<Image>(imageObject);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Button FindOrCreateButton(Transform parent, string name, string label)
        {
            var buttonObject = FindOrCreateUiChild(parent, name);
            var image = EnsureComponent<Image>(buttonObject);
            image.color = new Color(0.24f, 0.24f, 0.21f, 1f);

            var button = EnsureComponent<Button>(buttonObject);
            button.targetGraphic = image;

            var labelText = FindOrCreateText(buttonObject.transform, "Label", label, 14, FontStyles.Bold, TextAlignmentOptions.Center);
            SetFixedRect(labelText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 28f), new Vector2(0.5f, 0.5f));
            return button;
        }

        private static void ConfigureFullScreenRect(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
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

        private static void StretchRect(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.sizeDelta = new Vector2(
                Mathf.Max(0f, rectTransform.sizeDelta.x),
                Mathf.Max(0f, rectTransform.sizeDelta.y));
        }

        private static void ConfigurePanelImage(GameObject target, Color color)
        {
            var image = EnsureComponent<Image>(target);
            image.color = color;
        }

        private static void ConfigureVerticalList(GameObject target, float spacing)
        {
            var layout = EnsureComponent<VerticalLayoutGroup>(target);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void RemoveLayoutGroups(GameObject target)
        {
            foreach (var layout in target.GetComponents<LayoutGroup>())
            {
                Object.DestroyImmediate(layout);
            }
        }

        private static void ClearFormalDebugArtifacts(GameObject panel)
        {
            var debugNames = new[]
            {
                "InventoryDebugButtons",
                "TaskDebugButtons",
                "SuspicionDebugButtons",
                "LetterDebugButtons",
                "StoryDebugButtons",
                "RoundDebugButtons"
            };

            foreach (var debugName in debugNames)
            {
                var child = panel.transform.Find(debugName);
                if (child != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            DestroyIfExists<InventoryDebugControls>(panel);
            DestroyIfExists<TaskDebugControls>(panel);
            DestroyIfExists<FactionDebugControls>(panel);
            DestroyIfExists<LetterDebugControls>(panel);
            DestroyIfExists<StoryDebugControls>(panel);
            DestroyIfExists<RoundDebugControls>(panel);
        }

        private static void DestroyIfExists<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            if (component != null)
            {
                Object.DestroyImmediate(component);
            }
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

        private static InventoryItemCard LoadOrCreateInventoryCardPrefab()
        {
            var cardPrefab = AssetDatabase.LoadAssetAtPath<InventoryItemCard>(InventoryCardPrefabPath);
            if (cardPrefab != null)
            {
                return cardPrefab;
            }

            InventoryPrefabBuilder.CreateInventoryItemCardPrefab();
            AssetDatabase.Refresh();
            cardPrefab = AssetDatabase.LoadAssetAtPath<InventoryItemCard>(InventoryCardPrefabPath);
            if (cardPrefab == null)
            {
                throw new FileNotFoundException($"Failed to create inventory card prefab at {InventoryCardPrefabPath}.");
            }

            return cardPrefab;
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

        private static void ConfigureFactionService(FactionService factionService, ConfigManager configManager, RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(factionService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("highSuspicionReduceValue").intValue = 30;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureLetterService(LetterService letterService, ConfigManager configManager, RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(letterService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
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

        private static void ConfigureInventoryPanelView(
            InventoryPanelView panelView,
            InventoryService inventoryService,
            RuntimeDataService runtimeDataService,
            GameObject contentRoot,
            InventoryItemCard cardPrefab)
        {
            var serializedObject = new SerializedObject(panelView);
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("contentRoot").objectReferenceValue = contentRoot.GetComponent<RectTransform>();
            serializedObject.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
            serializedObject.FindProperty("showZeroCountItems").boolValue = false;
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
            serializedObject.FindProperty("rowPrefab").objectReferenceValue = null;
            serializedObject.FindProperty("emptyText").objectReferenceValue = emptyText;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSuspicionPanelView(
            SuspicionPanelView panelView,
            FactionService factionService,
            RuntimeDataService runtimeDataService,
            GameObject contentRoot,
            FactionSuspicionRow[] factionRows,
            RectTransform pointerIcon,
            TMP_Text feedbackText)
        {
            var serializedObject = new SerializedObject(panelView);
            serializedObject.FindProperty("factionService").objectReferenceValue = factionService;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("contentRoot").objectReferenceValue = contentRoot.GetComponent<RectTransform>();
            ConfigureFactionRows(serializedObject.FindProperty("factionRows"), factionRows);
            serializedObject.FindProperty("feedbackText").objectReferenceValue = feedbackText;
            serializedObject.FindProperty("pointerIcon").objectReferenceValue = pointerIcon;
            ConfigureFactionIconBindings(serializedObject.FindProperty("factionIcons"));
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureFactionRows(SerializedProperty rowsProperty, FactionSuspicionRow[] factionRows)
        {
            rowsProperty.arraySize = factionRows.Length;
            for (var index = 0; index < factionRows.Length; index++)
            {
                rowsProperty.GetArrayElementAtIndex(index).objectReferenceValue = factionRows[index];
            }
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

        private static void ConfigureDeskPanelView(
            DeskPanelView deskPanelView,
            GameObject taskPanel,
            GameObject suspicionPanel,
            GameObject letterArea,
            GameObject inventoryPanel,
            GameObject sharedActorSlot,
            GameObject documentPopupPanel)
        {
            var serializedObject = new SerializedObject(deskPanelView);
            serializedObject.FindProperty("taskPanel").objectReferenceValue = taskPanel.GetComponent<TaskPanelView>();
            serializedObject.FindProperty("suspicionPanel").objectReferenceValue = suspicionPanel.GetComponent<SuspicionPanelView>();
            serializedObject.FindProperty("letterArea").objectReferenceValue = letterArea.GetComponent<LetterAreaView>();
            serializedObject.FindProperty("inventoryPanel").objectReferenceValue = inventoryPanel.GetComponent<InventoryPanelView>();
            serializedObject.FindProperty("sharedActorSlot").objectReferenceValue = sharedActorSlot.GetComponent<SharedActorSlotView>();
            serializedObject.FindProperty("documentPopupPanel").objectReferenceValue = documentPopupPanel.GetComponent<DocumentPopupPanelView>();
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDeskDebugControls(
            DeskDebugControls controls,
            DeskPanelView deskPanelView,
            SharedActorSlotView sharedActorSlot,
            DocumentPopupPanelView documentPopupPanel,
            InventoryService inventoryService,
            TaskService taskService,
            RoundService roundService,
            RuntimeDataService runtimeDataService,
            FactionService factionService,
            LetterService letterService,
            DocumentService documentService,
            TMP_Text feedbackText)
        {
            var serializedObject = new SerializedObject(controls);
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.FindProperty("roundService").objectReferenceValue = roundService;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("factionService").objectReferenceValue = factionService;
            serializedObject.FindProperty("letterService").objectReferenceValue = letterService;
            serializedObject.FindProperty("documentService").objectReferenceValue = documentService;
            serializedObject.FindProperty("deskPanelView").objectReferenceValue = deskPanelView;
            serializedObject.FindProperty("sharedActorSlot").objectReferenceValue = sharedActorSlot;
            serializedObject.FindProperty("documentPopupPanel").objectReferenceValue = documentPopupPanel;
            serializedObject.FindProperty("feedbackText").objectReferenceValue = feedbackText;
            serializedObject.FindProperty("demoTaskId").stringValue = "task_demo_relief_01";
            serializedObject.FindProperty("lowTestFactionId").stringValue = "civilian";
            serializedObject.FindProperty("highTestFactionId").stringValue = "noble";
            serializedObject.FindProperty("demoLetterIdA").stringValue = "letter_relief_start";
            serializedObject.FindProperty("demoLetterIdB").stringValue = "letter_relief_prepare_end";
            serializedObject.FindProperty("demoLetterIdC").stringValue = "letter_relief_deliver_start";
            serializedObject.FindProperty("demoDocumentId").stringValue = "document_relief_prepare";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureGameEntry(GameEntry gameEntry, GameObject deskPanel)
        {
            var serializedObject = new SerializedObject(gameEntry);
            serializedObject.FindProperty("deskRoot").objectReferenceValue = deskPanel;
            serializedObject.FindProperty("showDeskOnStart").boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
