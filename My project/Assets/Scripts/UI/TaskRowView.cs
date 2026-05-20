using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine.UI;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class TaskRowView : MonoBehaviour
    {
        [SerializeField] private Button expandButton;
        [SerializeField] private TMP_Text expandButtonText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Image resultIconImage;
        [SerializeField] private TMP_Text resultIconText;

        private string taskId;
        private bool isExpanded;

        public event System.Action<string, bool> ExpandedChanged;

        public void Configure(TMP_Text title, TMP_Text status, TMP_Text stage, TMP_Text score)
        {
            Configure(null, null, title, null, null, status, stage, score, null, null);
        }

        public void Configure(
            Button expand,
            TMP_Text expandLabel,
            TMP_Text title,
            GameObject details,
            TMP_Text description,
            TMP_Text status,
            TMP_Text stage,
            TMP_Text score,
            Image resultIcon,
            TMP_Text resultLabel)
        {
            expandButton = expand;
            expandButtonText = expandLabel;
            titleText = title;
            detailRoot = details;
            descriptionText = description;
            statusText = status;
            stageText = stage;
            scoreText = score;
            resultIconImage = resultIcon;
            resultIconText = resultLabel;
            RegisterExpandButton();
        }

        private void Awake()
        {
            RegisterExpandButton();
        }

        public void Bind(TaskDefinition definition, RuntimeTaskState state, TaskStageDefinition stage, bool expanded)
        {
            taskId = state.TaskId;
            SetText(titleText, string.IsNullOrEmpty(definition.TaskName) ? definition.TaskId : definition.TaskName);
            SetText(descriptionText, string.IsNullOrEmpty(definition.Description) ? "No task description configured." : definition.Description);
            SetText(statusText, $"Status: {state.Status}");
            SetText(stageText, stage != null ? $"Stage {stage.StageIndex}: {stage.StageDescription}" : "Stage: none");
            SetText(scoreText, $"Score: {state.Score} / {definition.SuccessScore}");
            SetResultIcon(state.Status);
            SetExpanded(expanded, false);
        }

        public void Bind(TaskDefinition definition, RuntimeTaskState state, TaskStageDefinition stage)
        {
            Bind(definition, state, stage, false);
        }

        private void ToggleExpanded()
        {
            SetExpanded(!isExpanded, true);
        }

        private void SetExpanded(bool expanded, bool notify)
        {
            isExpanded = expanded;
            if (detailRoot != null)
            {
                detailRoot.SetActive(isExpanded);
            }

            SetText(expandButtonText, isExpanded ? "-" : "+");

            if (notify && !string.IsNullOrEmpty(taskId))
            {
                ExpandedChanged?.Invoke(taskId, isExpanded);
            }
        }

        private void SetResultIcon(TaskRuntimeStatus status)
        {
            var hasResult = status == TaskRuntimeStatus.Completed || status == TaskRuntimeStatus.Failed;
            if (resultIconImage != null)
            {
                resultIconImage.enabled = hasResult;
                resultIconImage.color = status == TaskRuntimeStatus.Completed
                    ? new Color(0.16f, 0.56f, 0.28f, 1f)
                    : new Color(0.64f, 0.18f, 0.16f, 1f);
            }

            if (resultIconText != null)
            {
                resultIconText.enabled = hasResult;
                resultIconText.text = status == TaskRuntimeStatus.Completed ? "OK" : "X";
            }
        }

        private void RegisterExpandButton()
        {
            if (expandButton == null)
            {
                return;
            }

            expandButton.onClick.RemoveListener(ToggleExpanded);
            expandButton.onClick.AddListener(ToggleExpanded);
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
