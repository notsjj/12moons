using System.Collections.Generic;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class TaskPanelView : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private TaskService taskService;
        [SerializeField] private RuntimeDataService runtimeDataService;

        [Header("Rows")]
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private TaskRowView rowPrefab;
        [SerializeField] private TMP_Text emptyText;

        private readonly List<TaskRowView> rows = new List<TaskRowView>();
        private readonly HashSet<string> expandedTaskIds = new HashSet<string>();

        private void Awake()
        {
            if (taskService == null)
            {
                taskService = FindFirstObjectByType<TaskService>();
            }

            if (runtimeDataService == null)
            {
                runtimeDataService = FindFirstObjectByType<RuntimeDataService>();
            }

            if (contentRoot == null)
            {
                contentRoot = transform as RectTransform;
            }
        }

        private void OnEnable()
        {
            if (taskService != null)
            {
                taskService.TasksChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (taskService != null)
            {
                taskService.TasksChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            ClearRows();

            if (taskService == null || runtimeDataService == null || contentRoot == null)
            {
                SetEmptyText("TaskService missing.");
                return;
            }

            var visibleCount = 0;
            foreach (var state in runtimeDataService.Data.Tasks)
            {
                if (!taskService.TryGetDefinition(state.TaskId, out var definition) ||
                    !definition.ShowInTaskPanel ||
                    state.Status == TaskRuntimeStatus.NotStarted)
                {
                    continue;
                }

                CreateRow(definition, state, taskService.GetCurrentStage(state));
                visibleCount++;
            }

            SetEmptyText(visibleCount == 0 ? "No active task." : "");
        }

        private void CreateRow(TaskDefinition definition, RuntimeTaskState state, TaskStageDefinition stage)
        {
            var row = rowPrefab != null
                ? Instantiate(rowPrefab, contentRoot)
                : CreateDefaultRow(contentRoot);

            row.ExpandedChanged += OnRowExpandedChanged;
            row.Bind(definition, state, stage, expandedTaskIds.Contains(state.TaskId));
            rows.Add(row);
        }

        private TaskRowView CreateDefaultRow(Transform parent)
        {
            var rowObject = new GameObject("TaskRow", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            rowObject.transform.SetParent(parent, false);

            var rectTransform = rowObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(360f, 44f);

            var image = rowObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.16f, 0.15f, 1f);

            var layout = rowObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 8);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var rowLayout = rowObject.AddComponent<LayoutElement>();
            rowLayout.minHeight = 44f;
            rowLayout.preferredHeight = -1f;

            var header = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            header.transform.SetParent(rowObject.transform, false);
            var headerLayout = header.GetComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 6f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;

            var headerElement = header.AddComponent<LayoutElement>();
            headerElement.minHeight = 30f;
            headerElement.preferredHeight = 30f;

            var expandButton = CreateIconButton("ExpandButton", header.transform, 28f);
            var expandButtonText = expandButton.transform.Find("Label").GetComponent<TMP_Text>();

            var titleText = CreateText("TitleText", header.transform, 16, FontStyles.Bold);
            var titleLayout = titleText.GetComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;

            var resultIcon = CreateResultIcon(header.transform, out var resultIconText);

            var details = new GameObject("DetailRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
            details.transform.SetParent(rowObject.transform, false);
            var detailLayout = details.GetComponent<VerticalLayoutGroup>();
            detailLayout.spacing = 3f;
            detailLayout.childControlWidth = true;
            detailLayout.childControlHeight = true;
            detailLayout.childForceExpandWidth = true;
            detailLayout.childForceExpandHeight = false;

            var descriptionText = CreateText("DescriptionText", details.transform, 13, FontStyles.Normal);
            descriptionText.overflowMode = TextOverflowModes.Overflow;
            var statusText = CreateText("StatusText", details.transform, 12, FontStyles.Normal);
            var stageText = CreateText("StageText", details.transform, 12, FontStyles.Normal);
            var scoreText = CreateText("ScoreText", details.transform, 12, FontStyles.Normal);

            var row = rowObject.AddComponent<TaskRowView>();
            row.Configure(expandButton, expandButtonText, titleText, details, descriptionText, statusText, stageText, scoreText, resultIcon, resultIconText);
            return row;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, int fontSize, FontStyles style)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Left;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.color = Color.white;
            text.raycastTarget = false;

            var layout = textObject.AddComponent<LayoutElement>();
            layout.minHeight = fontSize + 6f;
            layout.preferredHeight = fontSize + 8f;
            return text;
        }

        private static Button CreateIconButton(string name, Transform parent, float size)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            var layout = buttonObject.GetComponent<LayoutElement>();
            layout.minWidth = size;
            layout.preferredWidth = size;
            layout.minHeight = size;
            layout.preferredHeight = size;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.24f, 0.24f, 0.21f, 1f);

            var label = CreateText("Label", buttonObject.transform, 16, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Center;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Object.Destroy(label.GetComponent<LayoutElement>());

            return buttonObject.GetComponent<Button>();
        }

        private static Image CreateResultIcon(Transform parent, out TMP_Text label)
        {
            var iconObject = new GameObject("ResultIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconObject.transform.SetParent(parent, false);

            var layout = iconObject.GetComponent<LayoutElement>();
            layout.minWidth = 30f;
            layout.preferredWidth = 30f;
            layout.minHeight = 22f;
            layout.preferredHeight = 22f;

            var image = iconObject.GetComponent<Image>();
            image.enabled = false;

            label = CreateText("Label", iconObject.transform, 11, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Center;
            label.enabled = false;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Object.Destroy(label.GetComponent<LayoutElement>());

            return image;
        }

        private void ClearRows()
        {
            foreach (var row in rows)
            {
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            rows.Clear();
        }

        private void OnRowExpandedChanged(string taskId, bool expanded)
        {
            if (expanded)
            {
                expandedTaskIds.Add(taskId);
            }
            else
            {
                expandedTaskIds.Remove(taskId);
            }
        }

        private void SetEmptyText(string value)
        {
            if (emptyText != null)
            {
                emptyText.text = value;
            }
        }
    }
}
