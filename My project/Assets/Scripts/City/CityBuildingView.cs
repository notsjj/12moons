using UnityEngine;
using TMPro;

namespace TwelveMoons.City
{
    public sealed class CityBuildingView : MonoBehaviour
    {
        [Header("建筑配置匹配：对应 CityBuildingConfig.BuildingId")]
        [Tooltip("建筑 ID；必须与 CityBuildingConfig 表中的 BuildingId 完全一致，建筑注册器会用它匹配配置和运行时解锁状态。")]
        [SerializeField] private string buildingId;
        [Tooltip("建筑所在点位 ID；建议与 CityBuildingConfig.PointId、CityPointConfig.PointId 保持一致，用于检查建筑和城区点位的对应关系。")]
        [SerializeField] private string pointId;

        [Header("显示与点击：绑定实际 3D 模型")]
        [Tooltip("建筑显示根物体；填写实际建筑模型子物体或包住模型的父物体。留空时会控制本物体下所有 Renderer 和 Collider。")]
        [SerializeField] private GameObject visualRoot;
        [Tooltip("可点击碰撞体；留空时会自动使用本物体或子物体上的 Collider。建筑未解锁或冷却中时会禁用点击。")]
        [SerializeField] private Collider clickableCollider;
        [Tooltip("启用后鼠标点击 Collider 会尝试领取建筑效果；关闭时只显示建筑，不响应点击。")]
        [SerializeField] private bool allowMouseClick = true;

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

        [Header("鼠标高亮：可领取建筑的悬停提示")]
        [Tooltip("鼠标移上可领取建筑时参与高亮的 Renderer；留空时自动使用 VisualRoot 下所有 Renderer。")]
        [SerializeField] private Renderer[] highlightRenderers;
        [Tooltip("鼠标悬停时叠加到建筑材质上的高亮颜色；当前实现不依赖额外 Shader。")]
        [SerializeField] private Color hoverHighlightColor = new Color(1f, 0.82f, 0.25f, 1f);
        [Tooltip("鼠标悬停时的自发光强度；材质支持 Emission 时会更明显。")]
        [SerializeField] private float hoverEmissionIntensity = 0.8f;

        [Header("运行时只读快照：建筑显示与领取状态")]
        [Tooltip("当前建筑是否已经匹配到 CityBuildingConfig 中的配置行。")]
        [SerializeField] private bool inspectorIsMatched;
        [Tooltip("匹配到的建筑中文名；来自 CityBuildingConfig.BuildingName。")]
        [SerializeField] private string inspectorBuildingName;
        [Tooltip("匹配到的所属城区；来自 CityBuildingConfig.CityAreaId。")]
        [SerializeField] private string inspectorCityAreaId;
        [Tooltip("匹配到的点位 ID；来自 CityBuildingConfig.PointId，用于确认与本组件填写的 PointId 是否一致。")]
        [SerializeField] private string inspectorConfigPointId;
        [Tooltip("匹配到的建筑效果类型；Resource 表示产出资源/道具，Suspicion 表示降低阵营质疑度。")]
        [SerializeField] private string inspectorEffectType;
        [Tooltip("当前运行时是否已经解锁；未解锁时建筑模型应隐藏且不可点击。")]
        [SerializeField] private bool inspectorIsUnlocked;
        [Tooltip("当前回合是否可以点击领取；同回合重复点击或冷却未结束时为 false。")]
        [SerializeField] private bool inspectorCanCollect;
        [Tooltip("建筑配置和运行状态摘要；用于在 Inspector 中快速确认绑定是否正确。")]
        [SerializeField] private string inspectorSummary;

        private CityBuildingDefinition definition;
        private CityBuildingService service;
        private Renderer[] cachedRenderers;
        private Collider[] cachedColliders;
        private MaterialPropertyBlock highlightBlock;
        private bool isCollectHintVisible;
        private float collectResultHideAt = -1f;

        public string BuildingId => buildingId;

        public string PointId => pointId;

        public bool IsMatched => definition != null;

        public void Configure(string newBuildingId, string newPointId)
        {
            buildingId = newBuildingId ?? string.Empty;
            pointId = newPointId ?? string.Empty;
            ClearBinding();
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
            inspectorIsUnlocked = service != null && service.IsUnlocked(buildingId);
            inspectorCanCollect = service != null && service.CanCollect(buildingId);

            var pointStatus = definition == null || string.IsNullOrEmpty(pointId) || pointId == definition.PointId
                ? "点位匹配"
                : $"点位不一致：View={pointId}, Config={definition.PointId}";
            inspectorSummary = definition != null
                ? $"BuildingId={definition.BuildingId}, 名称={definition.BuildingName}, 城区={definition.CityAreaId}, {pointStatus}, 已解锁={inspectorIsUnlocked}, 可领取={inspectorCanCollect}"
                : $"BuildingId={buildingId} 未匹配到 CityBuildingConfig。";

            ApplyVisibility(inspectorIsUnlocked, inspectorCanCollect);
            RefreshCollectIndicators();
            if (!inspectorCanCollect)
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
            if (allowMouseClick)
            {
                TryCollect();
            }
        }

        private void OnMouseEnter()
        {
            if (allowMouseClick && inspectorCanCollect)
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
            cachedRenderers = root.GetComponentsInChildren<Renderer>(true);
            cachedColliders = root.GetComponentsInChildren<Collider>(true);
            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                highlightRenderers = cachedRenderers;
            }

            if (clickableCollider == null)
            {
                clickableCollider = GetComponent<Collider>();
                if (clickableCollider == null && cachedColliders.Length > 0)
                {
                    clickableCollider = cachedColliders[0];
                }
            }
        }

        private void ApplyVisibility(bool visible, bool canClick)
        {
            CacheSceneComponents();

            var root = visualRoot != null ? visualRoot : gameObject;
            if (visible && root != null && !root.activeSelf)
            {
                root.SetActive(true);
            }

            foreach (var renderer in cachedRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }

            foreach (var collider in cachedColliders)
            {
                if (collider != null)
                {
                    collider.enabled = visible && canClick;
                }
            }

            if (clickableCollider != null)
            {
                clickableCollider.enabled = visible && canClick;
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
            CacheSceneComponents();
            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                return;
            }

            if (highlightBlock == null)
            {
                highlightBlock = new MaterialPropertyBlock();
            }

            foreach (var targetRenderer in highlightRenderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                if (!enabled)
                {
                    targetRenderer.SetPropertyBlock(null);
                    continue;
                }

                highlightBlock.Clear();
                var color = hoverHighlightColor;
                highlightBlock.SetColor("_BaseColor", color);
                highlightBlock.SetColor("_Color", color);
                highlightBlock.SetColor("_EmissionColor", color * Mathf.Max(0f, hoverEmissionIntensity));
                targetRenderer.SetPropertyBlock(highlightBlock);
            }
        }
    }
}
