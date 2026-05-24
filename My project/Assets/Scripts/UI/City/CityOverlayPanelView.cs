using TwelveMoons.UI;
using UnityEngine;

namespace TwelveMoons.UI.City
{
    public sealed class CityOverlayPanelView : MonoBehaviour
    {
        [Header("城区界面组件：只读刷新当前阶段要求的覆盖层")]
        [Tooltip("城区左侧任务栏；复用桌面任务栏逻辑，用于在城区观察当前任务。")]
        [SerializeField] private TaskPanelView taskPanel;
        [Tooltip("城区专用质疑栏；只显示阵营图标、质疑度滑动条和数字，不显示桌面公文反馈。")]
        [SerializeField] private SuspicionPanelView citySuspicionPanel;
        [Tooltip("城区右上角回合面板；复用 RoundPanelView 显示当前回合和当前灾难阶段。")]
        [SerializeField] private RoundPanelView roundPanel;

        private void Awake()
        {
            ResolveMissingReferences();
        }

        private void OnEnable()
        {
            RefreshAll();
        }

        [ContextMenu("刷新城区覆盖层")]
        public void RefreshAll()
        {
            taskPanel?.Refresh();
            citySuspicionPanel?.Refresh();
            roundPanel?.Refresh();
        }

        private void ResolveMissingReferences()
        {
            if (taskPanel == null)
            {
                taskPanel = GetComponentInChildren<TaskPanelView>(true);
            }

            if (citySuspicionPanel == null)
            {
                citySuspicionPanel = GetComponentInChildren<SuspicionPanelView>(true);
            }

            if (roundPanel == null)
            {
                roundPanel = GetComponentInChildren<RoundPanelView>(true);
            }
        }
    }
}
