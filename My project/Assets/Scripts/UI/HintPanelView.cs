using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class HintPanelView : MonoBehaviour
    {
        private const string DefaultPrefabPath = "Prefabs/UI/提示面板";
        private const string NoSideEventMessage = "当前建筑没有事件";

        [Header("提示面板：建筑无事件反馈")]
        [Tooltip("提示正文文本；运行时会写入当前建筑没有事件等反馈，必须使用 TextMeshPro。")]
        [SerializeField] private TMP_Text messageText;

        [Tooltip("返回按钮；运行时会自动绑定 Hide 方法，用于关闭提示面板。")]
        [SerializeField] private Button returnButton;

        [Tooltip("提示面板 Prefab 的 Resources 路径；默认读取 Assets/Resources/Prefabs/UI/提示面板.prefab。")]
        [SerializeField] private string prefabResourcePath = DefaultPrefabPath;

        [Header("只读快照：提示面板状态")]
        [Tooltip("运行时只读；显示最近一次提示内容，方便在 Inspector 中确认点击反馈。")]
        [SerializeField] private string inspectorLastMessage;

        private static HintPanelView activeInstance;

        private void Awake()
        {
            ResolveReferences();
            BindReturnButton();
        }

        private void OnEnable()
        {
            ResolveReferences();
            BindReturnButton();
        }

        private void OnDisable()
        {
            if (returnButton != null)
            {
                returnButton.onClick.RemoveListener(Hide);
            }
        }

        public static void ShowNoSideEventHint()
        {
            Show(NoSideEventMessage);
        }

        public static void Show(string message)
        {
            var panel = ResolveOrCreateInstance();
            if (panel == null)
            {
                return;
            }

            panel.ShowMessage(message);
        }

        public void ShowMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(prefabResourcePath))
            {
                prefabResourcePath = DefaultPrefabPath;
            }

            ResolveReferences();
            BindReturnButton();
            inspectorLastMessage = string.IsNullOrWhiteSpace(message) ? NoSideEventMessage : message;
            if (messageText != null)
            {
                messageText.text = inspectorLastMessage;
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private static HintPanelView ResolveOrCreateInstance()
        {
            if (activeInstance != null)
            {
                return activeInstance;
            }

            activeInstance = FindObjectsByType<HintPanelView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(panel => panel != null);
            if (activeInstance != null)
            {
                return activeInstance;
            }

            var parent = ResolveCanvasTransform();
            var prefab = Resources.Load<GameObject>(DefaultPrefabPath);
            var instance = prefab != null
                ? Instantiate(prefab, parent, false)
                : CreateFallbackPanel(parent);
            instance.name = "提示面板";
            activeInstance = instance.GetComponent<HintPanelView>() ?? instance.AddComponent<HintPanelView>();
            activeInstance.prefabResourcePath = DefaultPrefabPath;
            activeInstance.ResolveReferences();
            activeInstance.BindReturnButton();
            return activeInstance;
        }

        private static Transform ResolveCanvasTransform()
        {
            var canvas = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(candidate => candidate != null)
                .OrderByDescending(candidate => candidate.gameObject.activeInHierarchy)
                .ThenByDescending(candidate => candidate.sortingOrder)
                .FirstOrDefault();
            if (canvas != null)
            {
                return canvas.transform;
            }

            var canvasObject = new GameObject("运行时提示面板Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvasObject.transform;
        }

        private static GameObject CreateFallbackPanel(Transform parent)
        {
            var panel = new GameObject("提示面板", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(800f, 600f);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f);

            var messageObject = new GameObject("提示", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            messageObject.transform.SetParent(panel.transform, false);
            var messageRect = messageObject.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0.5f, 1f);
            messageRect.anchorMax = new Vector2(0.5f, 1f);
            messageRect.anchoredPosition = new Vector2(0f, -260f);
            messageRect.sizeDelta = new Vector2(360f, 60f);
            var text = messageObject.GetComponent<TextMeshProUGUI>();
            text.text = NoSideEventMessage;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 36f;
            text.color = Color.white;

            var buttonObject = new GameObject("返回按钮", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panel.transform, false);
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0f, -140f);
            buttonRect.sizeDelta = new Vector2(300f, 64f);
            buttonObject.GetComponent<Image>().color = Color.white;

            var labelObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "返回";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 24f;
            label.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            return panel;
        }

        private void ResolveReferences()
        {
            if (returnButton == null)
            {
                returnButton = GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(button => button != null && button.name.Contains("返回")) ??
                    GetComponentInChildren<Button>(true);
            }

            if (messageText == null)
            {
                messageText = GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(text => text != null && text.name.Contains("提示")) ??
                    GetComponentsInChildren<TMP_Text>(true)
                        .FirstOrDefault(text => text != null && (returnButton == null || !text.transform.IsChildOf(returnButton.transform)));
            }
        }

        private void BindReturnButton()
        {
            if (returnButton == null)
            {
                return;
            }

            returnButton.onClick.RemoveListener(Hide);
            returnButton.onClick.AddListener(Hide);
        }
    }
}
