using TwelveMoons.UI;
using UnityEngine;

namespace TwelveMoons.UI.City
{
    public sealed class CityOverlayPanelView : MonoBehaviour
    {
        [Header("城区界面组件：只读刷新当前阶段要求的覆盖层")]
        [Tooltip("共享任务栏；直接引用现有 TaskPanel，不在城区下生成 CityTaskPanel 副本。")]
        [SerializeField] private TaskPanelView taskPanel;

        [Tooltip("城区专用质疑栏；只显示城区观察所需的阵营质疑度信息。")]
        [SerializeField] private SuspicionPanelView citySuspicionPanel;

        [Tooltip("共享回合面板；直接引用现有 RoundPanel，不在城区下生成 CityRoundPanel 副本。")]
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
                taskPanel = FindScenePanel<TaskPanelView>("TaskPanel");
            }

            if (citySuspicionPanel == null)
            {
                citySuspicionPanel = GetComponentInChildren<SuspicionPanelView>(true);
            }

            if (roundPanel == null)
            {
                roundPanel = FindScenePanel<RoundPanelView>("RoundPanel");
            }
        }

        private static T FindScenePanel<T>(string objectName) where T : Component
        {
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in transforms)
            {
                if (candidate == null || candidate.name != objectName)
                {
                    continue;
                }

                var panel = candidate.GetComponent<T>();
                if (panel != null)
                {
                    return panel;
                }
            }

            return null;
        }
    }
}
