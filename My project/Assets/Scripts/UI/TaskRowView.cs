using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private Image statusIconImage;

        [Header("Manual Layout")]
        [SerializeField] private RectTransform rowRoot;
        [SerializeField] private LayoutElement rowLayoutElement;
        [SerializeField] private float collapsedHeight = 44f;
        [SerializeField] private float expandedHeight = 150f;

        [Header("Status Icons")]
        [SerializeField] private Sprite activeIcon;
        [SerializeField] private Sprite successIcon;
        [SerializeField] private Sprite failedIcon;

        private string taskId;
        private bool isExpanded;

        public event System.Action<string, bool> ExpandedChanged;

        public void Configure(TMP_Text title, TMP_Text status, TMP_Text stage, TMP_Text score)
        {
            Configure(null, null, title, null, null, status, stage, score, null);
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
            Image statusIcon)
        {
            Configure(expand, expandLabel, title, details, description, status, stage, score, statusIcon, null, null, 44f, 150f);
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
            Image statusIcon,
            RectTransform root,
            LayoutElement layoutElement,
            float collapsed,
            float expanded)
        {
            expandButton = expand;
            expandButtonText = expandLabel;
            titleText = title;
            detailRoot = details;
            descriptionText = description;
            statusText = status;
            stageText = stage;
            scoreText = score;
            statusIconImage = statusIcon;
            rowRoot = root;
            rowLayoutElement = layoutElement;
            collapsedHeight = collapsed;
            expandedHeight = expanded;
            RegisterExpandButton();
        }

        private void Awake()
        {
            ResolveLayoutReferences();
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
            SetStatusIcon(state.Status);
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
            ApplyManualHeight(isExpanded ? expandedHeight : collapsedHeight);

            if (notify && !string.IsNullOrEmpty(taskId))
            {
                ExpandedChanged?.Invoke(taskId, isExpanded);
            }
        }

        private void SetStatusIcon(TaskRuntimeStatus status)
        {
            if (statusIconImage == null)
            {
                return;
            }

            Sprite icon;
            switch (status)
            {
                case TaskRuntimeStatus.Completed:
                    icon = successIcon;
                    break;
                case TaskRuntimeStatus.Failed:
                    icon = failedIcon;
                    break;
                default:
                    icon = activeIcon;
                    break;
            }

            statusIconImage.sprite = icon;
            statusIconImage.enabled = icon != null;
            statusIconImage.preserveAspect = true;
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

        private void ResolveLayoutReferences()
        {
            if (rowRoot == null)
            {
                rowRoot = transform as RectTransform;
            }

            if (rowLayoutElement == null)
            {
                rowLayoutElement = GetComponent<LayoutElement>();
            }
        }

        private void ApplyManualHeight(float height)
        {
            ResolveLayoutReferences();

            if (rowRoot != null)
            {
                rowRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }

            if (rowLayoutElement != null)
            {
                rowLayoutElement.minHeight = height;
                rowLayoutElement.preferredHeight = height;
            }

            if (transform.parent is RectTransform parentRect)
            {
                LayoutRebuilder.MarkLayoutForRebuild(parentRect);
            }
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
