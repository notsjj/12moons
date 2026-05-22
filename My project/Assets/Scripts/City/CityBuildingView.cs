using UnityEngine;

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

        private void TryCollect()
        {
            if (service == null)
            {
                Debug.LogWarning($"建筑 {buildingId} 缺少 CityBuildingService，无法点击。", this);
                return;
            }

            service.TryCollect(buildingId, out _);
            RefreshState();
        }

        private void CacheSceneComponents()
        {
            var root = visualRoot != null ? visualRoot : gameObject;
            cachedRenderers = root.GetComponentsInChildren<Renderer>(true);
            cachedColliders = root.GetComponentsInChildren<Collider>(true);
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
    }
}
