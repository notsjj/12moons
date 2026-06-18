using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

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

        [Header("任务面板收起：点击按钮后只露出这个展开按钮")]
        [Tooltip("面板级展开/收起按钮；点击后移动整个任务面板，不影响任务行自己的展开按钮。")]
        [SerializeField] private Button panelCollapseButton;
        [Tooltip("需要被 DOTween 移动的任务面板根节点；为空时使用当前脚本所在 RectTransform。")]
        [SerializeField] private RectTransform panelMoveRoot;
        [Tooltip("任务面板从原位移动到收起位置，或从收起位置返回原位的持续时间。")]
        [SerializeField, Min(0.01f)] private float panelCollapseDuration = 0.35f;
        [Tooltip("收起/展开移动使用的缓动类型。")]
        [SerializeField] private Ease panelCollapseEase = Ease.OutCubic;
        [Tooltip("运行时快照：当前任务面板是否处于收起状态，仅用于 Inspector 观察。")]
        [SerializeField] private bool isPanelCollapsedSnapshot;
        [Tooltip("运行时快照：面板原始展开位置，用于再次点击时恢复到原样。")]
        [SerializeField] private Vector2 panelOpenAnchoredPositionSnapshot;
        [Tooltip("运行时快照：计算这个按钮的左边界碰到屏幕左边界时，任务面板应该移动到的位置。")]
        [SerializeField] private Vector2 panelCollapsedAnchoredPositionSnapshot;

        private readonly List<TaskRowView> rows = new List<TaskRowView>();
        private readonly HashSet<string> expandedTaskIds = new HashSet<string>();
        private Tweener panelCollapseTween;
        private bool hasPanelOpenPosition;

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

            ResolvePanelCollapseReferences();
            RegisterPanelCollapseButton();
        }

        private void OnEnable()
        {
            ResolvePanelCollapseReferences();
            CapturePanelOpenPositionIfNeeded();
            RegisterPanelCollapseButton();

            if (taskService != null)
            {
                taskService.TasksChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            KillPanelCollapseTween();

            if (taskService != null)
            {
                taskService.TasksChanged -= Refresh;
            }
        }

        public void TogglePanelCollapsed()
        {
            SetPanelCollapsed(!isPanelCollapsedSnapshot);
        }

        public void SetPanelCollapsed(bool collapsed)
        {
            ResolvePanelCollapseReferences();
            CapturePanelOpenPositionIfNeeded();

            if (panelMoveRoot == null)
            {
                return;
            }

            var targetPosition = collapsed
                ? CalculateCollapsedAnchoredPosition()
                : panelOpenAnchoredPositionSnapshot;

            panelCollapsedAnchoredPositionSnapshot = collapsed ? targetPosition : panelCollapsedAnchoredPositionSnapshot;
            isPanelCollapsedSnapshot = collapsed;

            KillPanelCollapseTween();
            panelCollapseTween = panelMoveRoot
                .DOAnchorPos(targetPosition, Mathf.Max(0.01f, panelCollapseDuration))
                .SetEase(panelCollapseEase)
                .SetUpdate(true)
                .OnComplete(() => panelCollapseTween = null);
        }

        public Vector2 EditorCalculateCollapsedAnchoredPosition()
        {
            ResolvePanelCollapseReferences();
            CapturePanelOpenPositionIfNeeded();
            return CalculateCollapsedAnchoredPosition();
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

        private void ResolvePanelCollapseReferences()
        {
            if (panelMoveRoot == null)
            {
                panelMoveRoot = transform as RectTransform;
            }
        }

        private void RegisterPanelCollapseButton()
        {
            if (panelCollapseButton == null)
            {
                return;
            }

            if (HasPersistentPanelCollapseBinding())
            {
                return;
            }

            panelCollapseButton.onClick.RemoveListener(TogglePanelCollapsed);
            panelCollapseButton.onClick.AddListener(TogglePanelCollapsed);
        }

        private bool HasPersistentPanelCollapseBinding()
        {
            if (panelCollapseButton == null)
            {
                return false;
            }

            for (var index = 0; index < panelCollapseButton.onClick.GetPersistentEventCount(); index++)
            {
                if (panelCollapseButton.onClick.GetPersistentTarget(index) == this &&
                    panelCollapseButton.onClick.GetPersistentMethodName(index) == nameof(TogglePanelCollapsed))
                {
                    return true;
                }
            }

            return false;
        }

        private void CapturePanelOpenPositionIfNeeded()
        {
            if (hasPanelOpenPosition || panelMoveRoot == null)
            {
                return;
            }

            panelOpenAnchoredPositionSnapshot = panelMoveRoot.anchoredPosition;
            panelCollapsedAnchoredPositionSnapshot = CalculateCollapsedAnchoredPosition();
            hasPanelOpenPosition = true;
        }

        private Vector2 CalculateCollapsedAnchoredPosition()
        {
            if (panelMoveRoot == null || panelCollapseButton == null)
            {
                return panelMoveRoot != null ? panelMoveRoot.anchoredPosition : Vector2.zero;
            }

            var moveParent = panelMoveRoot.parent as RectTransform;
            var buttonRect = panelCollapseButton.transform as RectTransform;
            if (moveParent == null || buttonRect == null)
            {
                return panelMoveRoot.anchoredPosition;
            }

            var screenLeftLocalX = GetScreenLeftInParentLocal(moveParent);
            var buttonLeftLocalX = GetRectLeftInParentLocal(buttonRect, moveParent);
            var deltaX = screenLeftLocalX - buttonLeftLocalX;
            return panelMoveRoot.anchoredPosition + new Vector2(deltaX, 0f);
        }

        private float GetScreenLeftInParentLocal(RectTransform parent)
        {
            var canvas = GetComponentInParent<Canvas>();
            var canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null)
            {
                return parent.rect.xMin;
            }

            return GetRectLeftInParentLocal(canvasRect, parent);
        }

        private static float GetRectLeftInParentLocal(RectTransform rectTransform, RectTransform parent)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            var left = float.PositiveInfinity;
            for (var index = 0; index < corners.Length; index++)
            {
                var localPoint = parent.InverseTransformPoint(corners[index]);
                if (localPoint.x < left)
                {
                    left = localPoint.x;
                }
            }

            return left;
        }

        private void KillPanelCollapseTween()
        {
            if (panelCollapseTween != null && panelCollapseTween.IsActive())
            {
                panelCollapseTween.Kill();
            }

            panelCollapseTween = null;
        }
    }
}
