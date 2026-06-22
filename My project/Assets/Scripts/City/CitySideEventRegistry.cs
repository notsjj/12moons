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
        [Tooltip("支线角色图标父物体；留空时会在本物体下创建 SideEventViews，运行时生成的 SideEvent_* 角色根物体会统一放在这里，方便 Inspector 检查脚本和状态。")]
        [SerializeField] private Transform sideEventViewRoot;
        [Tooltip("可选支线角色视图预制体；留空时会创建 2D SpriteRenderer 图标、点击碰撞体和红色感叹号。")]
        [SerializeField] private CitySideEventView sideEventViewPrefab;
        [Tooltip("启用后刷新时会为当前回合可显示但场景中缺少的支线事件创建图标。")]
        [SerializeField] private bool createMissingViews = true;
        [Tooltip("支线角色图标相对点位的本地偏移；用于把 2D 角色图标放在地图点位上方。")]
        [SerializeField] private Vector3 sideEventLocalOffset = new Vector3(0f, -0.15f, 0f);
        [Tooltip("\u652f\u7ebf\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d\u751f\u6210\u65f6\u5f3a\u5236\u4f7f\u7528\u7684 Y \u8f74\u504f\u79fb\uff1b\u4f18\u5148\u7ea7\u9ad8\u4e8e\u65e7\u7248\u504f\u79fb\u5b57\u6bb5\uff0c\u7528\u4e8e\u4fdd\u8bc1\u70b9\u4f4d\u76f8\u5bf9\u5efa\u7b51\u9876\u90e8\u4e0b\u79fb 0.4\u3002")]
        [SerializeField] private float sideEventBoundsYOffset = -0.4f;
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
            ClearPointSideEventBindings();
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

                pointView.BindSideEvent(definition, sideEventService);

                var view = GetOrCreateView(definition, pointView.transform);
                if (view == null)
                {
                    continue;
                }

                PlaceViewAtPoint(view.transform, pointView);
                view.Bind(definition, sideEventService);
                viewsBySideEventId[definition.SideEventId] = view;
                boundIds.Add(definition.SideEventId);
            }

            inspectorVisibleViewCount = boundIds.Count;
            inspectorMissingPointIds = string.Join(", ", missingPointIds.OrderBy(id => id));
            inspectorBoundSideEventIds = string.Join(", ", boundIds.OrderBy(id => id));
        }

        private void ClearPointSideEventBindings()
        {
            if (pointRegistry == null)
            {
                return;
            }

            foreach (var pointView in pointRegistry.PointViews)
            {
                pointView?.ClearSideEventBinding();
            }
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
                if (sideEventViewRoot != null && existing.transform.parent != sideEventViewRoot)
                {
                    existing.transform.SetParent(sideEventViewRoot, true);
                }

                existing.EnsureDefaultWorldVisuals();
                return existing;
            }

            if (!createMissingViews)
            {
                return null;
            }

            if (sideEventViewPrefab != null)
            {
                var instance = Instantiate(sideEventViewPrefab, sideEventViewRoot, false);
                instance.name = $"SideEvent_{definition.SideEventId}";
                instance.Configure(definition.SideEventId);
                instance.EnsureDefaultWorldVisuals();
                return instance;
            }

            var icon = new GameObject($"SideEvent_{definition.SideEventId}");
            icon.transform.SetParent(sideEventViewRoot != null ? sideEventViewRoot : pointTransform, false);
            icon.transform.localScale = Vector3.one * Mathf.Max(0f, defaultIconScale);
            var view = icon.AddComponent<CitySideEventView>();
            view.Configure(definition.SideEventId);
            view.EnsureDefaultWorldVisuals();
            return view;
        }

        private void PlaceViewAtPoint(Transform viewTransform, CityPointView pointView)
        {
            if (viewTransform == null || pointView == null)
            {
                return;
            }

            if (sideEventViewRoot != null && viewTransform.parent != sideEventViewRoot)
            {
                viewTransform.SetParent(sideEventViewRoot, true);
            }

            viewTransform.position = pointView.GetWorldPositionAboveBounds(GetSideEventPlacementOffset());
            viewTransform.localScale = Vector3.one * Mathf.Max(0f, defaultIconScale);
        }

        private Vector3 GetSideEventPlacementOffset()
        {
            return new Vector3(sideEventLocalOffset.x, sideEventBoundsYOffset, sideEventLocalOffset.z);
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
