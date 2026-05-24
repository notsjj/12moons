using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TwelveMoons.City
{
    public sealed class CitySideEventView : MonoBehaviour, IPointerClickHandler
    {
        [Header("支线事件匹配：对应 SideEventConfig.SideEventId")]
        [Tooltip("支线事件 ID；必须与 SideEventConfig.SideEventId 完全一致，点击时会播放该行配置的 StoryId。")]
        [SerializeField] private string sideEventId;
        [Tooltip("是否允许鼠标或 UI 点击触发支线剧情；关闭后只显示角色，不播放剧情。")]
        [SerializeField] private bool allowClick = true;

        [Header("显示引用：角色图标和可选文本")]
        [Tooltip("可选 Button；如果该支线角色是 UI 图标，请拖入按钮，OnClick 会自动绑定到 OnClicked。")]
        [SerializeField] private Button clickButton;
        [Tooltip("可选 TMP 文本；用于显示角色 ID 或调试名称，不使用 legacy Text。")]
        [SerializeField] private TMP_Text labelText;
        [Tooltip("可选 Renderer；用于 3D 图标显示/隐藏，留空时自动查找子物体 Renderer。")]
        [SerializeField] private Renderer iconRenderer;
        [Tooltip("可选 Collider；用于 3D 场景点击，留空时自动查找子物体 Collider。")]
        [SerializeField] private Collider clickableCollider;

        [Header("运行时只读快照：支线角色状态")]
        [Tooltip("当前支线角色是否已经绑定到 SideEventConfig 配置。")]
        [SerializeField] private bool inspectorIsBound;
        [Tooltip("显示角色 ID；来自 SideEventConfig.DisplayCharacterId。")]
        [SerializeField] private string inspectorDisplayCharacterId;
        [Tooltip("点击后播放的剧情 ID；来自 SideEventConfig.StoryId。")]
        [SerializeField] private string inspectorStoryId;
        [Tooltip("所属点位 ID；来自 SideEventConfig.PointId，用于确认角色是否生成在正确点位。")]
        [SerializeField] private string inspectorPointId;
        [Tooltip("最近一次点击结果；用于在 Inspector 中确认支线角色点击是否成功。")]
        [SerializeField] private string inspectorLastClickResult;

        private CitySideEventService service;
        private SideEventDefinition definition;

        public string SideEventId => sideEventId;

        public bool IsBound => definition != null;

        private void Awake()
        {
            ResolveVisualReferences();
            BindButton();
        }

        private void OnEnable()
        {
            BindButton();
        }

        private void OnDisable()
        {
            if (clickButton != null)
            {
                clickButton.onClick.RemoveListener(OnClicked);
            }
        }

        public void Configure(string newSideEventId)
        {
            sideEventId = newSideEventId ?? string.Empty;
            ClearBinding();
        }

        public void Bind(SideEventDefinition sideEventDefinition, CitySideEventService sideEventService)
        {
            definition = sideEventDefinition;
            service = sideEventService;
            sideEventId = definition != null ? definition.SideEventId : sideEventId;
            inspectorIsBound = definition != null;
            inspectorDisplayCharacterId = definition != null ? definition.DisplayCharacterId : string.Empty;
            inspectorStoryId = definition != null ? definition.StoryId : string.Empty;
            inspectorPointId = definition != null ? definition.PointId : string.Empty;

            ResolveVisualReferences();
            RefreshLabel();
            ApplyVisible(definition != null);
        }

        public void ClearBinding()
        {
            definition = null;
            service = null;
            inspectorIsBound = false;
            inspectorDisplayCharacterId = string.Empty;
            inspectorStoryId = string.Empty;
            inspectorPointId = string.Empty;
            inspectorLastClickResult = string.Empty;
            RefreshLabel();
            ApplyVisible(false);
        }

        public void OnClicked()
        {
            if (!allowClick || service == null || string.IsNullOrEmpty(sideEventId))
            {
                inspectorLastClickResult = "缺少服务或 SideEventId，无法播放支线剧情。";
                return;
            }

            if (service.TryStartSideEvent(sideEventId, out var resultMessage))
            {
                inspectorLastClickResult = resultMessage;
                ApplyVisible(false);
                return;
            }

            inspectorLastClickResult = resultMessage;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked();
        }

        private void OnMouseDown()
        {
            OnClicked();
        }

        private void ResolveVisualReferences()
        {
            if (iconRenderer == null)
            {
                iconRenderer = GetComponentInChildren<Renderer>(true);
            }

            if (clickableCollider == null)
            {
                clickableCollider = GetComponentInChildren<Collider>(true);
            }
        }

        private void BindButton()
        {
            if (clickButton == null)
            {
                return;
            }

            clickButton.onClick.RemoveListener(OnClicked);
            clickButton.onClick.AddListener(OnClicked);
        }

        private void RefreshLabel()
        {
            if (labelText != null)
            {
                labelText.text = string.IsNullOrEmpty(inspectorDisplayCharacterId)
                    ? sideEventId
                    : inspectorDisplayCharacterId;
            }
        }

        private void ApplyVisible(bool visible)
        {
            if (iconRenderer != null)
            {
                iconRenderer.enabled = visible;
            }

            if (clickableCollider != null)
            {
                clickableCollider.enabled = visible && allowClick;
            }

            if (clickButton != null)
            {
                clickButton.gameObject.SetActive(visible);
            }
        }
    }
}
