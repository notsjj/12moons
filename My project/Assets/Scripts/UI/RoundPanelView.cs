using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class RoundPanelView : MonoBehaviour
    {
        [Header("依赖服务：读取当前回合和灾难阶段")]
        [Tooltip("回合服务；用于读取当前回合、总回合数和当前灾难阶段。")]
        [SerializeField] private RoundService roundService;

        [Header("文本引用：回合数和当前阶段显示")]
        [Tooltip("当前回合文本。")]
        [SerializeField] private TMP_Text roundText;
        [Tooltip("总回合数文本。")]
        [SerializeField] private TMP_Text totalRoundText;
        [Tooltip("当前灾难阶段文本。")]
        [SerializeField] private TMP_Text disasterStageText;
        [Tooltip("回合状态反馈文本。")]
        [SerializeField] private TMP_Text feedbackText;

        private void Awake()
        {
            if (roundService == null)
            {
                roundService = FindFirstObjectByType<RoundService>();
            }
        }

        private void OnEnable()
        {
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
            if (roundService == null)
            {
                SetText(feedbackText, "RoundService missing.");
                return;
            }

            SetText(roundText, $"第 {roundService.CurrentRound} 回合");
            SetText(totalRoundText, $"共 {roundService.TotalRound} 回合");

            var stage = roundService.CurrentDisasterStage;
            SetText(disasterStageText, stage != null ? stage.StageName : "暂无阶段");
            SetText(feedbackText, roundService.CurrentRound >= roundService.TotalRound ? "已到最后一回合。" : "");
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
