using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class RoundPanelView : MonoBehaviour
    {
        [Header("依赖服务：读取当前回合和灾难阶段")]
        [Tooltip("回合服务；用于读取当前回合、总回合数和当前灾难阶段。为空时运行时自动查找。")]
        [SerializeField] private RoundService roundService;

        [Header("文本引用：回合数和当前阶段显示")]
        [Tooltip("当前回合文本；显示为 Day 换行回合数，例如 Day\\n1。")]
        [SerializeField] private TMP_Text roundText;
        [Tooltip("总回合数文本。")]
        [SerializeField] private TMP_Text totalRoundText;
        [Tooltip("当前灾难阶段文本；读取 DisasterStageConfig 的阶段名称。")]
        [SerializeField] private TMP_Text disasterStageText;
        [Tooltip("回合状态反馈文本。")]
        [SerializeField] private TMP_Text feedbackText;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            if (roundService != null)
            {
                roundService.RoundChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (roundService != null)
            {
                roundService.RoundChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            ResolveDependencies();
            if (roundService == null)
            {
                SetText(feedbackText, "缺少 RoundService。");
                SetText(disasterStageText, string.Empty);
                Debug.LogWarning("[回合面板] RoundService 为空，无法显示灾害阶段。");
                return;
            }

            roundService.EnsureInitialRuntimeData();
            SetText(roundText, $"Day\n{roundService.CurrentRound}");
            SetText(totalRoundText, $"共 {roundService.TotalRound} 回合");

            var stage = roundService.CurrentDisasterStage;
            if (stage != null)
            {
                Debug.Log($"[回合面板] 灾害阶段: {stage.StageId} → {stage.StageName} (回合 {stage.StartRound}-{stage.EndRound})");
                SetText(disasterStageText, stage.StageName);
            }
            else
            {
                Debug.LogWarning($"[回合面板] CurrentDisasterStage 为空: round={roundService.CurrentRound}");
                SetText(disasterStageText, "暂无阶段");
            }
            SetText(feedbackText, roundService.CurrentRound >= roundService.TotalRound ? "已到最后一回合。" : string.Empty);
        }

        private void ResolveDependencies()
        {
            if (roundService == null)
            {
                roundService = FindFirstObjectByType<RoundService>();
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }
}
