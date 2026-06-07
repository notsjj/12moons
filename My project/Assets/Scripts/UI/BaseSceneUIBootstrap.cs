using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class BaseSceneUIBootstrap : MonoBehaviour
    {
        private static readonly UIType SharedHudPanel = new UIType("Prefabs/UI/SharedHudPanel", UILayer.Persistent);
        private static readonly UIType DeskPanel = new UIType("Prefabs/UI/DeskPanel", UILayer.Panel);
        private static readonly UIType StoryPanel = new UIType("Prefabs/UI/StoryPanel", UILayer.Overlay);
        private static readonly UIType CityHudPanel = new UIType("Prefabs/UI/CityHudPanel", UILayer.Panel);
        private static readonly UIType DocumentPopupPanel = new UIType("Prefabs/UI/DocumentPopupPanel", UILayer.Popup);
        private static readonly UIType NewspaperPanel = new UIType("Prefabs/UI/NewspaperPanel", UILayer.Popup);
        private static readonly UIType LetterReaderPanel = new UIType("Prefabs/UI/LetterReaderPanel", UILayer.Popup);

        [Header("UI 上下文")]
        [Tooltip("BaseScene 中的 UI 上下文；为空时自动从当前物体或场景中查找。")]
        [SerializeField] private BaseSceneUIContext uiContext;

        [Header("UI 管理器")]
        [Tooltip("UIFramework 管理器；为空时自动使用 UIManager.Instance 或当前物体上的 UIManager。")]
        [SerializeField] private UIManager uiManager;

        [Header("启动调试开关")]
        [Tooltip("进入游戏时是否显示各 UI Prefab 内的调试按钮。正式流程应保持关闭。")]
        [SerializeField] private bool showDebugControlsOnStart;

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

            ShowAndPrepare(SharedHudPanel);
            ShowAndPrepare(DeskPanel);
        }

        public void ShowDesk()
        {
            ShowAndPrepare(DeskPanel);
            uiManager?.HideUI(CityHudPanel);
        }

        public void ShowCity()
        {
            ShowAndPrepare(CityHudPanel);
            uiManager?.HideUI(DeskPanel);
        }

        public void ShowStory()
        {
            ShowAndPrepare(StoryPanel);
        }

        public void HideStory()
        {
            uiManager?.HideUI(StoryPanel);
        }

        public void ShowDocumentPopup()
        {
            ShowAndPrepare(DocumentPopupPanel);
        }

        public void ShowNewspaper()
        {
            ShowAndPrepare(NewspaperPanel);
        }

        public void ShowLetterReader()
        {
            ShowAndPrepare(LetterReaderPanel);
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

            return handle.GameObject;
        }
    }
}
