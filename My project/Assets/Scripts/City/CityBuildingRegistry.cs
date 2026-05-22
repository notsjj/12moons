using System.Collections.Generic;
using System.Linq;
using TwelveMoons.Core.Config;
using UnityEngine;

namespace TwelveMoons.City
{
    public sealed class CityBuildingRegistry : MonoBehaviour
    {
        [Header("建筑服务：读取配置并执行点击效果")]
        [Tooltip("建筑服务；留空时会在场景中自动查找，用于读取 CityBuildingConfig 并执行建筑点击效果。")]
        [SerializeField] private CityBuildingService buildingService;
        [Tooltip("配置管理器；可选引用，仅用于在 Inspector 中明确本注册器依赖的配置来源。")]
        [SerializeField] private ConfigManager configManager;
        [Tooltip("启用后 Start 时自动绑定场景中的 CityBuildingView。")]
        [SerializeField] private bool bindOnStart = true;

        [Header("场景建筑：与配置 BuildingId 匹配")]
        [Tooltip("需要参与匹配的 CityBuildingView 列表；为空或勾选自动收集时，会从场景中查找，包含未激活物体。")]
        [SerializeField] private List<CityBuildingView> buildingViews = new List<CityBuildingView>();
        [Tooltip("启用后刷新时自动收集场景中所有 CityBuildingView，适合正式场景由模型子物体逐个挂脚本。")]
        [SerializeField] private bool autoCollectSceneViews = true;

        [Header("运行时只读快照：建筑匹配状态")]
        [Tooltip("CityBuildingConfig 中成功读取到的建筑配置数量。")]
        [SerializeField] private int inspectorConfigCount;
        [Tooltip("本注册器参与匹配的 CityBuildingView 数量。")]
        [SerializeField] private int inspectorViewCount;
        [Tooltip("BuildingId 成功匹配配置的 CityBuildingView 数量。")]
        [SerializeField] private int inspectorMatchedViewCount;
        [Tooltip("场景中有 CityBuildingView，但在 CityBuildingConfig 中找不到对应 BuildingId 的列表。")]
        [SerializeField] private string inspectorUnmatchedViewBuildingIds;
        [Tooltip("CityBuildingConfig 中存在，但场景中没有 CityBuildingView 使用的 BuildingId 列表。")]
        [SerializeField] private string inspectorUnusedConfigBuildingIds;
        [Tooltip("场景中重复填写的 BuildingId 列表；重复建筑会造成显示和点击目标不明确。")]
        [SerializeField] private string inspectorDuplicateViewBuildingIds;

        public IReadOnlyList<CityBuildingView> BuildingViews => buildingViews;

        public int MatchedViewCount => inspectorMatchedViewCount;

        public string UnmatchedViewBuildingIds => inspectorUnmatchedViewBuildingIds;

        public string UnusedConfigBuildingIds => inspectorUnusedConfigBuildingIds;

        public string DuplicateViewBuildingIds => inspectorDuplicateViewBuildingIds;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            if (buildingService != null)
            {
                buildingService.BuildingStatesChanged += RefreshBoundViewStates;
            }

            if (bindOnStart)
            {
                RefreshAndBind();
            }
            else
            {
                RefreshBoundViewStates();
            }
        }

        private void OnDisable()
        {
            if (buildingService != null)
            {
                buildingService.BuildingStatesChanged -= RefreshBoundViewStates;
            }
        }

        private void Start()
        {
            if (bindOnStart)
            {
                RefreshAndBind();
            }
        }

        [ContextMenu("刷新建筑绑定")]
        public void RefreshAndBind()
        {
            ResolveDependencies();
            buildingService?.Refresh();
            CollectSceneViewsIfNeeded();
            BindViews();
            RefreshInspectorSnapshot();
        }

        public void RefreshBoundViewStates()
        {
            foreach (var view in buildingViews)
            {
                if (view != null)
                {
                    view.RefreshState();
                }
            }

            RefreshInspectorSnapshot();
        }

        private void ResolveDependencies()
        {
            if (buildingService == null)
            {
                buildingService = FindFirstObjectByType<CityBuildingService>(FindObjectsInactive.Include);
            }

            if (configManager == null)
            {
                configManager = FindFirstObjectByType<ConfigManager>(FindObjectsInactive.Include);
            }
        }

        private void CollectSceneViewsIfNeeded()
        {
            if (!autoCollectSceneViews && buildingViews.Count > 0)
            {
                buildingViews.RemoveAll(view => view == null);
                return;
            }

            buildingViews = FindObjectsByType<CityBuildingView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(view => view != null)
                .OrderBy(view => view.BuildingId)
                .ThenBy(view => view.name)
                .ToList();
        }

        private void BindViews()
        {
            foreach (var view in buildingViews)
            {
                if (view == null)
                {
                    continue;
                }

                if (buildingService != null &&
                    buildingService.TryGetDefinition(view.BuildingId, out var definition))
                {
                    view.Bind(definition, buildingService);
                }
                else
                {
                    view.ClearBinding();
                }
            }
        }

        private void RefreshInspectorSnapshot()
        {
            var usableViews = buildingViews.Where(view => view != null).ToList();
            var groupedViewIds = usableViews
                .Select(view => view.BuildingId)
                .Where(id => !string.IsNullOrEmpty(id))
                .GroupBy(id => id)
                .ToList();
            var uniqueViewIds = new HashSet<string>(groupedViewIds.Select(group => group.Key));
            var configIds = buildingService != null
                ? new HashSet<string>(buildingService.Definitions.Select(definition => definition.BuildingId))
                : new HashSet<string>();

            inspectorConfigCount = configIds.Count;
            inspectorViewCount = usableViews.Count;
            inspectorMatchedViewCount = usableViews.Count(view => view != null && view.IsMatched);
            inspectorUnmatchedViewBuildingIds = string.Join(
                ", ",
                uniqueViewIds.Where(id => !configIds.Contains(id)).OrderBy(id => id));
            inspectorUnusedConfigBuildingIds = string.Join(
                ", ",
                configIds.Where(id => !uniqueViewIds.Contains(id)).OrderBy(id => id));
            inspectorDuplicateViewBuildingIds = string.Join(
                ", ",
                groupedViewIds.Where(group => group.Count() > 1).Select(group => group.Key).OrderBy(id => id));
        }
    }
}
