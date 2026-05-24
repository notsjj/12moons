using System.Collections.Generic;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class TaskPanelView : MonoBehaviour
    {
        [Header("依赖服务：读取任务配置和运行时任务")]
        [Tooltip("任务服务；用于读取任务配置、当前任务阶段和任务状态变化。")]
        [SerializeField] private TaskService taskService;
        [Tooltip("运行时数据服务；用于读取当前已经激活的任务列表。")]
        [SerializeField] private RuntimeDataService runtimeDataService;

        [Header("任务行显示：任务栏内容和空状态")]
        [Tooltip("任务行父节点；新建的任务行会挂在这里。")]
        [SerializeField] private RectTransform contentRoot;
        [Tooltip("任务行预制体；每个可见任务会实例化一个任务行。")]
        [SerializeField] private TaskRowView rowPrefab;
        [Tooltip("没有可见任务或缺少依赖时显示的提示文本。")]
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
                SetEmptyText("缺少任务服务。");
                return;
            }

            var visibleCount = 0;
            if (rowPrefab == null)
            {
                SetEmptyText("缺少任务行预制体。");
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

            SetEmptyText(visibleCount == 0 ? "暂无进行中的任务。" : "");
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
