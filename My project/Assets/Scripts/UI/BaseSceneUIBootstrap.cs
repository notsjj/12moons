using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class BaseSceneUIBootstrap : MonoBehaviour
    {
        private static readonly UIType StartPanel = new UIType("Prefabs/UI/开始面板", UILayer.Overlay);
        private static readonly UIType SharedHudPanel = new UIType("Prefabs/UI/共享HUD面板", UILayer.Overlay);
        private static readonly UIType DeskPanel = new UIType("Prefabs/UI/桌面面板", UILayer.Panel);
        private static readonly UIType StoryPanel = new UIType("Prefabs/UI/剧情面板", UILayer.Overlay);
        private static readonly UIType CityHudPanel = new UIType("Prefabs/UI/城区HUD面板", UILayer.Panel);
        private static readonly UIType DocumentPopupPanel = new UIType("Prefabs/UI/公文弹窗面板", UILayer.Popup);
        private static readonly UIType NewspaperPanel = new UIType("Prefabs/UI/报纸面板", UILayer.Popup);
        private static readonly UIType LetterReaderPanel = new UIType("Prefabs/UI/信件阅读面板", UILayer.Popup);
        private static readonly UIType LoadingPanel = new UIType("Prefabs/UI/加载过场面板", UILayer.Overlay);

        [Header("UI 上下文")]
        [Tooltip("BaseScene 中的 UI 上下文；为空时自动从当前物体或场景中查找。")]
        [SerializeField] private BaseSceneUIContext uiContext;

        [Header("UI 管理器")]
        [Tooltip("UIFramework 管理器；为空时自动使用 UIManager.Instance 或当前物体上的 UIManager。")]
        [SerializeField] private UIManager uiManager;

        [Header("启动调试开关")]
        [Tooltip("进入游戏时是否显示各 UI Prefab 内的调试按钮。正式流程应保持关闭。")]
        [SerializeField] private bool showDebugControlsOnStart;

        [Header("LoadingPanel 运行时调试")]
        [Tooltip("启用后，在 Play 模式按 P 键会显示并重播 LoadingPanel 过场；此调试播放不会切换到城区。")]
        [SerializeField] private bool enableLoadingPanelDebugHotkey = true;

        public bool IsLoadingPanelDebugHotkeyEnabled => enableLoadingPanelDebugHotkey;

        private DocumentPopupPanelView observedDocumentPopup;

        private void Start()
        {
            ResolveReferences();

            if (uiContext == null || uiManager == null)
            {
                Debug.LogError("BaseScene UI 启动失败：缺少 UI 上下文或 UIManager。");
                return;
            }

            uiContext.ResolveMissingReferences();
            uiManager.EnsureLayerRoots();

            ShowStartPanel();
        }

        private void Update()
        {
            if (!enableLoadingPanelDebugHotkey || !Input.GetKeyDown(KeyCode.P))
            {
                return;
            }

            var loadingPanel = ShowLoadingPanel();
            loadingPanel?.PlayDebugTransition();
        }

        public void ShowDesk()
        {
            ShowAndPrepare(DeskPanel);
            ShowSharedHud(true);
            uiManager?.HideUI(CityHudPanel);
        }

        public void ShowCity()
        {
            ShowSharedHud(true);
            ShowAndPrepare(CityHudPanel);
            uiManager?.HideUI(DeskPanel);
        }

        public void ShowStory()
        {
            ShowAndPrepare(StoryPanel);
            ShowSharedHud(true);
        }

        public void ShowStartPanel()
        {
            var startObject = ShowAndPrepare(StartPanel);
            if (startObject == null)
            {
                ShowStory();
                return;
            }

            uiManager?.HideUI(DeskPanel);
            uiManager?.HideUI(StoryPanel);
            uiManager?.HideUI(SharedHudPanel);
            uiManager?.HideUI(CityHudPanel);

            var startPanel = startObject.GetComponent<StartPanelView>();
            if (startPanel == null)
            {
                startPanel = startObject.AddComponent<StartPanelView>();
            }

            startPanel.Initialize(EnterStoryFromStartPanel);
            startObject.transform.SetAsLastSibling();
        }

        private void EnterStoryFromStartPanel()
        {
            uiManager?.HideUI(StartPanel);
            var deskObject = ShowAndPrepare(DeskPanel);
            ShowSharedHud(true);
            uiManager?.HideUI(CityHudPanel);
            var deskLoopController = deskObject != null ? deskObject.GetComponent<DeskLoopController>() : null;
            deskLoopController?.BeginCurrentRoundFromEntry();
            ShowStory();
        }

        public void HideStory()
        {
            uiManager?.HideUI(StoryPanel);
        }

        public void ShowDocumentPopup()
        {
            var popupObject = ShowAndPrepare(DocumentPopupPanel);
            var popup = popupObject != null ? popupObject.GetComponent<DocumentPopupPanelView>() : null;
            RegisterDocumentPopupStateListener(popup);
            HandleDocumentPopupStateChanged();
        }

        public void ShowNewspaper()
        {
            ShowAndPrepare(NewspaperPanel);
        }

        public void ShowLetterReader()
        {
            ShowAndPrepare(LetterReaderPanel);
        }

        public LoadingPanelTransitionView ShowLoadingPanel()
        {
            var loadingObject = ShowAndPrepare(LoadingPanel);
            if (loadingObject == null)
            {
                return null;
            }

            BringLoadingPanelToFront(loadingObject);

            var loadingView = loadingObject.GetComponent<LoadingPanelTransitionView>();
            if (loadingView == null)
            {
                loadingView = loadingObject.AddComponent<LoadingPanelTransitionView>();
            }

            return loadingView;
        }

        public void HideLoadingPanel()
        {
            uiManager?.HideUI(LoadingPanel);
        }

        public void HidePopup(UIType type)
        {
            if (type == null)
            {
                Debug.LogError("要隐藏的 Popup UIType 为空。");
                return;
            }

            uiManager?.HideUI(type);
        }

        private void OnDestroy()
        {
            UnregisterDocumentPopupStateListener(observedDocumentPopup);
        }

        private void ResolveReferences()
        {
            if (uiContext == null)
            {
                uiContext = GetComponent<BaseSceneUIContext>();
            }

            if (uiContext == null)
            {
                uiContext = FindFirstObjectByType<BaseSceneUIContext>(FindObjectsInactive.Include);
            }

            if (uiManager == null)
            {
                uiManager = UIManager.Instance;
            }

            if (uiManager == null)
            {
                uiManager = GetComponent<UIManager>();
            }
        }

        private GameObject ShowAndPrepare(UIType type)
        {
            if (uiManager == null)
            {
                Debug.LogError($"无法显示 UI：{type.Name}，缺少 UIManager。");
                return null;
            }

            var handle = uiManager.ShowUI(type);
            if (handle?.GameObject == null)
            {
                return null;
            }

            var root = handle.GameObject.GetComponent<BaseSceneUIPanelRoot>();
            if (root != null)
            {
                root.ShowDebugControls = showDebugControlsOnStart;
                root.ApplyContext(uiContext);
            }

            if (type == DeskPanel)
            {
                EnsureDeskLoopController(handle.GameObject);
            }

            return handle.GameObject;
        }

        private void ShowSharedHud(bool showTaskPanel)
        {
            var sharedHudObject = ShowAndPrepare(SharedHudPanel);
            if (sharedHudObject == null)
            {
                return;
            }

            SetSharedHudPanelVisibility(sharedHudObject, showTaskPanel, !IsObservedDocumentFlowActive());
            BringSharedHudToFront(sharedHudObject);
            BringActiveLoadingPanelToFront();
        }

        private void BringSharedHudToFront(GameObject sharedHudObject)
        {
            sharedHudObject.transform.SetAsLastSibling();
        }

        private void BringLoadingPanelToFront(GameObject loadingObject)
        {
            if (loadingObject == null)
            {
                return;
            }

            var overlayRoot = uiManager != null ? uiManager.GetLayerRoot(UILayer.Overlay) : null;
            overlayRoot?.SetAsLastSibling();
            loadingObject.transform.SetAsLastSibling();
        }

        private void BringActiveLoadingPanelToFront()
        {
            if (uiManager == null ||
                !uiManager.TryGetUI<LoadingPanelTransitionView>(LoadingPanel, out var loadingPanel) ||
                loadingPanel == null ||
                !loadingPanel.gameObject.activeInHierarchy)
            {
                return;
            }

            BringLoadingPanelToFront(loadingPanel.gameObject);
        }

        private static void SetSharedHudPanelVisibility(GameObject sharedHudObject, bool showTaskPanel, bool showRoundPanel = true)
        {
            if (sharedHudObject == null)
            {
                return;
            }

            var taskPanel = sharedHudObject.GetComponentInChildren<TaskPanelView>(true);
            if (taskPanel != null)
            {
                taskPanel.gameObject.SetActive(showTaskPanel);
                if (showTaskPanel)
                {
                    taskPanel.Refresh();
                }
            }

            var roundPanel = sharedHudObject.GetComponentInChildren<RoundPanelView>(true);
            if (roundPanel != null)
            {
                roundPanel.gameObject.SetActive(showRoundPanel);
                if (showRoundPanel)
                {
                    roundPanel.Refresh();
                }
            }
        }

        private void RegisterDocumentPopupStateListener(DocumentPopupPanelView popup)
        {
            if (popup == observedDocumentPopup)
            {
                return;
            }

            UnregisterDocumentPopupStateListener(observedDocumentPopup);
            observedDocumentPopup = popup;

            if (popup != null)
            {
                popup.DocumentFlowStateChanged -= HandleDocumentPopupStateChanged;
                popup.DocumentFlowStateChanged += HandleDocumentPopupStateChanged;
            }
        }

        private void UnregisterDocumentPopupStateListener(DocumentPopupPanelView popup)
        {
            if (popup != null)
            {
                popup.DocumentFlowStateChanged -= HandleDocumentPopupStateChanged;
            }
        }

        private void HandleDocumentPopupStateChanged()
        {
            var isDocumentFlowActive = IsObservedDocumentFlowActive();
            // 公文打开时隐藏共享 HUD 回合面板；公文关闭后恢复，任务面板保持原有显示规则。
            SetSharedHudRoundPanelVisibility(!isDocumentFlowActive);
        }

        private bool IsObservedDocumentFlowActive()
        {
            return observedDocumentPopup != null && observedDocumentPopup.IsDocumentFlowActive;
        }

        private void SetSharedHudRoundPanelVisibility(bool showRoundPanel)
        {
            if (!TryGetSharedHudObject(out var sharedHudObject))
            {
                return;
            }

            var roundPanel = sharedHudObject.GetComponentInChildren<RoundPanelView>(true);
            if (roundPanel == null)
            {
                return;
            }

            roundPanel.gameObject.SetActive(showRoundPanel);
            if (showRoundPanel)
            {
                roundPanel.Refresh();
            }
        }

        private bool TryGetSharedHudObject(out GameObject sharedHudObject)
        {
            sharedHudObject = null;
            if (uiManager != null &&
                uiManager.TryGetUI<BaseSceneUIPanelRoot>(SharedHudPanel, out var root) &&
                root != null)
            {
                sharedHudObject = root.gameObject;
                return true;
            }

            return false;
        }

        private void EnsureDeskLoopController(GameObject deskPanelObject)
        {
            if (deskPanelObject == null || deskPanelObject.GetComponent<DeskLoopController>() != null)
            {
                return;
            }

            deskPanelObject.AddComponent<DeskLoopController>();
            Debug.LogWarning("桌面面板缺少 DeskLoopController，已在运行时自动补上；否则剧情面板会显示但不会启动回合剧情。", deskPanelObject);
        }
    }
}
