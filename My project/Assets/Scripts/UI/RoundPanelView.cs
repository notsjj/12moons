using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class RoundPanelView : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private RoundService roundService;

        [Header("Text")]
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text totalRoundText;
        [SerializeField] private TMP_Text disasterStageText;
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

            SetText(roundText, $"Round {roundService.CurrentRound}");
            SetText(totalRoundText, $"Total {roundService.TotalRound}");

            var stage = roundService.CurrentDisasterStage;
            SetText(disasterStageText, stage != null ? stage.StageName : "No stage");
            SetText(feedbackText, roundService.CurrentRound >= roundService.TotalRound ? "Final round reached." : "");
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
