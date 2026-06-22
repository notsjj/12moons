using System;
using System.Linq;
using TwelveMoons.Core;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.City
{
    [DisallowMultipleComponent]
    public sealed class CityPointView : MonoBehaviour
    {
        [Header("点位配置匹配：对应 CityPointConfig.PointId")]
        [Tooltip("\u57ce\u533a\u70b9\u4f4d ID\uff1b\u5fc5\u987b\u4e0e CityPointConfig \u8868\u4e2d\u7684 PointId \u5b8c\u5168\u4e00\u81f4\uff0c\u6ce8\u518c\u5668\u4f1a\u7528\u5b83\u5339\u914d\u914d\u7f6e\u3002")]
        [SerializeField] private string pointId;

        [Tooltip("\u662f\u5426\u5728 Scene \u89c6\u56fe\u7ed8\u5236\u70b9\u4f4d\u6807\u8bb0\uff1b\u53ea\u7528\u4e8e\u7f16\u8f91\u5668\u89c2\u5bdf\uff0c\u4e0d\u89e6\u53d1\u5efa\u7b51\u3001\u5267\u60c5\u6216\u8d44\u6e90\u903b\u8f91\u3002")]
        [SerializeField] private bool drawSceneGizmo = true;
        [Tooltip("Scene \u89c6\u56fe\u70b9\u4f4d\u6807\u8bb0\u534a\u5f84\uff1b\u4ec5\u7528\u4e8e\u89c2\u5bdf\u70b9\u4f4d\u4f4d\u7f6e\uff0c\u4e0d\u80fd\u586b\u8d1f\u6570\u3002")]
        [SerializeField] private float gizmoRadius = 0.25f;
        [Tooltip("Scene \u89c6\u56fe\u70b9\u4f4d\u6807\u8bb0\u989c\u8272\uff1b\u7528\u4e8e\u533a\u5206\u5df2\u7ecf\u5339\u914d\u914d\u7f6e\u7684\u70b9\u4f4d\u3002")]
        [SerializeField] private Color matchedGizmoColor = new Color(0.25f, 0.85f, 0.55f, 0.9f);
        [Tooltip("Scene \u89c6\u56fe\u70b9\u4f4d\u6807\u8bb0\u989c\u8272\uff1b\u7528\u4e8e\u63d0\u793a PointId \u672a\u5339\u914d\u5230\u914d\u7f6e\u3002")]
        [SerializeField] private Color unmatchedGizmoColor = new Color(1f, 0.35f, 0.2f, 0.9f);

        [Header("\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d\uff1a\u652f\u7ebf\u4e8b\u4ef6\u51fa\u73b0\u65f6\u663e\u793a")]
        [Tooltip("\u662f\u5426\u5141\u8bb8\u8be5\u70b9\u4f4d\u751f\u6210\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d\uff1b\u5f53\u524d\u56de\u5408\u6709\u652f\u7ebf\u4e8b\u4ef6\u65f6\u4f1a\u81ea\u52a8\u663e\u793a\u3002")]
        [SerializeField] private bool showPortrait = true;
        [Tooltip("\u6ca1\u6709\u652f\u7ebf\u89d2\u8272 ID \u65f6\u4f7f\u7528\u7684\u4e34\u65f6\u4eba\u7269\u7acb\u7ed8\u8d44\u6e90\u8def\u5f84\u3002")]
        [SerializeField] private string portraitResourceId = "Art/Art/Character/IMG_0940";
        [Tooltip("\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d Prefab \u7684 Resources \u8def\u5f84\uff1b\u8fd0\u884c\u65f6\u5b9e\u4f8b\u5316\u4e3a\u5f53\u524d CityPointView \u5efa\u7b51\u7269\u4f53\u7684\u5b50\u7269\u4f53\u3002")]
        [SerializeField] private string portraitPrefabResourcePath = "Prefabs/UI/\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d";
        [Tooltip("\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d\u76f8\u5bf9\u5efa\u7b51\u5305\u56f4\u76d2\u9876\u90e8\u7684\u504f\u79fb\uff1b\u6570\u503c\u8fc7\u9ad8\u4f1a\u8ba9\u70b9\u4f4d\u6f02\u5728\u7a7a\u4e2d\u3002")]
        [SerializeField] private Vector3 portraitLocalOffset = new Vector3(0f, -0.15f, 0f);
        [Tooltip("\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d\u751f\u6210\u65f6\u5f3a\u5236\u4f7f\u7528\u7684 Y \u8f74\u504f\u79fb\uff1b\u4f18\u5148\u7ea7\u9ad8\u4e8e\u65e7\u7248\u504f\u79fb\u5b57\u6bb5\uff0c\u7528\u4e8e\u4fdd\u8bc1\u70b9\u4f4d\u76f8\u5bf9\u5efa\u7b51\u9876\u90e8\u4e0b\u79fb 0.4\u3002")]
        [SerializeField] private float portraitBoundsYOffset = -0.4f;
        [Tooltip("\u517c\u5bb9\u65e7\u6570\u636e\u7684\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d\u7f29\u653e\u5b57\u6bb5\uff1b\u5f53\u524d\u8fd0\u884c\u65f6\u4e0d\u518d\u7f29\u653e\u3002")]
        [SerializeField] private Vector3 portraitLocalScale = Vector3.one;
        [Tooltip("\u517c\u5bb9\u65e7\u6570\u636e\u7684\u989d\u5916\u538b\u7f29\u500d\u7387\uff1b\u5f53\u524d\u8fd0\u884c\u65f6\u4e0d\u518d\u4f7f\u7528\u3002")]
        [SerializeField, Range(0.01f, 1f)] private float portraitMapScaleMultiplier = 1f;
        [Tooltip("\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d\u6e32\u67d3\u6392\u5e8f\uff1b\u6570\u503c\u8d8a\u5927\u8d8a\u9760\u524d\u3002")]
        [SerializeField] private int portraitSortingOrder = 50;
        [Tooltip("\u542f\u7528\u540e\uff0c\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d\u4f1a\u5728\u6bcf\u5e27\u9762\u5411\u5f53\u524d\u4e3b\u6444\u50cf\u673a\u3002")]
        [SerializeField] private bool portraitFacesCamera = true;

        [Header("\u9f20\u6807\u60ac\u505c\u63cf\u8fb9\uff1a\u70b9\u4f4d\u81ea\u8eab\u81ea\u52a8\u8865\u9f50\u4f9d\u8d56")]
        [Tooltip("\u9f20\u6807\u79fb\u5230\u70b9\u4f4d\u4e0a\u65f6\u53c2\u4e0e\u9ad8\u4eae\u7684 Renderer\uff1b\u4e3a\u7a7a\u65f6\u81ea\u52a8\u8bfb\u53d6\u5f53\u524d\u7269\u4f53\u53ca\u5b50\u7269\u4f53 Renderer\u3002")]
        [SerializeField] private Renderer[] hoverOutlineRenderers;
        [Tooltip("\u70b9\u4f4d\u63a5\u6536\u9f20\u6807\u60ac\u505c\u548c\u70b9\u51fb\u7684 Collider\uff1b\u4e3a\u7a7a\u65f6\u81ea\u52a8\u6dfb\u52a0\u3002")]
        [SerializeField] private Collider hoverCollider;
        [Tooltip("\u70b9\u4f4d\u60ac\u505c\u63cf\u8fb9\u7ec4\u4ef6\uff1b\u4e3a\u7a7a\u65f6\u8fd0\u884c\u65f6\u81ea\u52a8\u6dfb\u52a0 CityBuildingOutlineEffect\u3002")]
        [SerializeField] private CityBuildingOutlineEffect hoverOutlineEffect;
        [Tooltip("\u70b9\u4f4d\u9f20\u6807\u60ac\u505c\u63cf\u8fb9\u989c\u8272\uff1b\u53ea\u5f71\u54cd\u5916\u8f6e\u5ed3\u3002")]
        [SerializeField] private Color hoverOutlineColor = new Color(1f, 0.78f, 0.18f, 1f);
        [Tooltip("\u70b9\u4f4d\u9f20\u6807\u60ac\u505c\u63cf\u8fb9\u5bbd\u5ea6\uff1b\u6570\u503c\u8d8a\u5927\u8f6e\u5ed3\u8d8a\u7c97\u3002")]
        [SerializeField] private int hoverOutlinePixelWidth = 3;
        [Tooltip("\u542f\u7528\u540e\uff0c\u8fd0\u884c\u65f6\u81ea\u52a8\u67e5\u627e Renderer\u3001\u6dfb\u52a0 Collider\u3001\u6dfb\u52a0 CityBuildingOutlineEffect\u3002")]
        [SerializeField] private bool autoBindHoverOutlineDependencies = true;
        [Tooltip("\u542f\u7528\u540e\uff0c\u81ea\u52a8\u6dfb\u52a0\u7684 BoxCollider \u4f1a\u6309\u70b9\u4f4d Renderer \u5305\u56f4\u76d2\u9002\u914d\u3002")]
        [SerializeField] private bool autoFitHoverColliderToRenderers = true;

        [Header("\u57ce\u533a\u4ea4\u4e92\u5f00\u5173\uff1a\u8fdb\u5165\u57ce\u533a\u524d\u548c\u5267\u60c5\u64ad\u653e\u4e2d\u7981\u7528")]
        [Tooltip("\u542f\u7528\u540e\uff0c\u53ea\u6709 GameEntry \u5df2\u5207\u6362\u5230\u57ce\u533a\u754c\u9762\u65f6\uff0c\u70b9\u4f4d\u624d\u54cd\u5e94\u70b9\u51fb\u548c\u60ac\u505c\u3002")]
        [SerializeField] private bool requireCityRootActive = true;
        [Tooltip("\u6e38\u620f\u5165\u53e3\u5bf9\u8c61\uff1b\u7528\u4e8e\u786e\u8ba4\u662f\u5426\u5df2\u7ecf\u901a\u8fc7\u8fdb\u5165\u57ce\u533a\u6309\u94ae\u5207\u6362\u5230 CityRoot\u3002\u4e3a\u7a7a\u65f6\u8fd0\u884c\u65f6\u81ea\u52a8\u67e5\u627e\u3002")]
        [SerializeField] private GameEntry gameEntry;
        [Tooltip("\u5267\u60c5\u670d\u52a1\uff1b\u7528\u4e8e\u5728\u5267\u60c5\u9762\u677f\u64ad\u653e\u671f\u95f4\u4e34\u65f6\u7981\u7528\u5efa\u7b51\u70b9\u4f4d\u70b9\u51fb\u548c\u60ac\u505c\uff0c\u907f\u514d\u70b9\u7a7f\u5230\u5e95\u5c42\u5efa\u7b51\u3002\u4e3a\u7a7a\u65f6\u8fd0\u884c\u65f6\u81ea\u52a8\u67e5\u627e\u3002")]
        [SerializeField] private StoryService storyService;
        [Tooltip("运行时只读：当前点位是否允许交互，用于 Inspector 排查。")]
        [SerializeField] private bool inspectorCityInteractionEnabled;

        [Header("运行时只读快照：配置匹配结果")]
        [Tooltip("当前点位是否已经匹配到 CityPointConfig 中的配置行。")]
        [SerializeField] private bool inspectorIsMatched;
        [Tooltip("匹配到的点位中文名；来自 CityPointConfig.PointName。")]
        [SerializeField] private string inspectorPointName;
        [Tooltip("匹配到的所属城区；来自 CityPointConfig.AreaId。")]
        [SerializeField] private string inspectorAreaId;
        [Tooltip("匹配到的点位类型；来自 CityPointConfig.PointType。")]
        [SerializeField] private string inspectorPointType;
        [Tooltip("匹配到的配置说明；来自 CityPointConfig.Description。")]
        [SerializeField] private string inspectorDescription;
        [Tooltip("匹配摘要；用于在 Inspector 中快速确认当前 GameObject 与配置表的对应关系。")]
        [SerializeField] private string inspectorMatchSummary;

        [Header("运行时只读快照：当前点位支线事件")]
        [Tooltip("当前绑定到该点位的支线事件 ID；来自 SideEventConfig.SideEventId。为空表示当前回合该点位没有支线事件。")]
        [SerializeField] private string inspectorActiveSideEventId;
        [Tooltip("当前点位支线事件点击后播放的剧情 ID；来自 SideEventConfig.StoryId。")]
        [SerializeField] private string inspectorActiveSideEventStoryId;
        [Tooltip("当前点位支线事件的显示角色 ID；来自 SideEventConfig.DisplayCharacterId，用于建筑人物点位显示。")]
        [SerializeField] private string inspectorActiveSideEventCharacterId;
        [Tooltip("最近一次点击该点位支线事件的结果；用于在 Inspector 中确认是否成功触发剧情。")]
        [SerializeField] private string inspectorActiveSideEventClickResult;

        private CityPointDefinition definition;
        private SpriteRenderer portraitRenderer;
        private Camera portraitCamera;
        private SideEventDefinition activeSideEventDefinition;
        private CitySideEventService activeSideEventService;
        private bool isHoverOutlineVisible;
        private static bool cityPortraitsVisible;

        public string PointId => pointId;
        public bool IsMatched => definition != null;
        public CityPointDefinition Definition => definition;
        public string ActiveSideEventId => activeSideEventDefinition != null ? activeSideEventDefinition.SideEventId : string.Empty;
        public bool HasActiveSideEvent => activeSideEventDefinition != null;
        public bool IsHoverOutlineVisible => isHoverOutlineVisible;
        public bool IsCityInteractionCurrentlyEnabled => IsCityInteractionEnabled();
        public bool IsHoverOutlineRuntimeReady =>
            hoverOutlineEffect != null &&
            hoverCollider != null &&
            hoverOutlineRenderers != null &&
            hoverOutlineRenderers.Any(renderer => renderer != null);

        private void Awake()
        {
            RemoveDuplicateComponents();
            InitializeRuntimeHoverDependencies();
            RefreshPortraitDisplay();
        }

        private void OnEnable()
        {
            RemoveDuplicateComponents();
            InitializeRuntimeHoverDependencies();
            RefreshPortraitDisplay();
        }

        private void OnDisable()
        {
            ApplyHoverOutline(false);
            if (portraitRenderer != null)
            {
                portraitRenderer.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            FacePortraitToCamera();
        }

        public void InitializeRuntimeHoverDependenciesForTest()
        {
            InitializeRuntimeHoverDependencies();
        }

        public void Configure(string newPointId)
        {
            pointId = newPointId ?? string.Empty;
            ClearBinding();
        }

        public void Bind(CityPointDefinition cityPointDefinition)
        {
            definition = cityPointDefinition;
            inspectorIsMatched = definition != null;
            inspectorPointName = definition != null ? definition.PointName : string.Empty;
            inspectorAreaId = definition != null ? definition.AreaId : string.Empty;
            inspectorPointType = definition != null ? definition.PointType : string.Empty;
            inspectorDescription = definition != null ? definition.Description : string.Empty;
            inspectorMatchSummary = definition != null
                ? $"PointId={definition.PointId}, 名称={definition.PointName}, 城区={definition.AreaId}, 类型={definition.PointType}"
                : $"PointId={pointId} 未匹配到 CityPointConfig";
            RefreshPortraitDisplay();
        }

        public void ClearBinding()
        {
            definition = null;
            inspectorIsMatched = false;
            inspectorPointName = string.Empty;
            inspectorAreaId = string.Empty;
            inspectorPointType = string.Empty;
            inspectorDescription = string.Empty;
            inspectorMatchSummary = string.IsNullOrEmpty(pointId)
                ? "PointId 为空，无法匹配 CityPointConfig"
                : $"PointId={pointId} 尚未绑定";
            RefreshPortraitDisplay();
        }

        public void BindSideEvent(SideEventDefinition sideEventDefinition, CitySideEventService sideEventService)
        {
            activeSideEventDefinition = sideEventDefinition;
            activeSideEventService = sideEventService;
            inspectorActiveSideEventId = sideEventDefinition != null ? sideEventDefinition.SideEventId : string.Empty;
            inspectorActiveSideEventStoryId = sideEventDefinition != null ? sideEventDefinition.StoryId : string.Empty;
            inspectorActiveSideEventCharacterId = sideEventDefinition != null ? sideEventDefinition.DisplayCharacterId : string.Empty;
            inspectorActiveSideEventClickResult = string.Empty;
            InitializeRuntimeHoverDependencies();
            ApplyHoverOutline(false);
            RefreshPortraitDisplay();
        }

        public void ClearSideEventBinding()
        {
            activeSideEventDefinition = null;
            activeSideEventService = null;
            inspectorActiveSideEventId = string.Empty;
            inspectorActiveSideEventStoryId = string.Empty;
            inspectorActiveSideEventCharacterId = string.Empty;
            inspectorActiveSideEventClickResult = string.Empty;
            ApplyHoverOutline(false);
            RefreshPortraitDisplay();
        }

        public bool TryTriggerBoundSideEvent()
        {
            if (!IsCityInteractionEnabled())
            {
                inspectorActiveSideEventClickResult = "尚未进入城区，点位点击被禁用。";
                return false;
            }

            if (activeSideEventDefinition == null || activeSideEventService == null)
            {
                inspectorActiveSideEventClickResult = "当前点位没有可触发的支线事件。";
                HintPanelView.ShowNoSideEventHint();
                return false;
            }

            var started = activeSideEventService.TryStartSideEvent(activeSideEventDefinition.SideEventId, out var resultMessage);
            inspectorActiveSideEventClickResult = resultMessage;
            if (started)
            {
                ClearSideEventBinding();
            }

            return started;
        }
        public void RefreshPortraitDisplay()
        {
            SetPortraitVisible(cityPortraitsVisible || HasActiveSideEvent);
        }

        public static void SetAllPortraitsVisible(bool visible)
        {
            cityPortraitsVisible = visible;
            var pointViews = FindObjectsByType<CityPointView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var pointView in pointViews)
            {
                if (pointView != null)
                {
                    pointView.SetPortraitVisible(visible || pointView.HasActiveSideEvent);
                }
            }
        }

        private void SetPortraitVisible(bool visible)
        {
            if (!visible || (!showPortrait && !HasActiveSideEvent))
            {
                if (portraitRenderer == null)
                {
                    portraitRenderer = FindExistingPortraitRenderer();
                }

                if (portraitRenderer != null)
                {
                    portraitRenderer.gameObject.SetActive(false);
                }

                return;
            }

            var renderer = EnsurePortraitRenderer();
            if (renderer == null)
            {
                return;
            }

            var portraitId = HasActiveSideEvent && !string.IsNullOrEmpty(activeSideEventDefinition.DisplayCharacterId)
                ? activeSideEventDefinition.DisplayCharacterId
                : portraitResourceId;
            renderer.gameObject.SetActive(true);
            renderer.sprite = CharacterPlaceholderPortraitProvider.LoadPortrait(portraitId);
            renderer.enabled = renderer.sprite != null;
            renderer.sortingOrder = portraitSortingOrder;
            var portraitRoot = GetPortraitRootTransform(renderer);
            portraitRoot.localPosition = GetLocalPositionAboveBounds(GetPortraitPlacementOffset());
            portraitRoot.localScale = Vector3.one;
            portraitRoot.localRotation = Quaternion.identity;
            FacePortraitToCamera();
        }

        private Vector3 GetPortraitPlacementOffset()
        {
            return new Vector3(portraitLocalOffset.x, portraitBoundsYOffset, portraitLocalOffset.z);
        }

        public Vector3 GetWorldPositionAboveBounds(Vector3 localOffset)
        {
            return transform.TransformPoint(GetLocalPositionAboveBounds(localOffset));
        }

        private Vector3 GetLocalPositionAboveBounds(Vector3 localOffset)
        {
            if (!TryGetHoverRendererWorldBounds(out var worldBounds))
            {
                return localOffset;
            }

            var worldTopCenter = new Vector3(worldBounds.center.x, worldBounds.max.y, worldBounds.center.z);
            return transform.InverseTransformPoint(worldTopCenter) + localOffset;
        }

        private bool TryGetHoverRendererWorldBounds(out Bounds worldBounds)
        {
            CacheHoverRenderers();
            var hasBounds = false;
            worldBounds = new Bounds(transform.position, Vector3.zero);
            if (hoverOutlineRenderers == null)
            {
                return false;
            }

            foreach (var renderer in hoverOutlineRenderers)
            {
                if (renderer == null || IsPortraitRenderer(renderer))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    worldBounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                worldBounds.Encapsulate(renderer.bounds);
            }

            return hasBounds;
        }

        private bool IsPortraitRenderer(Renderer renderer)
        {
            return renderer != null &&
                (renderer.transform == portraitRenderer?.transform ||
                 renderer.transform.name == "建筑人物点位" ||
                 renderer.transform.name == "点位人物立绘");
        }

        private SpriteRenderer FindExistingPortraitRenderer()
        {
            var namedPortraitTransform = transform.Find("建筑人物点位") ?? transform.Find("点位人物立绘");
            if (namedPortraitTransform != null && namedPortraitTransform.TryGetComponent<SpriteRenderer>(out var namedRenderer))
            {
                return namedRenderer;
            }

            return GetComponentsInChildren<SpriteRenderer>(true)
                .FirstOrDefault(renderer => renderer != null && renderer.transform != transform);
        }

        private SpriteRenderer EnsurePortraitRenderer()
        {
            if (portraitRenderer != null)
            {
                return portraitRenderer;
            }

            var portraitTransform = transform.Find("建筑人物点位") ?? transform.Find("点位人物立绘");
            if (portraitTransform == null)
            {
                var prefab = string.IsNullOrWhiteSpace(portraitPrefabResourcePath)
                    ? null
                    : Resources.Load<GameObject>(portraitPrefabResourcePath);
                var portraitObject = prefab != null
                    ? Instantiate(prefab, transform, false)
                    : new GameObject("建筑人物点位");
                portraitObject.name = "建筑人物点位";
                portraitTransform = portraitObject.transform;
                if (portraitTransform.parent != transform)
                {
                    portraitTransform.SetParent(transform, false);
                }
            }

            portraitRenderer = portraitTransform.GetComponentInChildren<SpriteRenderer>(true);
            if (portraitRenderer == null)
            {
                portraitRenderer = portraitTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            return portraitRenderer;
        }

        private Transform GetPortraitRootTransform(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return transform;
            }

            var directChild = renderer.transform;
            while (directChild.parent != null && directChild.parent != transform)
            {
                directChild = directChild.parent;
            }

            return directChild.parent == transform ? directChild : renderer.transform;
        }

        private void FacePortraitToCamera()
        {
            if (!portraitFacesCamera || portraitRenderer == null || !portraitRenderer.gameObject.activeInHierarchy)
            {
                return;
            }

            if (portraitCamera == null)
            {
                portraitCamera = Camera.main;
            }

            if (portraitCamera == null)
            {
                return;
            }

            var portraitRoot = GetPortraitRootTransform(portraitRenderer);
            var direction = portraitRoot.position - portraitCamera.transform.position;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            portraitRoot.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void OnMouseEnter()
        {
            if (IsCityInteractionEnabled())
            {
                ApplyHoverOutline(true);
                return;
            }

            ApplyHoverOutline(false);
        }

        private void OnMouseExit()
        {
            ApplyHoverOutline(false);
        }

        private void OnMouseDown()
        {
            TryTriggerBoundSideEvent();
        }

        private void InitializeRuntimeHoverDependencies()
        {
            if (!autoBindHoverOutlineDependencies)
            {
                return;
            }

            CacheHoverRenderers();
            EnsureHoverOutlineEffect();
            EnsureHoverCollider();
        }

        private void CacheHoverRenderers()
        {
            if (hoverOutlineRenderers != null && hoverOutlineRenderers.Any(renderer => renderer != null))
            {
                return;
            }

            hoverOutlineRenderers = GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer != null && renderer.GetComponent<TMPro.TMP_Text>() == null && !IsPortraitRenderer(renderer))
                .ToArray();
        }

        private void EnsureHoverOutlineEffect()
        {
            if (hoverOutlineEffect == null)
            {
                hoverOutlineEffect = GetComponent<CityBuildingOutlineEffect>() ?? gameObject.AddComponent<CityBuildingOutlineEffect>();
            }
        }

        private void EnsureHoverCollider()
        {
            if (hoverCollider == null)
            {
                hoverCollider = GetComponent<Collider>();
            }

            if (hoverCollider == null)
            {
                hoverCollider = gameObject.AddComponent<BoxCollider>();
            }

            hoverCollider.enabled = true;
            if (autoFitHoverColliderToRenderers && hoverCollider is BoxCollider boxCollider)
            {
                FitBoxColliderToHoverRenderers(boxCollider);
            }
        }

        private void FitBoxColliderToHoverRenderers(BoxCollider boxCollider)
        {
            if (boxCollider == null || hoverOutlineRenderers == null || hoverOutlineRenderers.Length == 0)
            {
                return;
            }

            var hasBounds = false;
            var worldBounds = new Bounds(transform.position, Vector3.zero);
            foreach (var renderer in hoverOutlineRenderers)
            {
                if (renderer == null || IsPortraitRenderer(renderer))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    worldBounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                worldBounds.Encapsulate(renderer.bounds);
            }

            if (!hasBounds)
            {
                return;
            }

            boxCollider.center = transform.InverseTransformPoint(worldBounds.center);
            boxCollider.size = new Vector3(
                Mathf.Max(0.01f, worldBounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x))),
                Mathf.Max(0.01f, worldBounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y))),
                Mathf.Max(0.01f, worldBounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.z))));
        }

        private void ApplyHoverOutline(bool visible)
        {
            if (!visible || !enabled || !gameObject.activeInHierarchy || !IsCityInteractionEnabled())
            {
                isHoverOutlineVisible = false;
                hoverOutlineEffect?.SetVisible(false);
                return;
            }

            InitializeRuntimeHoverDependencies();
            if (hoverOutlineEffect == null || hoverOutlineRenderers == null || !hoverOutlineRenderers.Any(renderer => renderer != null))
            {
                isHoverOutlineVisible = false;
                return;
            }

            hoverOutlineEffect.Configure(hoverOutlineRenderers, hoverOutlineColor, hoverOutlinePixelWidth);
            hoverOutlineEffect.SetVisible(true);
            isHoverOutlineVisible = true;
        }

        private bool IsCityInteractionEnabled()
        {
            if (IsStoryBlockingCityInteraction())
            {
                inspectorCityInteractionEnabled = false;
                return false;
            }

            if (!requireCityRootActive)
            {
                inspectorCityInteractionEnabled = true;
                return true;
            }

            ResolveGameEntry();
            if (gameEntry != null && gameEntry.CityRoot != null)
            {
                var isCityVisible = gameEntry.CityRoot.activeInHierarchy;
                var isDeskHidden = gameEntry.DeskRoot == null || !gameEntry.DeskRoot.activeInHierarchy;
                inspectorCityInteractionEnabled = isCityVisible && isDeskHidden;
                return inspectorCityInteractionEnabled;
            }

            inspectorCityInteractionEnabled = IsCityHudPanelVisible();
            return inspectorCityInteractionEnabled;
        }

        private bool IsStoryBlockingCityInteraction()
        {
            ResolveStoryService();
            if (storyService != null && storyService.CurrentPlayback != null)
            {
                return true;
            }

            return IsStoryPanelVisible();
        }

        private static bool IsCityHudPanelVisible()
        {
            var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in transforms)
            {
                if (candidate != null && candidate.name == "城区HUD面板")
                {
                    return candidate.gameObject.activeInHierarchy;
                }
            }

            return false;
        }

        private static bool IsStoryPanelVisible()
        {
            var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in transforms)
            {
                if (candidate != null && candidate.name == "\u5267\u60c5\u9762\u677f")
                {
                    if (!candidate.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    var canvasGroup = candidate.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        return canvasGroup.alpha > 0.01f &&
                               (canvasGroup.blocksRaycasts || canvasGroup.interactable);
                    }

                    var visibleGraphic = candidate.GetComponentsInChildren<Graphic>(true)
                        .Any(graphic =>
                            graphic != null &&
                            graphic.gameObject.activeInHierarchy &&
                            graphic.enabled &&
                            graphic.canvasRenderer.GetAlpha() > 0.01f);
                    return visibleGraphic;
                }
            }

            return false;
        }

        private void ResolveGameEntry()
        {
            if (gameEntry == null)
            {
                gameEntry = FindFirstObjectByType<GameEntry>(FindObjectsInactive.Include);
            }
        }

        private void ResolveStoryService()
        {
            if (storyService == null)
            {
                storyService = FindFirstObjectByType<StoryService>(FindObjectsInactive.Include);
            }
        }

        private void RemoveDuplicateComponents()
        {
            var pointViews = GetComponents<CityPointView>();
            if (pointViews.Length <= 1 || pointViews[0] == this)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(this);
            }
            else
            {
                DestroyImmediate(this);
            }
        }

        private void OnValidate()
        {
            RemoveDuplicateComponents();
            gizmoRadius = Mathf.Max(0f, gizmoRadius);
            portraitLocalScale = new Vector3(
                Mathf.Max(0f, portraitLocalScale.x),
                Mathf.Max(0f, portraitLocalScale.y),
                Mathf.Max(0f, portraitLocalScale.z));
            portraitMapScaleMultiplier = Mathf.Clamp(portraitMapScaleMultiplier, 0.01f, 1f);
            hoverOutlinePixelWidth = Mathf.Max(1, hoverOutlinePixelWidth);
        }

        private void OnDrawGizmos()
        {
            if (!drawSceneGizmo)
            {
                return;
            }

            Gizmos.color = inspectorIsMatched ? matchedGizmoColor : unmatchedGizmoColor;
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, gizmoRadius));
        }
    }
}
