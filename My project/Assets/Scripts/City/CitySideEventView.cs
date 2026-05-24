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

        [Header("显示引用：2D 角色图标、感叹号和点击区域")]
        [Tooltip("支线角色 2D 图片；运行时会优先使用此 SpriteRenderer 显示角色图标。")]
        [SerializeField] private SpriteRenderer characterSpriteRenderer;
        [Tooltip("正式角色图标 Sprite；留空时会按 DisplayCharacterId 尝试从 Resources 加载，仍为空则生成一个临时 2D 小人图片。")]
        [SerializeField] private Sprite characterSprite;
        [Tooltip("红色感叹号根物体；位于角色上方，运行时会轻微上下浮动并始终朝向摄像机。")]
        [SerializeField] private Transform exclamationRoot;
        [Tooltip("红色感叹号 TMP 文本；必须使用 TextMeshPro，不使用 legacy Text。")]
        [SerializeField] private TMP_Text exclamationText;
        [Tooltip("可选 Button；如果该支线角色是 UI 图标，请拖入按钮，OnClick 会自动绑定到 OnClicked。")]
        [SerializeField] private Button clickButton;
        [Tooltip("可选 TMP 文本；用于显示角色 ID 或调试名称，不使用 legacy Text。")]
        [SerializeField] private TMP_Text labelText;
        [Tooltip("可点击碰撞体；用于 3D 场景点击，留空时自动创建 BoxCollider。")]
        [SerializeField] private Collider clickableCollider;
        [Tooltip("用于 Billboard 朝向的摄像机；留空时自动使用 Main Camera。")]
        [SerializeField] private Camera billboardCamera;

        [Header("显示参数：位置、大小和浮动")]
        [Tooltip("红色感叹号相对角色图标的位置；用于放在角色上方一点。")]
        [SerializeField] private Vector3 exclamationLocalOffset = new Vector3(0f, 1.15f, 0f);
        [Tooltip("红色感叹号上下轻微移动的幅度。")]
        [SerializeField] private float exclamationBobDistance = 0.12f;
        [Tooltip("红色感叹号上下轻微移动的速度。")]
        [SerializeField] private float exclamationBobSpeed = 2.6f;
        [Tooltip("默认点击区域大小；自动创建 BoxCollider 时使用，所有数值都会限制为非负。")]
        [SerializeField] private Vector3 defaultClickSize = new Vector3(1.2f, 1.8f, 0.12f);

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

        private static Sprite fallbackCharacterSprite;
        private CitySideEventService service;
        private SideEventDefinition definition;
        private bool isVisible;

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

        private void Update()
        {
            UpdateBillboard();
            UpdateExclamationBob();
        }

        public void Configure(string newSideEventId)
        {
            sideEventId = newSideEventId ?? string.Empty;
            ClearBinding();
        }

        public void EnsureDefaultWorldVisuals()
        {
            if (characterSpriteRenderer == null)
            {
                var spriteObject = new GameObject("CharacterSprite");
                spriteObject.transform.SetParent(transform, false);
                characterSpriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
                characterSpriteRenderer.sortingOrder = 20;
            }

            if (clickableCollider == null)
            {
                var boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.size = ClampVector(defaultClickSize);
                clickableCollider = boxCollider;
            }

            EnsureExclamation();
            ApplySprite();
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
            EnsureDefaultWorldVisuals();
            RefreshLabel();
            ApplySprite();
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
            if (characterSpriteRenderer == null)
            {
                characterSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (clickableCollider == null)
            {
                clickableCollider = GetComponentInChildren<Collider>(true);
            }

            if (exclamationText == null && exclamationRoot != null)
            {
                exclamationText = exclamationRoot.GetComponentInChildren<TMP_Text>(true);
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

        private void ApplySprite()
        {
            if (characterSpriteRenderer == null)
            {
                return;
            }

            characterSpriteRenderer.sprite = ResolveCharacterSprite();
        }

        private Sprite ResolveCharacterSprite()
        {
            if (characterSprite != null)
            {
                return characterSprite;
            }

            if (!string.IsNullOrEmpty(inspectorDisplayCharacterId))
            {
                var configuredSprite = Resources.Load<Sprite>(inspectorDisplayCharacterId);
                if (configuredSprite != null)
                {
                    return configuredSprite;
                }
            }

            return GetFallbackCharacterSprite();
        }

        private void EnsureExclamation()
        {
            if (exclamationRoot == null)
            {
                var exclamationObject = new GameObject("SideEventExclamation");
                exclamationObject.transform.SetParent(transform, false);
                exclamationObject.transform.localPosition = exclamationLocalOffset;
                exclamationRoot = exclamationObject.transform;
            }

            if (exclamationText == null)
            {
                exclamationText = exclamationRoot.gameObject.AddComponent<TextMeshPro>();
                exclamationText.text = "!";
                exclamationText.color = new Color(1f, 0.05f, 0.05f, 1f);
                exclamationText.fontSize = 5.2f;
                exclamationText.fontStyle = FontStyles.Bold;
                exclamationText.alignment = TextAlignmentOptions.Center;
                exclamationText.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        private void ApplyVisible(bool visible)
        {
            isVisible = visible;

            if (characterSpriteRenderer != null)
            {
                characterSpriteRenderer.enabled = visible;
            }

            if (exclamationRoot != null)
            {
                exclamationRoot.gameObject.SetActive(visible);
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

        private void UpdateBillboard()
        {
            if (!isVisible)
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

            var direction = billboardCamera.transform.position - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private void UpdateExclamationBob()
        {
            if (!isVisible || exclamationRoot == null)
            {
                return;
            }

            var offset = exclamationLocalOffset;
            offset.y += Mathf.Sin(Time.time * Mathf.Max(0.01f, exclamationBobSpeed)) *
                Mathf.Max(0f, exclamationBobDistance);
            exclamationRoot.localPosition = offset;
        }

        private static Vector3 ClampVector(Vector3 value)
        {
            return new Vector3(
                Mathf.Max(0f, value.x),
                Mathf.Max(0f, value.y),
                Mathf.Max(0f, value.z));
        }

        private static Sprite GetFallbackCharacterSprite()
        {
            if (fallbackCharacterSprite != null)
            {
                return fallbackCharacterSprite;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "SideEventFallbackCharacterSprite",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var clear = new Color(0f, 0f, 0f, 0f);
            var coat = new Color(0.18f, 0.52f, 0.9f, 1f);
            var face = new Color(1f, 0.86f, 0.62f, 1f);
            var outline = new Color(0.08f, 0.1f, 0.13f, 1f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - 32f;
                    var dy = y - 32f;
                    var color = clear;
                    if (InsideEllipse(dx, dy - 12f, 13f, 15f))
                    {
                        color = coat;
                    }

                    if (InsideEllipse(dx, dy + 11f, 12f, 12f))
                    {
                        color = face;
                    }

                    if (InsideEllipse(dx, dy + 11f, 14f, 14f) && !InsideEllipse(dx, dy + 11f, 12f, 12f))
                    {
                        color = outline;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            fallbackCharacterSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.08f),
                64f);
            fallbackCharacterSprite.name = "SideEventFallbackCharacterSprite";
            return fallbackCharacterSprite;
        }

        private static bool InsideEllipse(float x, float y, float radiusX, float radiusY)
        {
            return (x * x) / (radiusX * radiusX) + (y * y) / (radiusY * radiusY) <= 1f;
        }

        private void OnValidate()
        {
            exclamationBobDistance = Mathf.Max(0f, exclamationBobDistance);
            exclamationBobSpeed = Mathf.Max(0f, exclamationBobSpeed);
            defaultClickSize = ClampVector(defaultClickSize);
        }
    }
}
