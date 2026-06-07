using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class BaseSceneUIPanelRoot : MonoBehaviour
    {
        [Header("调试控件")]
        [Tooltip("是否显示本面板内的调试按钮和测试入口。正式流程默认关闭。")]
        [SerializeField] private bool showDebugControls;

        [Header("调试控件根节点")]
        [Tooltip("需要随调试开关显示或隐藏的节点。")]
        [SerializeField] private GameObject[] debugRoots = new GameObject[0];

        public BaseSceneUIContext Context { get; private set; }

        public bool ShowDebugControls
        {
            get => showDebugControls;
            set
            {
                showDebugControls = value;
                ApplyDebugVisibility();
            }
        }

        private void Awake()
        {
            ApplyDebugVisibility();
        }

        private void OnValidate()
        {
            ApplyDebugVisibility();
        }

        public void ApplyContext(BaseSceneUIContext context)
        {
            Context = context;
            ApplyDebugVisibility();
        }

        public void ApplyDebugVisibility()
        {
            if (debugRoots == null)
            {
                return;
            }

            foreach (var debugRoot in debugRoots)
            {
                if (debugRoot != null)
                {
                    debugRoot.SetActive(showDebugControls);
                }
            }
        }
    }
}
