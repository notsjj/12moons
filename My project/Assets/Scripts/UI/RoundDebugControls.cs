using System.Collections;
using TwelveMoons.City;
using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class RoundDebugControls : MonoBehaviour
    {
        [Header("\u56de\u5408\u63a8\u8fdb\uff1a\u5f53\u524d\u56de\u5408\u670d\u52a1")]
        [Tooltip("\u7528\u4e8e\u63a8\u8fdb\u56de\u5408\uff1b\u7559\u7a7a\u65f6\u81ea\u52a8\u67e5\u627e\u3002")]
        [SerializeField] private RoundService roundService;

        [Header("\u56de\u5408\u63a8\u8fdb\uff1a\u9ed1\u573a\u8fc7\u6e21\u548c\u684c\u9762\u5207\u6362")]
        [Tooltip("\u7528\u4e8e\u663e\u793a\u9ed1\u573a\u9762\u677f\u548c\u5207\u6362\u56de\u684c\u9762\uff1b\u7559\u7a7a\u65f6\u81ea\u52a8\u67e5\u627e\u3002")]
        [SerializeField] private BaseSceneUIBootstrap uiBootstrap;

        [Header("\u56de\u5408\u63a8\u8fdb\uff1a\u5267\u60c5\u670d\u52a1")]
        [Tooltip("\u7528\u4e8e\u89e6\u53d1\u65b0\u56de\u5408\u5267\u60c5\uff1b\u7559\u7a7a\u65f6\u81ea\u52a8\u67e5\u627e\u3002")]
        [SerializeField] private StoryService storyService;

        [Header("回合推进：城区摄像机重置")]
        [Tooltip("城区摄像机控制器；黑场全黑后会重置到 GlobalViewPoint，避免下一回合沿用局部观察点。为空时自动查找。")]
        [SerializeField] private CityCameraController cityCameraController;

        private void Awake()
        {
            if (roundService == null)
            {
                roundService = FindFirstObjectByType<RoundService>();
            }

            if (uiBootstrap == null)
            {
                uiBootstrap = FindFirstObjectByType<BaseSceneUIBootstrap>();
            }

            if (storyService == null)
            {
                storyService = FindFirstObjectByType<StoryService>();
            }

            if (cityCameraController == null)
            {
                cityCameraController = FindFirstObjectByType<CityCameraController>(FindObjectsInactive.Include);
            }
        }

        public void NextRound()
        {
            if (roundService == null)
            {
                return;
            }

            // 优先使用 DeskLoopController 的城区专用回合推进流程
            // \uff08\u5305\u62ec\u9ed1\u573a\u8fc7\u6e21\u3001\u5207\u6362\u56de\u684c\u9762\u3001\u63a8\u8fdb\u56de\u5408\u3001\u65b0\u56de\u5408\u521d\u59cb\u5316\u548c\u5267\u60c5\u89e6\u53d1\uff09\u3002
            var deskLoop = FindFirstObjectByType<DeskLoopController>();
            if (deskLoop != null)
            {
                deskLoop.EndRoundFromCityView();
                return;
            }

            // \u56de\u9000\uff1a\u627e\u4e0d\u5230 DeskLoopController \u65f6\u7528\u7b80\u5316\u6d41\u7a0b\uff1a\u9ed1\u573a → \u63a8\u8fdb → \u684c\u9762 → \u5267\u60c5\u3002
            StartCoroutine(AdvanceRoundWithTransition());
        }

        private IEnumerator AdvanceRoundWithTransition()
        {
            var blackPanel = uiBootstrap != null ? uiBootstrap.ShowBlackScreenPanel() : null;
            if (blackPanel != null)
            {
                yield return blackPanel.FadeIn(0.3f);
            }

            cityCameraController?.JumpToDefaultView();
            uiBootstrap?.ShowDesk();

            var advanced = roundService.NextRound();
            if (advanced && storyService != null)
            {
                storyService.StartNextQueuedStory();
            }

            if (blackPanel != null)
            {
                yield return blackPanel.FadeOut(0.3f);
                uiBootstrap?.HideBlackScreenPanel();
            }
        }

        public void Restart()
        {
            if (roundService != null)
            {
                roundService.RestartInitialDisaster();
            }
        }
    }
}
