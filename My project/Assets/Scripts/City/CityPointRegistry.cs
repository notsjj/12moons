using System.Collections.Generic;
using System.Linq;
using TwelveMoons.Core.Config;
using UnityEngine;

namespace TwelveMoons.City
{
    public sealed class CityPointRegistry : MonoBehaviour
    {
        private const string TableName = "CityPointConfig";
        private const string IdFieldName = "PointId";

        [Header("配置来源：读取 CityPointConfig")]
        [Tooltip("配置管理器；留空时会在场景中自动查找，用于读取 CityPointConfig。")]
        [SerializeField] private ConfigManager configManager;

        [Tooltip("启用后 Start 时自动读取配置并绑定场景中的 CityPointView。")]
        [SerializeField] private bool bindOnStart = true;

        [Header("场景点位：与配置 PointId 匹配")]
        [Tooltip("需要参与匹配的 CityPointView 列表；为空或勾选自动收集时，会从场景中查找。")]
        [SerializeField] private List<CityPointView> pointViews = new List<CityPointView>();

        [Tooltip("启用后刷新时自动收集场景中所有 CityPointView，包含未激活物体。")]
        [SerializeField] private bool autoCollectSceneViews = true;

        [Header("运行时只读快照：点位匹配状态")]
        [Tooltip("CityPointConfig 中成功读取到的配置数量。")]
        [SerializeField] private int inspectorConfigCount;

        [Tooltip("本注册器参与匹配的 CityPointView 数量。")]
        [SerializeField] private int inspectorViewCount;

        [Tooltip("PointId 成功匹配配置的 CityPointView 数量。")]
        [SerializeField] private int inspectorMatchedViewCount;

        [Tooltip("场景中有 CityPointView，但在 CityPointConfig 中找不到对应 PointId 的列表。")]
        [SerializeField] private string inspectorUnmatchedViewPointIds;

        [Tooltip("CityPointConfig 中存在，但场景中没有 CityPointView 使用的 PointId 列表。")]
        [SerializeField] private string inspectorUnusedConfigPointIds;

        [Tooltip("场景中重复填写的 PointId 列表；重复点位会造成后续建筑或支线定位不明确。")]
        [SerializeField] private string inspectorDuplicateViewPointIds;

        private readonly Dictionary<string, CityPointDefinition> definitions =
            new Dictionary<string, CityPointDefinition>();

        private readonly Dictionary<string, CityPointView> viewsByPointId =
            new Dictionary<string, CityPointView>();

        public IReadOnlyCollection<CityPointDefinition> Definitions => definitions.Values;

        public IReadOnlyList<CityPointView> PointViews => pointViews;

        public int MatchedViewCount => inspectorMatchedViewCount;

        public int ConfigCount => inspectorConfigCount;

        public string UnmatchedViewPointIds => inspectorUnmatchedViewPointIds;

        public string UnusedConfigPointIds => inspectorUnusedConfigPointIds;

        public string DuplicateViewPointIds => inspectorDuplicateViewPointIds;

        private void Awake()
        {
            ResolveConfigManager();
        }

        private void Start()
        {
            if (bindOnStart)
            {
                RefreshAndBind();
            }
        }

        [ContextMenu("刷新城区点位匹配")]
        public void RefreshAndBind()
        {
            ResolveConfigManager();
            LoadDefinitions();
            CollectSceneViewsIfNeeded();
            BindViews();
            RefreshInspectorSnapshot();
        }

        public bool TryGetDefinition(string pointId, out CityPointDefinition definition)
        {
            return definitions.TryGetValue(pointId, out definition);
        }

        public bool TryGetView(string pointId, out CityPointView view)
        {
            return viewsByPointId.TryGetValue(pointId, out view);
        }

        private void ResolveConfigManager()
        {
            if (configManager == null)
            {
                configManager = FindFirstObjectByType<ConfigManager>();
            }
        }

        private void LoadDefinitions()
        {
            definitions.Clear();
            if (configManager == null || !configManager.TryGetTable(TableName, out var table))
            {
                return;
            }

            foreach (var row in table.Rows)
            {
                var definition = new CityPointDefinition(row);
                if (string.IsNullOrEmpty(definition.PointId))
                {
                    continue;
                }

                definitions[definition.PointId] = definition;
            }
        }

        private void CollectSceneViewsIfNeeded()
        {
            if (!autoCollectSceneViews && pointViews.Count > 0)
            {
                pointViews.RemoveAll(view => view == null);
                return;
            }

            pointViews = FindObjectsByType<CityPointView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(view => view != null)
                .OrderBy(view => view.PointId)
                .ThenBy(view => view.name)
                .ToList();
        }

        private void BindViews()
        {
            viewsByPointId.Clear();
            foreach (var view in pointViews)
            {
                if (view == null)
                {
                    continue;
                }

                if (definitions.TryGetValue(view.PointId, out var definition))
                {
                    view.Bind(definition);
                    if (!viewsByPointId.ContainsKey(view.PointId))
                    {
                        viewsByPointId.Add(view.PointId, view);
                    }
                }
                else
                {
                    view.ClearBinding();
                }
            }
        }

        private void RefreshInspectorSnapshot()
        {
            var usableViews = pointViews.Where(view => view != null).ToList();
            var groupedViewIds = usableViews
                .Select(view => view.PointId)
                .Where(id => !string.IsNullOrEmpty(id))
                .GroupBy(id => id)
                .ToList();
            var uniqueViewIds = new HashSet<string>(groupedViewIds.Select(group => group.Key));
            var unmatchedViewIds = uniqueViewIds
                .Where(id => !definitions.ContainsKey(id))
                .OrderBy(id => id);
            var unusedConfigIds = definitions.Keys
                .Where(id => !uniqueViewIds.Contains(id))
                .OrderBy(id => id);
            var duplicateIds = groupedViewIds
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(id => id);

            inspectorConfigCount = definitions.Count;
            inspectorViewCount = usableViews.Count;
            inspectorMatchedViewCount = usableViews.Count(view => view != null && view.IsMatched);
            inspectorUnmatchedViewPointIds = string.Join(", ", unmatchedViewIds);
            inspectorUnusedConfigPointIds = string.Join(", ", unusedConfigIds);
            inspectorDuplicateViewPointIds = string.Join(", ", duplicateIds);
        }
    }
}
