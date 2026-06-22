using System.Linq;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class CityHudPanelView : MonoBehaviour
    {
        [Header("城区 HUD：下一回合按钮")]
        [Tooltip("下一回合按钮；点击后走 DeskLoopController.EndRoundFromCityView，包含黑场过渡和摄像机重置。为空时按名称自动查找。")]
        [SerializeField] private Button nextRoundButton;

        [Tooltip("桌面回合流程控制器；用于执行城区 HUD 专用的下一回合流程。为空时运行时自动查找。")]
        [SerializeField] private DeskLoopController deskLoopController;

        [Tooltip("备用回合调试控制器；找不到 DeskLoopController 时使用，仍会尝试黑场过渡和摄像机重置。为空时运行时自动查找或补充。")]
        [SerializeField] private RoundDebugControls fallbackRoundControls;

        [Header("只读快照：按钮绑定状态")]
        [Tooltip("运行时只读；显示下一回合按钮是否已经绑定到正确流程，方便在 Inspector 中排查。")]
        [SerializeField] private string inspectorNextRoundBindingSnapshot;

        private BaseSceneUIContext context;

        private void Awake()
        {
            BindNextRoundButton();
        }

        private void OnEnable()
        {
            BindNextRoundButton();
        }

        public void ApplyContext(BaseSceneUIContext newContext)
        {
            context = newContext;
            BindNextRoundButton();
        }

        public void BindNextRoundButton()
        {
            ResolveReferences();
            if (nextRoundButton == null)
            {
                inspectorNextRoundBindingSnapshot = "未找到下一回合按钮。";
                return;
            }

            nextRoundButton.onClick.RemoveListener(HandleNextRoundClicked);
            nextRoundButton.onClick.AddListener(HandleNextRoundClicked);
            inspectorNextRoundBindingSnapshot = deskLoopController != null
                ? $"已绑定到 {deskLoopController.gameObject.name}.EndRoundFromCityView。"
                : fallbackRoundControls != null
                    ? $"已绑定到 {fallbackRoundControls.gameObject.name}.NextRound 备用流程。"
                    : "已绑定点击入口，但缺少回合流程控制器。";
        }

        private void HandleNextRoundClicked()
        {
            ResolveReferences();
            if (deskLoopController != null)
            {
                deskLoopController.EndRoundFromCityView();
                return;
            }

            fallbackRoundControls?.NextRound();
        }

        private void ResolveReferences()
        {
            if (context == null)
            {
                var panelRoot = GetComponent<BaseSceneUIPanelRoot>() ?? GetComponentInParent<BaseSceneUIPanelRoot>(true);
                context = panelRoot != null ? panelRoot.Context : FindFirstObjectByType<BaseSceneUIContext>(FindObjectsInactive.Include);
            }

            if (nextRoundButton == null)
            {
                nextRoundButton = GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(button => button != null && button.gameObject.name == "下一回合按钮") ??
                    GetComponentsInChildren<Button>(true)
                        .FirstOrDefault(button => button != null && button.gameObject.name.Contains("下一回合"));
            }

            if (deskLoopController == null)
            {
                deskLoopController = FindFirstObjectByType<DeskLoopController>(FindObjectsInactive.Include);
            }

            if (fallbackRoundControls == null)
            {
                fallbackRoundControls = GetComponent<RoundDebugControls>() ?? gameObject.AddComponent<RoundDebugControls>();
            }
        }
    }
}
