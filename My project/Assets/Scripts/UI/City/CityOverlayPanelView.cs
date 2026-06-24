using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI.City
{
    public sealed class CityOverlayPanelView : MonoBehaviour
    {
        private static readonly string[] CitySuspicionFactionIds = { "noble", "academy", "church", "civilian" };

        [Header("城区界面组件：只刷新当前阶段需要的覆盖层")]
        [Tooltip("共享任务栏；直接引用现有 TaskPanel，不在城区下生成 CityTaskPanel 副本。")]
        [SerializeField] private TaskPanelView taskPanel;

        [Tooltip("城区质疑度面板根节点；这里只作为查找范围保留，不复用桌面质疑度行的刷新逻辑。")]
        [SerializeField] private SuspicionPanelView citySuspicionPanel;

        [Tooltip("共享回合面板；直接引用现有 RoundPanel，不在城区下生成 CityRoundPanel 副本。")]
        [SerializeField] private RoundPanelView roundPanel;

        [Header("城区质疑度同步：按行名绑定")]
        [Tooltip("按 noble质疑行、academy质疑行、church质疑行、civilian质疑行 查到的子级 Slider；运行时会每次刷新重新查找，避免绑定到桌面面板。")]
        [SerializeField] private Slider[] citySuspicionSliders = System.Array.Empty<Slider>();

        [Tooltip("阵营服务，提供阵营配置和质疑度变化事件；为空时会自动从场景中查找。")]
        [SerializeField] private FactionService factionService;

        [Tooltip("运行时数据服务，提供当前阵营质疑度数值；为空时会自动从场景中查找。")]
        [SerializeField] private RuntimeDataService runtimeDataService;

        [Header("城区质疑度调试快照")]
        [Tooltip("只读快照：记录城区 HUD 最近一次按行名同步到的阵营、行名和值，方便在 Inspector 中排查。")]
        [SerializeField, TextArea(2, 8)] private string citySuspicionDebugSnapshot;

        private bool isListeningFactionChanges;

        private void Awake()
        {
            ResolveMissingReferences();
        }

        private void OnEnable()
        {
            ResolveMissingReferences();
            SubscribeFactionChangesIfNeeded();
            RefreshAll();
        }

        private void OnDisable()
        {
            if (factionService != null && isListeningFactionChanges)
            {
                factionService.FactionsChanged -= RefreshCitySuspicionSliders;
            }

            isListeningFactionChanges = false;
        }

        [ContextMenu("刷新城区覆盖层")]
        public void RefreshAll()
        {
            RefreshCitySuspicionSliders();
            taskPanel?.Refresh();
            roundPanel?.Refresh();
        }

        private void ResolveMissingReferences()
        {
            if (taskPanel == null)
            {
                taskPanel = FindScenePanel<TaskPanelView>("任务面板", "TaskPanel");
            }

            if (citySuspicionPanel == null)
            {
                citySuspicionPanel = GetComponentInChildren<SuspicionPanelView>(true);
            }

            if (citySuspicionPanel == null)
            {
                citySuspicionPanel = EnsureCitySuspicionPanel();
            }

            ResolveCitySuspicionSliders();

            if (factionService == null)
            {
                factionService = Object.FindFirstObjectByType<FactionService>(FindObjectsInactive.Include);
            }

            if (runtimeDataService == null)
            {
                runtimeDataService = Object.FindFirstObjectByType<RuntimeDataService>(FindObjectsInactive.Include);
            }

            SubscribeFactionChangesIfNeeded();

            if (roundPanel == null)
            {
                roundPanel = FindScenePanel<RoundPanelView>("回合面板", "RoundPanel");
            }
        }

        private static T FindScenePanel<T>(params string[] objectNames) where T : Component
        {
            var directPanel = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (directPanel != null)
            {
                return directPanel;
            }

            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in transforms)
            {
                if (candidate == null || !MatchesAnyName(candidate.name, objectNames))
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

        private SuspicionPanelView EnsureCitySuspicionPanel()
        {
            var rows = GetComponentsInChildren<FactionSuspicionRow>(true);
            if (rows == null || rows.Length == 0)
            {
                return null;
            }

            var panelRoot = rows[0] != null ? rows[0].transform.parent : null;
            if (panelRoot == null)
            {
                return null;
            }

            var panel = panelRoot.GetComponent<SuspicionPanelView>();
            if (panel != null)
            {
                return panel;
            }

            return panelRoot.gameObject.AddComponent<SuspicionPanelView>();
        }

        private void RefreshCitySuspicionSliders()
        {
            ResolveCitySuspicionSliders();

            if (factionService == null)
            {
                factionService = Object.FindFirstObjectByType<FactionService>(FindObjectsInactive.Include);
            }

            if (runtimeDataService == null)
            {
                runtimeDataService = Object.FindFirstObjectByType<RuntimeDataService>(FindObjectsInactive.Include);
            }

            SubscribeFactionChangesIfNeeded();

            if (factionService == null || runtimeDataService == null)
            {
                citySuspicionDebugSnapshot = "城区质疑度同步失败：缺少 FactionService 或 RuntimeDataService。";
                return;
            }

            var snapshotBuilder = new System.Text.StringBuilder();
            snapshotBuilder.AppendLine($"同步时间：{Time.frameCount}");

            for (var index = 0; index < CitySuspicionFactionIds.Length; index++)
            {
                var roleFactionId = CitySuspicionFactionIds[index];
                var configuredFactionId = FactionRoleIdResolver.ResolveConfiguredFactionId(factionService, roleFactionId);
                var rowName = GetCitySuspicionRowName(roleFactionId);
                var slider = citySuspicionSliders != null && index < citySuspicionSliders.Length
                    ? citySuspicionSliders[index]
                    : null;

                if (slider == null)
                {
                    snapshotBuilder.AppendLine($"{rowName}：未找到子级 Slider");
                    continue;
                }

                var definition = FindFactionDefinition(configuredFactionId);
                var initSuspicion = definition != null ? definition.InitSuspicion : 0;
                var maxSuspicion = Mathf.Max(1, definition != null ? definition.MaxSuspicion : 1);
                var state = runtimeDataService.Data.GetOrCreateFaction(configuredFactionId, initSuspicion);
                var value = Mathf.Clamp(state.Suspicion, 0, maxSuspicion);

                slider.minValue = 0;
                slider.maxValue = maxSuspicion;
                slider.SetValueWithoutNotify(value);

                snapshotBuilder.AppendLine($"{rowName}：{value}/{maxSuspicion}");
            }

            citySuspicionDebugSnapshot = snapshotBuilder.ToString().TrimEnd();
        }

        private void ResolveCitySuspicionSliders()
        {
            var matchedSliders = new Slider[CitySuspicionFactionIds.Length];
            for (var index = 0; index < CitySuspicionFactionIds.Length; index++)
            {
                var rowName = GetCitySuspicionRowName(CitySuspicionFactionIds[index]);
                var rowTransform = FindChildByName(transform, rowName);
                matchedSliders[index] = rowTransform != null ? rowTransform.GetComponentInChildren<Slider>(true) : null;
            }

            citySuspicionSliders = matchedSliders;
        }

        private void SubscribeFactionChangesIfNeeded()
        {
            if (!isActiveAndEnabled || factionService == null || isListeningFactionChanges)
            {
                return;
            }

            factionService.FactionsChanged += RefreshCitySuspicionSliders;
            isListeningFactionChanges = true;
        }

        private FactionDefinition FindFactionDefinition(string factionId)
        {
            if (factionService == null || string.IsNullOrEmpty(factionId))
            {
                return null;
            }

            var definitions = factionService.Definitions;
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition != null && definition.FactionId == factionId)
                {
                    return definition;
                }
            }

            return null;
        }

        private static string GetCitySuspicionRowName(string factionId)
        {
            return $"{factionId}质疑行";
        }

        private static Transform FindChildByName(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            var children = root.GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (child != null && child.name == objectName)
                {
                    return child;
                }
            }

            return null;
        }

        private static bool MatchesAnyName(string candidateName, string[] objectNames)
        {
            foreach (var objectName in objectNames)
            {
                if (candidateName == objectName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
