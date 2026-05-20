using System.Collections.Generic;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;

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
            if (rowPrefab == null)
            {
                SetEmptyText("TaskRow prefab missing.");
                return;
            }

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
            var row = Instantiate(rowPrefab, contentRoot);

            row.ExpandedChanged += OnRowExpandedChanged;
            row.Bind(definition, state, stage, expandedTaskIds.Contains(state.TaskId));
            rows.Add(row);
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
