using TMPro;
using System;
using System.Linq;
using TwelveMoons.Core;
using UnityEngine;

namespace TwelveMoons.City
{
    public sealed class CityBuildingView : MonoBehaviour
    {
        [Header("建筑配置匹配：对应 CityBuildingConfig.BuildingId")]
        [Tooltip("建筑 ID；必须与 CityBuildingConfig 表中的 BuildingId 完全一致，用于匹配配置和运行时解锁状态。")]
        [SerializeField] private string buildingId;
        [Tooltip("建筑所在点位 ID；建议与 CityBuildingConfig.PointId、CityPointConfig.PointId 保持一致，方便检查建筑和城区点位关系。")]
        [SerializeField] private string pointId;

        [Header("显示与点击：绑定实际 3D 模型")]
        [Tooltip("建筑显示根物体；填写实际建筑模型子物体或包住模型的父物体。留空时使用当前物体。")]
        [SerializeField] private GameObject visualRoot;
        [Tooltip("可点击碰撞体；留空时自动使用当前物体或子物体上的 Collider。已解锁建筑会保持 Collider 开启，以便领取后仍可悬停显示轮廓。")]
        [SerializeField] private Collider clickableCollider;
        [Tooltip("启用后，建筑在可领取时可通过鼠标点击领取产出；领取后仍可悬停高亮，但不会重复领取。")]
        [SerializeField] private bool allowMouseClick = true;
        [Tooltip("启用后，建筑未解锁时会隐藏 Renderer；自动绑定既有地图模型时应关闭，避免误隐藏城市场景。")]
        [SerializeField] private bool controlVisualVisibilityByUnlock = true;
        [Tooltip("启用后，只有 GameEntry 已切换到城区界面时，建筑才响应点击和悬停。")]
        [SerializeField] private bool requireCityRootActive = true;
        [Tooltip("游戏入口对象；用于确认是否已经通过进入城区按钮切换到 CityRoot。留空时运行时自动查找。")]
        [SerializeField] private GameEntry gameEntry;

        [Header("领取提示：建筑上方红色感叹号和领取结果")]
        [Tooltip("未领取时显示的红色感叹号根物体；留空时运行时会在建筑下自动创建 3D TMP 感叹号。")]
        [SerializeField] private Transform collectHintRoot;
        [Tooltip("未领取时显示的红色感叹号文本；留空时运行时会自动创建 TextMeshPro 文本。")]
        [SerializeField] private TMP_Text collectHintText;
        [Tooltip("点击领取后显示的结果文本；留空时运行时会自动创建 TextMeshPro 文本。")]
        [SerializeField] private TMP_Text collectResultText;
        [Tooltip("提示文字相对建筑坐标的偏移；用于把感叹号和结果文字放在建筑上方。")]
        [SerializeField] private Vector3 indicatorLocalOffset = new Vector3(0f, 2.2f, 0f);
        [Tooltip("红色感叹号上下浮动的幅度。")]
        [SerializeField] private float collectHintBobDistance = 0.16f;
        [Tooltip("红色感叹号上下浮动的速度。")]
        [SerializeField] private float collectHintBobSpeed = 2.4f;
        [Tooltip("领取结果文字显示的秒数；时间结束后自动隐藏。")]
        [SerializeField] private float resultVisibleSeconds = 2.2f;
        [Tooltip("用于 Billboard 朝向的摄像机；留空时自动使用 Main Camera。")]
        [SerializeField] private Camera billboardCamera;

        [Header("鼠标轮廓高亮：悬停时加粗建筑外圈")]
        [Tooltip("鼠标移上已解锁建筑时参与轮廓高亮的 Renderer；留空时自动使用 VisualRoot 下所有 Renderer。")]
        [SerializeField] private Renderer[] highlightRenderers;
        [Tooltip("轮廓高亮组件；留空时运行时自动挂到当前建筑物体。")]
        [SerializeField] private CityBuildingOutlineEffect outlineEffect;
        [Tooltip("轮廓高亮颜色；只影响外圈描边，不改变建筑原材质颜色。")]
        [SerializeField] private Color hoverOutlineColor = new Color(1f, 0.78f, 0.18f, 1f);
        [Tooltip("轮廓高亮像素宽度；数值越大，鼠标移上去后屏幕空间外圈越粗。")]
        [SerializeField] private int hoverOutlinePixelWidth = 3;
        [Header("悬停描边自动绑定：运行时补齐缺失组件")]
        [Tooltip("启用后，运行时会自动查找建筑 Renderer、添加同物体 Collider、添加 CityBuildingOutlineEffect，避免手动漏拖引用。")]
        [SerializeField] private bool autoBindHoverOutlineDependencies = true;
        [Tooltip("启用后，未填写 BuildingId 或未绑定 CityBuildingConfig 的建筑也可以显示鼠标悬停描边；不会影响正式领取逻辑。")]
        [SerializeField] private bool allowHoverOutlineWhenUnbound = true;
        [Tooltip("启用后，自动添加的 BoxCollider 会按建筑 Renderer 的整体包围盒适配，保证鼠标能点到整栋建筑。")]
        [SerializeField] private bool autoFitHoverColliderToRenderers = true;

        [Header("运行时只读快照：建筑显示与领取状态")]
        [Tooltip("当前建筑是否已经匹配到 CityBuildingConfig 中的配置行。")]
        [SerializeField] private bool inspectorIsMatched;
        [Tooltip("匹配到的建筑中文名；来自 CityBuildingConfig.BuildingName。")]
        [SerializeField] private string inspectorBuildingName;
        [Tooltip("匹配到的所属城区；来自 CityBuildingConfig.CityAreaId。")]
        [SerializeField] private string inspectorCityAreaId;
        [Tooltip("匹配到的点位 ID；来自 CityBuildingConfig.PointId，用于确认与本组件填写的 PointId 是否一致。")]
        [SerializeField] private string inspectorConfigPointId;
        [Tooltip("匹配到的建筑效果类型；Resource 表示产出资源或道具，Suspicion 表示降低阵营质疑度。")]
        [SerializeField] private string inspectorEffectType;
        [Tooltip("当前运行时是否已经解锁；未解锁时建筑模型隐藏且不可点击。")]
        [SerializeField] private bool inspectorIsUnlocked;
        [Tooltip("当前回合是否可以点击领取；同回合重复点击或冷却未结束时为 false。")]
        [SerializeField] private bool inspectorCanCollect;
        [Tooltip("建筑配置和运行状态摘要；用于在 Inspector 中快速确认绑定是否正确。")]
        [SerializeField] private string inspectorSummary;
        [Tooltip("悬停描边自动绑定诊断：显示 Renderer、Collider、OutlineEffect 是否已经由运行时代码补齐。")]
        [SerializeField] private string inspectorHoverOutlineBindingSnapshot;

        private CityBuildingDefinition definition;
        private CityBuildingService service;
        private Renderer[] cachedRenderers;
        private Collider[] cachedColliders;
        private bool isCollectHintVisible;
        private float collectResultHideAt = -1f;

        public string BuildingId => buildingId;

        public string PointId => pointId;

        public bool IsMatched => definition != null;

        public bool IsHoverOutlineRuntimeReady =>
            outlineEffect != null &&
            clickableCollider != null &&
            highlightRenderers != null &&
            highlightRenderers.Any(renderer => renderer != null);

        private void Awake()
        {
            InitializeRuntimeHoverDependencies();
        }

        private void OnEnable()
        {
            InitializeRuntimeHoverDependencies();
        }

        public void InitializeRuntimeHoverDependenciesForTest()
        {
            InitializeRuntimeHoverDependencies();
        }

        public void Configure(string newBuildingId, string newPointId)
        {
            buildingId = newBuildingId ?? string.Empty;
            pointId = newPointId ?? string.Empty;
            ClearBinding();
        }

        public void ConfigureRuntimeBinding(string newBuildingId, string newPointId, bool controlVisualVisibility)
        {
            controlVisualVisibilityByUnlock = controlVisualVisibility;
            Configure(newBuildingId, newPointId);
        }

        public void Bind(CityBuildingDefinition buildingDefinition, CityBuildingService buildingService)
        {
            definition = buildingDefinition;
            service = buildingService;
            CacheSceneComponents();
            RefreshState();
        }

        public void ClearBinding()
        {
            definition = null;
            service = null;
            inspectorIsMatched = false;
            inspectorBuildingName = string.Empty;
            inspectorCityAreaId = string.Empty;
            inspectorConfigPointId = string.Empty;
            inspectorEffectType = string.Empty;
            inspectorIsUnlocked = false;
            inspectorCanCollect = false;
            ApplyHoverHighlight(false);
            inspectorSummary = string.IsNullOrEmpty(buildingId)
                ? "BuildingId 为空，无法匹配 CityBuildingConfig。"
                : $"BuildingId={buildingId} 尚未绑定。";
        }

        public void RefreshState()
        {
            inspectorIsMatched = definition != null;
            inspectorBuildingName = definition != null ? definition.BuildingName : string.Empty;
            inspectorCityAreaId = definition != null ? definition.CityAreaId : string.Empty;
            inspectorConfigPointId = definition != null ? definition.PointId : string.Empty;
            inspectorEffectType = definition != null ? definition.BuildingEffectType : string.Empty;
            inspectorIsUnlocked = service != null && (service.IsUnlocked(buildingId) || !controlVisualVisibilityByUnlock);
            inspectorCanCollect = service != null && service.CanCollect(buildingId);

            var pointStatus = definition == null || string.IsNullOrEmpty(pointId) || pointId == definition.PointId
                ? "点位匹配"
                : $"点位不一致：View={pointId}, Config={definition.PointId}";
            inspectorSummary = definition != null
                ? $"BuildingId={definition.BuildingId}, 名称={definition.BuildingName}, 城区={definition.CityAreaId}, {pointStatus}, 已解锁={inspectorIsUnlocked}, 可领取={inspectorCanCollect}"
                : $"BuildingId={buildingId} 未匹配到 CityBuildingConfig。";

            ApplyVisibility(inspectorIsUnlocked);
            RefreshCollectIndicators();
            if (!inspectorIsUnlocked)
            {
                ApplyHoverHighlight(false);
            }
        }

        [ContextMenu("尝试点击当前建筑")]
        public void TryCollectFromInspector()
        {
            TryCollect();
        }

        private void OnMouseDown()
        {
            if (IsCityInteractionEnabled() && allowMouseClick && inspectorCanCollect)
            {
                TryCollect();
            }
        }

        private void OnMouseEnter()
        {
            if (IsCityInteractionEnabled() && CanShowHoverOutline())
            {
                ApplyHoverHighlight(true);
            }
        }

        private void OnMouseExit()
        {
            ApplyHoverHighlight(false);
        }

        private void Update()
        {
            UpdateIndicatorBillboard();
            UpdateHintBob();
            UpdateResultLifetime();
        }

        private void TryCollect()
        {
            if (service == null)
            {
                Debug.LogWarning($"建筑 {buildingId} 缺少 CityBuildingService，无法点击。", this);
                return;
            }

            var collected = service.TryCollect(buildingId, out var resultMessage);
            ShowCollectResult(resultMessage, collected);
            RefreshState();
        }

        private void CacheSceneComponents()
        {
            var root = visualRoot != null ? visualRoot : gameObject;
            cachedRenderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer != null &&
                    !renderer.gameObject.name.EndsWith("_Outline", StringComparison.Ordinal) &&
                    renderer.GetComponent<TMP_Text>() == null)
                .ToArray();
            cachedColliders = root.GetComponentsInChildren<Collider>(true);
            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                highlightRenderers = cachedRenderers;
            }

            EnsureOutlineEffect();
            ResolveGameEntry();

            EnsureSameObjectHoverCollider();
            RefreshHoverOutlineBindingSnapshot();
        }

        private void ApplyVisibility(bool visible)
        {
            CacheSceneComponents();

            var root = visualRoot != null ? visualRoot : gameObject;
            if (visible && root != null && !root.activeSelf)
            {
                root.SetActive(true);
            }

            foreach (var renderer in cachedRenderers)
            {
                if (renderer != null && controlVisualVisibilityByUnlock)
                {
                    renderer.enabled = visible;
                }
            }

            foreach (var collider in cachedColliders)
            {
                if (collider != null)
                {
                    collider.enabled = visible;
                }
            }

            if (clickableCollider != null)
            {
                clickableCollider.enabled = visible;
            }
        }

        private void EnsureCollectIndicators()
        {
            if (collectHintRoot == null)
            {
                var hintObject = new GameObject("CollectHintBillboard");
                hintObject.transform.SetParent(transform, false);
                hintObject.transform.localPosition = indicatorLocalOffset;
                collectHintRoot = hintObject.transform;
            }

            if (collectHintText == null)
            {
                collectHintText = CreateWorldText("CollectHintText", collectHintRoot, "!", new Color(1f, 0.05f, 0.05f, 1f), 5.2f);
                collectHintText.fontStyle = FontStyles.Bold;
            }

            if (collectResultText == null)
            {
                collectResultText = CreateWorldText("CollectResultText", collectHintRoot, string.Empty, new Color(1f, 0.92f, 0.62f, 1f), 2.2f);
                collectResultText.alignment = TextAlignmentOptions.Center;
                collectResultText.gameObject.SetActive(false);
            }
        }

        private static TMP_Text CreateWorldText(string objectName, Transform parent, string text, Color color, float fontSize)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            var textComponent = textObject.AddComponent<TextMeshPro>();
            textComponent.text = text;
            textComponent.color = color;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            return textComponent;
        }

        private void RefreshCollectIndicators()
        {
            EnsureCollectIndicators();
            isCollectHintVisible = inspectorIsUnlocked && inspectorCanCollect;

            if (collectHintText != null)
            {
                collectHintText.gameObject.SetActive(isCollectHintVisible);
            }
        }

        private void ShowCollectResult(string message, bool success)
        {
            EnsureCollectIndicators();
            if (collectResultText == null)
            {
                return;
            }

            collectResultText.text = string.IsNullOrEmpty(message)
                ? (success ? "已领取。" : "暂不可领取。")
                : message;
            collectResultText.color = success
                ? new Color(1f, 0.92f, 0.62f, 1f)
                : new Color(1f, 0.42f, 0.36f, 1f);
            collectResultText.gameObject.SetActive(true);
            collectResultHideAt = Time.time + Mathf.Max(0.1f, resultVisibleSeconds);
        }

        private void UpdateIndicatorBillboard()
        {
            if (collectHintRoot == null ||
                (collectHintText == null && collectResultText == null))
            {
                return;
            }

            if (billboardCamera == null)
            {
                billboardCamera = Camera.main;
            }

            if (billboardCamera == null)
            {
                return;
            }

            var direction = billboardCamera.transform.position - collectHintRoot.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                collectHintRoot.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private void UpdateHintBob()
        {
            if (collectHintRoot == null || !isCollectHintVisible)
            {
                return;
            }

            var offset = indicatorLocalOffset;
            offset.y += Mathf.Sin(Time.time * Mathf.Max(0.01f, collectHintBobSpeed)) * Mathf.Max(0f, collectHintBobDistance);
            collectHintRoot.localPosition = offset;
        }

        private void UpdateResultLifetime()
        {
            if (collectResultText == null || !collectResultText.gameObject.activeSelf || collectResultHideAt < 0f)
            {
                return;
            }

            if (Time.time >= collectResultHideAt)
            {
                collectResultText.gameObject.SetActive(false);
                collectResultHideAt = -1f;
            }
        }

        private void ApplyHoverHighlight(bool enabled)
        {
            if (!enabled || !CanShowHoverOutline() || !IsCityInteractionEnabled())
            {
                outlineEffect?.SetVisible(false);
                return;
            }

            CacheSceneComponents();
            EnsureOutlineEffect();
            outlineEffect.Configure(highlightRenderers, hoverOutlineColor, hoverOutlinePixelWidth);
            outlineEffect.SetVisible(true);
        }

        private void InitializeRuntimeHoverDependencies()
        {
            if (!autoBindHoverOutlineDependencies)
            {
                return;
            }

            CacheSceneComponents();
        }

        private void EnsureOutlineEffect()
        {
            if (outlineEffect == null)
            {
                outlineEffect = GetComponent<CityBuildingOutlineEffect>() ??
                    gameObject.AddComponent<CityBuildingOutlineEffect>();
            }
        }

        private void EnsureSameObjectHoverCollider()
        {
            var ownCollider = GetComponent<Collider>();
            if (ownCollider == null)
            {
                ownCollider = gameObject.AddComponent<BoxCollider>();
            }

            clickableCollider = ownCollider;
            clickableCollider.enabled = true;

            if (autoFitHoverColliderToRenderers && clickableCollider is BoxCollider boxCollider)
            {
                FitBoxColliderToRenderers(boxCollider);
            }
        }

        private void FitBoxColliderToRenderers(BoxCollider boxCollider)
        {
            if (boxCollider == null || cachedRenderers == null || cachedRenderers.Length == 0)
            {
                return;
            }

            var hasBounds = false;
            var worldBounds = new Bounds(transform.position, Vector3.zero);
            foreach (var renderer in cachedRenderers)
            {
                if (renderer == null)
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

        private bool CanShowHoverOutline()
        {
            return inspectorIsUnlocked || (allowHoverOutlineWhenUnbound && definition == null);
        }

        private void RefreshHoverOutlineBindingSnapshot()
        {
            var rendererCount = highlightRenderers != null ? highlightRenderers.Count(renderer => renderer != null) : 0;
            inspectorHoverOutlineBindingSnapshot =
                $"Renderer={rendererCount}, Collider={(clickableCollider != null ? clickableCollider.GetType().Name : "无")}, OutlineEffect={(outlineEffect != null ? "已绑定" : "缺失")}, 未绑定可悬停={allowHoverOutlineWhenUnbound}";
        }

        private bool IsCityInteractionEnabled()
        {
            if (!requireCityRootActive)
            {
                return true;
            }

            ResolveGameEntry();
            if (gameEntry == null || gameEntry.CityRoot == null)
            {
                return false;
            }

            var isCityVisible = gameEntry.CityRoot.activeInHierarchy;
            var isDeskHidden = gameEntry.DeskRoot == null || !gameEntry.DeskRoot.activeInHierarchy;
            return isCityVisible && isDeskHidden;
        }

        private void ResolveGameEntry()
        {
            if (gameEntry == null)
            {
                gameEntry = FindFirstObjectByType<GameEntry>(FindObjectsInactive.Include);
            }
        }
    }
}
