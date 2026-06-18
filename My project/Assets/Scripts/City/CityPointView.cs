using TwelveMoons.UI;
using UnityEngine;

namespace TwelveMoons.City
{
    public sealed class CityPointView : MonoBehaviour
    {
        [Header("点位配置匹配：对应 CityPointConfig.PointId")]
        [Tooltip("城区点位 ID；必须与 CityPointConfig 表中的 PointId 完全一致，注册器会用它匹配配置。")]
        [SerializeField] private string pointId;

        [Tooltip("是否在 Scene 视图绘制点位标记；只用于编辑器观察，不触发建筑、剧情或资源逻辑。")]
        [SerializeField] private bool drawSceneGizmo = true;

        [Tooltip("Scene 视图点位标记半径；仅用于观察点位位置，不能填负数。")]
        [SerializeField] private float gizmoRadius = 0.25f;

        [Tooltip("Scene 视图点位标记颜色；用于区分已经匹配配置的点位。")]
        [SerializeField] private Color matchedGizmoColor = new Color(0.25f, 0.85f, 0.55f, 0.9f);

        [Tooltip("Scene 视图点位标记颜色；用于提示 PointId 未匹配到配置。")]
        [SerializeField] private Color unmatchedGizmoColor = new Color(1f, 0.35f, 0.2f, 0.9f);

        [Header("城区人物立绘显示：当前阶段先全部显示")]
        [Tooltip("是否在该城区点位下显示人物立绘；当前阶段先全部开启，后续会改为读取配置表。")]
        [SerializeField] private bool showPortrait = true;

        [Tooltip("当前阶段使用的临时人物立绘资源路径；留空时使用默认角色立绘。")]
        [SerializeField] private string portraitResourceId = "Art/Art/Character/IMG_0940";

        [Tooltip("点位人物立绘相对 CityPointView 的本地偏移；只移动运行时补充的立绘子物体。")]
        [SerializeField] private Vector3 portraitLocalOffset = new Vector3(0f, 1.2f, 0f);

        [Tooltip("点位人物立绘本地缩放；只影响运行时补充的立绘子物体。")]
        [SerializeField] private Vector3 portraitLocalScale = new Vector3(0.058333f, 0.058333f, 0.058333f);
        [Tooltip("城区地图上额外压缩人物立绘的倍率；用于覆盖场景中已经序列化的旧尺寸。")]
        [SerializeField, Range(0.01f, 1f)] private float portraitMapScaleMultiplier = 0.333333f;

        [Tooltip("点位人物立绘渲染排序；数值越大越靠前，避免被城区背景遮住。")]
        [SerializeField] private int portraitSortingOrder = 50;
        [Tooltip("启用后，城区点位人物立绘会在每帧面向当前主摄像机；用于保持向日葵视角。")]
        [SerializeField] private bool portraitFacesCamera = true;

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

        private CityPointDefinition definition;
        private SpriteRenderer portraitRenderer;
        private Camera portraitCamera;
        private static bool cityPortraitsVisible;

        public string PointId => pointId;

        public bool IsMatched => definition != null;

        public CityPointDefinition Definition => definition;

        private void Awake()
        {
            SetPortraitVisible(cityPortraitsVisible);
        }

        private void OnEnable()
        {
            SetPortraitVisible(cityPortraitsVisible);
        }

        private void OnDisable()
        {
            if (portraitRenderer != null)
            {
                portraitRenderer.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            FacePortraitToCamera();
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

        public void RefreshPortraitDisplay()
        {
            SetPortraitVisible(cityPortraitsVisible);
        }

        public static void SetAllPortraitsVisible(bool visible)
        {
            cityPortraitsVisible = visible;
            var pointViews = FindObjectsByType<CityPointView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var pointView in pointViews)
            {
                if (pointView != null)
                {
                    pointView.SetPortraitVisible(visible);
                }
            }
        }

        private void SetPortraitVisible(bool visible)
        {
            if (!visible || !showPortrait)
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

            renderer.gameObject.SetActive(true);
            renderer.sprite = CharacterPlaceholderPortraitProvider.LoadPortrait(portraitResourceId);
            renderer.enabled = renderer.sprite != null;
            renderer.sortingOrder = portraitSortingOrder;
            renderer.transform.localPosition = portraitLocalOffset;
            renderer.transform.localScale = portraitLocalScale * portraitMapScaleMultiplier;
            renderer.transform.localRotation = Quaternion.identity;
            FacePortraitToCamera();
        }

        private SpriteRenderer FindExistingPortraitRenderer()
        {
            var namedPortraitTransform = transform.Find("点位人物立绘");
            if (namedPortraitTransform != null &&
                namedPortraitTransform.TryGetComponent<SpriteRenderer>(out var namedRenderer))
            {
                return namedRenderer;
            }

            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.transform != transform)
                {
                    return renderer;
                }
            }

            return null;
        }

        private void FacePortraitToCamera()
        {
            if (!portraitFacesCamera ||
                portraitRenderer == null ||
                !portraitRenderer.gameObject.activeInHierarchy)
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

            var direction = portraitRenderer.transform.position - portraitCamera.transform.position;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            portraitRenderer.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private SpriteRenderer EnsurePortraitRenderer()
        {
            if (portraitRenderer != null)
            {
                return portraitRenderer;
            }

            var portraitTransform = transform.Find("点位人物立绘");
            if (portraitTransform == null)
            {
                var portraitObject = new GameObject("点位人物立绘");
                portraitTransform = portraitObject.transform;
                portraitTransform.SetParent(transform, false);
            }

            portraitRenderer = portraitTransform.GetComponent<SpriteRenderer>();
            if (portraitRenderer == null)
            {
                portraitRenderer = portraitTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            return portraitRenderer;
        }

        private void OnValidate()
        {
            if (gizmoRadius < 0f)
            {
                gizmoRadius = 0f;
            }

            portraitLocalScale = new Vector3(
                Mathf.Max(0f, portraitLocalScale.x),
                Mathf.Max(0f, portraitLocalScale.y),
                Mathf.Max(0f, portraitLocalScale.z));
            portraitMapScaleMultiplier = Mathf.Clamp(portraitMapScaleMultiplier, 0.01f, 1f);
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
