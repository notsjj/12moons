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

        public string PointId => pointId;

        public bool IsMatched => definition != null;

        public CityPointDefinition Definition => definition;

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
        }

        private void OnValidate()
        {
            if (gizmoRadius < 0f)
            {
                gizmoRadius = 0f;
            }
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
