using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TwelveMoons.City
{
    public sealed class CitySideEventRegistry : MonoBehaviour
    {
        [Header("依赖服务：支线事件与城区点位")]
        [Tooltip("支线事件服务；用于读取 SideEventConfig，并判断当前回合哪些支线角色应该显示。")]
        [SerializeField] private CitySideEventService sideEventService;
        [Tooltip("城区点位注册器；用于把 SideEventConfig.PointId 定位到场景中的 CityPointView。")]
        [SerializeField] private CityPointRegistry pointRegistry;

        [Header("支线角色生成：只在点位上生成当前回合角色")]
        [Tooltip("支线角色图标父物体；留空时会在本物体下创建 SideEventViews。")]
        [SerializeField] private Transform sideEventViewRoot;
        [Tooltip("可选支线角色视图预制体；留空时会创建 2D SpriteRenderer 图标、点击碰撞体和红色感叹号。")]
        [SerializeField] private CitySideEventView sideEventViewPrefab;
        [Tooltip("启用后刷新时会为当前回合可显示但场景中缺少的支线事件创建图标。")]
        [SerializeField] private bool createMissingViews = true;
        [Tooltip("支线角色图标相对点位的本地偏移；用于把 2D 角色图标放在地图点位上方。")]
        [SerializeField] private Vector3 sideEventLocalOffset = new Vector3(0f, 0.8f, 0f);
        [Tooltip("默认 2D 支线角色图标缩放；数值会被限制为非负，避免生成反向或不可见图标。")]
        [SerializeField] private float defaultIconScale = 0.65f;

        [Header("运行时只读快照：生成与匹配状态")]
        [Tooltip("当前回合成功显示的支线事件数量。")]
        [SerializeField] private int inspectorVisibleViewCount;
        [Tooltip("当前回合可显示但找不到 CityPointView 的 PointId 列表。")]
        [SerializeField] private string inspectorMissingPointIds;
        [Tooltip("当前已绑定显示的 SideEventId 列表。")]
        [SerializeField] private string inspectorBoundSideEventIds;

        private readonly Dictionary<string, CitySideEventView> viewsBySideEventId =
            new Dictionary<string, CitySideEventView>();

        private void Awake()
        {
            ResolveDependencies();
            EnsureViewRoot();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            if (sideEventService != null)
            {
                sideEventService.SideEventsChanged += RefreshAndBind;
            }
        }

        private void OnDisable()
        {
            if (sideEventService != null)
            {
                sideEventService.SideEventsChanged -= RefreshAndBind;
            }
        }

        private void Start()
        {
            RefreshAndBind();
        }

        [ContextMenu("刷新支线角色点位显示")]
        public void RefreshAndBind()
        {
            ResolveDependencies();
            EnsureViewRoot();
            pointRegistry?.RefreshAndBind();
            CollectExistingViews();

            var visibleEvents = sideEventService != null
                ? sideEventService.GetVisibleEvents()
                : new List<SideEventDefinition>();
            var visibleIds = new HashSet<string>(visibleEvents.Select(definition => definition.SideEventId));
            var missingPointIds = new List<string>();
            var boundIds = new List<string>();

            foreach (var pair in viewsBySideEventId)
            {
                if (!visibleIds.Contains(pair.Key))
                {
                    pair.Value.ClearBinding();
                }
            }

            foreach (var definition in visibleEvents)
            {
                if (pointRegistry == null || !pointRegistry.TryGetView(definition.PointId, out var pointView))
                {
                    missingPointIds.Add($"{definition.SideEventId}:{definition.PointId}");
                    continue;
                }

                var view = GetOrCreateView(definition, pointView.transform);
                if (view == null)
                {
                    continue;
                }

                view.transform.SetParent(pointView.transform, false);
                view.transform.localPosition = sideEventLocalOffset;
                view.Bind(definition, sideEventService);
                viewsBySideEventId[definition.SideEventId] = view;
                boundIds.Add(definition.SideEventId);
            }

            inspectorVisibleViewCount = boundIds.Count;
            inspectorMissingPointIds = string.Join(", ", missingPointIds.OrderBy(id => id));
            inspectorBoundSideEventIds = string.Join(", ", boundIds.OrderBy(id => id));
        }

        private void ResolveDependencies()
        {
            if (sideEventService == null)
            {
                sideEventService = FindFirstObjectByType<CitySideEventService>(FindObjectsInactive.Include);
            }

            if (pointRegistry == null)
            {
                pointRegistry = FindFirstObjectByType<CityPointRegistry>(FindObjectsInactive.Include);
            }
        }

        private void EnsureViewRoot()
        {
            if (sideEventViewRoot != null)
            {
                return;
            }

            var existing = transform.Find("SideEventViews");
            if (existing == null)
            {
                existing = new GameObject("SideEventViews").transform;
                existing.SetParent(transform, false);
            }

            sideEventViewRoot = existing;
        }

        private void CollectExistingViews()
        {
            viewsBySideEventId.Clear();
            foreach (var view in FindObjectsByType<CitySideEventView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (view != null && !string.IsNullOrEmpty(view.SideEventId) && !viewsBySideEventId.ContainsKey(view.SideEventId))
                {
                    viewsBySideEventId.Add(view.SideEventId, view);
                }
            }
        }

        private CitySideEventView GetOrCreateView(SideEventDefinition definition, Transform pointTransform)
        {
            if (viewsBySideEventId.TryGetValue(definition.SideEventId, out var existing) && existing != null)
            {
                return existing;
            }

            if (!createMissingViews)
            {
                return null;
            }

            if (sideEventViewPrefab != null)
            {
                var instance = Instantiate(sideEventViewPrefab, pointTransform, false);
                instance.name = $"SideEvent_{definition.SideEventId}";
                instance.Configure(definition.SideEventId);
                return instance;
            }

            var icon = new GameObject($"SideEvent_{definition.SideEventId}");
            icon.transform.SetParent(pointTransform, false);
            icon.transform.localScale = Vector3.one * Mathf.Max(0f, defaultIconScale);
            var view = icon.AddComponent<CitySideEventView>();
            view.Configure(definition.SideEventId);
            view.EnsureDefaultWorldVisuals();
            return view;
        }

        private void OnValidate()
        {
            if (defaultIconScale < 0f)
            {
                defaultIconScale = 0f;
            }
        }
    }
}
