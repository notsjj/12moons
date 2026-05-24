using TMPro;
using TwelveMoons.Core;
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
    public static class DeskLoopUiOnlyBuilder
    {
        [MenuItem("Twelve Moons/Setup/Create Desk Loop UI Only")]
        public static void CreateDeskLoopUiOnly()
        {
            var gameEntry = Object.FindFirstObjectByType<GameEntry>(FindObjectsInactive.Include);
            if (gameEntry == null)
            {
                Fail("找不到 GameEntry。本工具只做桌面循环 UI 局部更新，不创建核心场景对象。");
                return;
            }

            var deskPanel = FindExistingChild(gameEntry.transform, "DeskPanel") ??
                Object.FindFirstObjectByType<DeskPanelView>(FindObjectsInactive.Include)?.transform;
            if (deskPanel == null)
            {
                Fail("找不到 DeskPanel。本工具不会重建桌面总布局，请先保留现有 DeskPanel。");
                return;
            }

            var runtimeDataService = FindRequired<RuntimeDataService>("RuntimeDataService");
            var roundService = FindRequired<RoundService>("RoundService");
            var taskService = FindRequired<TaskService>("TaskService");
            var storyService = FindRequired<StoryService>("StoryService");
            var documentService = FindRequired<DocumentService>("DocumentService");
            var gameEntryService = FindRequired<GameEntry>("GameEntry");
            var documentPopupPanel = Object.FindFirstObjectByType<DocumentPopupPanelView>(FindObjectsInactive.Include);
            if (runtimeDataService == null ||
                roundService == null ||
                taskService == null ||
                storyService == null ||
                documentService == null ||
                gameEntryService == null ||
                documentPopupPanel == null)
            {
                Fail("缺少桌面循环依赖服务或 DocumentPopupPanelView。本工具只绑定已有系统，不重建其它 UI。");
                return;
            }

            var newspaperPanel = BuildNewspaperPanel(deskPanel, runtimeDataService);
            BuildDeskLoopControls(
                deskPanel,
                runtimeDataService,
                roundService,
                taskService,
                storyService,
                documentService,
                gameEntryService,
                documentPopupPanel,
                newspaperPanel.GetComponent<NewspaperPanelView>());

            newspaperPanel.SetActive(false);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = deskPanel.gameObject;
            Debug.Log("Desk loop UI only setup completed. Only DeskLoopControls and NewspaperPanel were created or updated.");
        }

        private static GameObject BuildNewspaperPanel(
            Transform parent,
            RuntimeDataService runtimeDataService)
        {
            var panel = FindOrCreateUiChild(parent, "NewspaperPanel");
            SetFixedRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 560f), new Vector2(0.5f, 0.5f));
            ConfigurePanelImage(panel, new Color(0.78f, 0.72f, 0.56f, 0.98f));

            var titleText = FindOrCreateText(panel.transform, "TitleText", "报纸", 28, FontStyles.Bold, TextAlignmentOptions.Center);
            titleText.color = new Color(0.14f, 0.09f, 0.04f, 1f);
            SetFixedRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(560f, 44f), new Vector2(0.5f, 1f));

            var bodyText = FindOrCreateText(panel.transform, "BodyText", "", 18, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            bodyText.color = new Color(0.14f, 0.09f, 0.04f, 1f);
            bodyText.overflowMode = TextOverflowModes.Overflow;
            SetFixedRect(bodyText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(540f, 360f), new Vector2(0.5f, 1f));

            var emptyText = FindOrCreateText(panel.transform, "EmptyText", "", 16, FontStyles.Normal, TextAlignmentOptions.Center);
            emptyText.color = new Color(0.14f, 0.09f, 0.04f, 1f);
            SetFixedRect(emptyText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 60f), new Vector2(0.5f, 0.5f));

            var closeButton = FindOrCreateButton(panel.transform, "CloseButton", "关闭");
            SetFixedRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(140f, 42f), new Vector2(0.5f, 0f));

            var panelView = EnsureComponent<NewspaperPanelView>(panel);
            var serializedObject = new SerializedObject(panelView);
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("titleText").objectReferenceValue = titleText;
            serializedObject.FindProperty("bodyText").objectReferenceValue = bodyText;
            serializedObject.FindProperty("emptyText").objectReferenceValue = emptyText;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AddPersistentListenerIfMissing(closeButton, panelView, nameof(NewspaperPanelView.Hide), panelView.Hide);
            return panel;
        }

        private static GameObject BuildDeskLoopControls(
            Transform parent,
            RuntimeDataService runtimeDataService,
            RoundService roundService,
            TaskService taskService,
            StoryService storyService,
            DocumentService documentService,
            GameEntry gameEntryService,
            DocumentPopupPanelView documentPopupPanel,
            NewspaperPanelView newspaperPanel)
        {
            var panel = FindOrCreateUiChild(parent, "DeskLoopControls");
            SetFixedRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(884f, 92f), new Vector2(0.5f, 0f));
            ConfigurePanelImage(panel, new Color(0.1f, 0.095f, 0.082f, 0.92f));

            var storyButton = FindOrCreateButton(panel.transform, "StoryButton", "播放剧情");
            SetFixedRect(storyButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(18f, -16f), new Vector2(150f, 38f), new Vector2(0f, 1f));
            var documentButton = FindOrCreateButton(panel.transform, "DocumentButton", "处理公文");
            SetFixedRect(documentButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(182f, -16f), new Vector2(150f, 38f), new Vector2(0f, 1f));
            var endRoundButton = FindOrCreateButton(panel.transform, "EndRoundButton", "结束回合");
            SetFixedRect(endRoundButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(346f, -16f), new Vector2(150f, 38f), new Vector2(0f, 1f));
            var newspaperButton = FindOrCreateButton(panel.transform, "NewspaperButton", "报纸");
            SetFixedRect(newspaperButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(510f, -16f), new Vector2(150f, 38f), new Vector2(0f, 1f));
            var cityButton = FindOrCreateButton(panel.transform, "CityButton", "进入城区");
            SetFixedRect(cityButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(674f, -16f), new Vector2(150f, 38f), new Vector2(0f, 1f));

            var statusText = FindOrCreateText(panel.transform, "StatusText", "", 14, FontStyles.Normal, TextAlignmentOptions.Left);
            SetFixedRect(statusText.rectTransform, new Vector2(0f, 0f), new Vector2(18f, 12f), new Vector2(824f, 28f), new Vector2(0f, 0f));

            var controller = EnsureComponent<DeskLoopController>(panel);
            var serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("roundService").objectReferenceValue = roundService;
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.FindProperty("storyService").objectReferenceValue = storyService;
            serializedObject.FindProperty("documentService").objectReferenceValue = documentService;
            serializedObject.FindProperty("gameEntry").objectReferenceValue = gameEntryService;
            serializedObject.FindProperty("documentPopupPanel").objectReferenceValue = documentPopupPanel;
            serializedObject.FindProperty("newspaperPanel").objectReferenceValue = newspaperPanel;
            serializedObject.FindProperty("storyButton").objectReferenceValue = storyButton;
            serializedObject.FindProperty("documentButton").objectReferenceValue = documentButton;
            serializedObject.FindProperty("endRoundButton").objectReferenceValue = endRoundButton;
            serializedObject.FindProperty("newspaperButton").objectReferenceValue = newspaperButton;
            serializedObject.FindProperty("cityButton").objectReferenceValue = cityButton;
            serializedObject.FindProperty("statusText").objectReferenceValue = statusText;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AddPersistentListenerIfMissing(storyButton, controller, nameof(DeskLoopController.StartOrContinueStoryQueue), controller.StartOrContinueStoryQueue);
            AddPersistentListenerIfMissing(documentButton, controller, nameof(DeskLoopController.BeginDocumentFlow), controller.BeginDocumentFlow);
            AddPersistentListenerIfMissing(endRoundButton, controller, nameof(DeskLoopController.EndCurrentRound), controller.EndCurrentRound);
            AddPersistentListenerIfMissing(newspaperButton, controller, nameof(DeskLoopController.ShowPreviousRoundNewspaper), controller.ShowPreviousRoundNewspaper);
            AddPersistentListenerIfMissing(cityButton, controller, nameof(DeskLoopController.EnterCity), controller.EnterCity);
            return panel;
        }

        private static T FindRequired<T>(string label) where T : Object
        {
            var component = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (component == null)
            {
                Debug.LogWarning($"{label} not found. Desk loop UI only setup will not create unrelated systems.");
            }

            return component;
        }

        private static Transform FindExistingChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child.name == childName)
                {
                    return child;
                }

                var nested = FindExistingChild(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static GameObject FindOrCreateUiChild(Transform parent, string childName)
        {
            var existing = parent.Find(childName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var childObject = new GameObject(childName, typeof(RectTransform));
            childObject.transform.SetParent(parent, false);
            return childObject;
        }

        private static TextMeshProUGUI FindOrCreateText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            var textObject = FindOrCreateUiChild(parent, name);
            RemoveLegacyText(textObject);
            var tmp = EnsureComponent<TextMeshProUGUI>(textObject);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
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
            var image = EnsureComponent<Image>(target);
            image.color = color;
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

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void Fail(string message)
        {
            Debug.LogError(message);
            EditorUtility.DisplayDialog("Create Desk Loop UI Only", message, "OK");
        }
    }
}
