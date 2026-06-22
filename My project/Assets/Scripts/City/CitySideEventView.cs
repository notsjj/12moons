using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TwelveMoons.City
{
    public sealed class CitySideEventView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlinePixelWidthId = Shader.PropertyToID("_OutlinePixelWidth");

        [Header("支线事件匹配：对应 SideEventConfig.SideEventId")]
        [Tooltip("支线事件 ID；必须与 SideEventConfig.SideEventId 完全一致，点击时会播放该行配置的 StoryId。")]
        [SerializeField] private string sideEventId;
        [Tooltip("是否允许鼠标或 UI 点击触发支线剧情；关闭后只显示角色，不播放剧情。")]
        [SerializeField] private bool allowClick = true;

        [Header("显示引用：2D 角色图标、感叹号和点击区域")]
        [Tooltip("支线角色 2D 图片；运行时优先使用此 SpriteRenderer 显示角色图标。若已在此组件上拖入 Sprite，不会被默认图覆盖。")]
        [SerializeField] private SpriteRenderer characterSpriteRenderer;
        [Tooltip("旧版轮廓 SpriteRenderer；仅用于兼容旧场景。新高亮使用角色 SpriteRenderer 的轮廓 Shader，此对象会被自动隐藏。")]
        [SerializeField] private SpriteRenderer hoverOutlineSpriteRenderer;
        [Tooltip("正式角色图标 Sprite；留空时会保留 SpriteRenderer 上手动拖入的 Sprite，再按 DisplayCharacterId 从 Resources 加载，仍为空才生成临时默认图。")]
        [SerializeField] private Sprite characterSprite;
        [Tooltip("红色感叹号根物体；位于角色上方，运行时只移动这一份根物体，避免出现一个显示、另一个移动。")]
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
        [Tooltip("红色感叹号上下轻微移动的幅度；会被限制为非负数。")]
        [SerializeField] private float exclamationBobDistance = 0.16f;
        [Tooltip("红色感叹号上下轻微移动的速度；会被限制为非负数。")]
        [SerializeField] private float exclamationBobSpeed = 2.4f;
        [Tooltip("默认点击区域大小；自动创建 BoxCollider 时使用，所有数值都会限制为非负。")]
        [SerializeField] private Vector3 defaultClickSize = new Vector3(1.2f, 1.8f, 0.12f);

        [Header("鼠标悬停：角色 Sprite 外轮廓 Shader 高亮")]
        [Tooltip("鼠标移到支线角色上时的外轮廓颜色；作用在角色 Sprite 的透明边缘。")]
        [SerializeField] private Color hoverOutlineColor = new Color(1f, 0.62f, 0.12f, 1f);
        [Tooltip("鼠标移到支线角色上时的轮廓像素宽度；数值越大，Sprite 透明边缘外圈越粗。")]
        [SerializeField] private int hoverOutlinePixelWidth = 3;
        [Tooltip("角色 Sprite 外轮廓 Shader；留空时会自动查找 TwelveMoons/SpriteAlphaOutline。")]
        [SerializeField] private Shader spriteOutlineShader;


        [Header("\u652f\u7ebf\u4e8b\u4ef6\u6a21\u578b\uff1a\u6709\u4e8b\u4ef6\u65f6\u663e\u793a Man \u6a21\u578b")]
        [Tooltip("\u652f\u7ebf\u4e8b\u4ef6\u9ed8\u8ba4\u663e\u793a\u7684 3D \u6a21\u578b Resources \u8def\u5f84\uff1b\u5f53\u524d\u4f7f\u7528 Assets/Resources/Art/Man.fbx\u3002")]
        [SerializeField] private string modelResourcePath = "Art/Man";
        [Tooltip("\u652f\u7ebf\u4e8b\u4ef6\u6a21\u578b\u7684\u6839\u7269\u4f53\uff1b\u4e3a\u7a7a\u65f6\u4f1a\u4ece modelResourcePath \u52a0\u8f7d\u5e76\u5b9e\u4f8b\u5316\u3002")]
        [SerializeField] private Transform modelRoot;
        [Tooltip("\u652f\u7ebf\u4e8b\u4ef6\u6a21\u578b\u76f8\u5bf9\u70b9\u4f4d\u7684\u672c\u5730\u504f\u79fb\uff1b\u53ea\u79fb\u52a8\u6a21\u578b\u6839\u7269\u4f53\uff0c\u4e0d\u6539\u70b9\u4f4d\u4f4d\u7f6e\u3002")]
        [SerializeField] private Vector3 modelLocalOffset = Vector3.zero;
        [Tooltip("\u652f\u7ebf\u4e8b\u4ef6\u6a21\u578b\u672c\u5730\u7f29\u653e\uff1b\u6240\u6709\u5206\u91cf\u4f1a\u9650\u5236\u4e3a\u975e\u8d1f\u6570\u3002")]
        [SerializeField] private Vector3 modelLocalScale = Vector3.one;
        [Tooltip("\u6a21\u578b\u60ac\u505c\u63cf\u8fb9\u7ec4\u4ef6\uff1b\u4e3a\u7a7a\u65f6\u8fd0\u884c\u65f6\u81ea\u52a8\u6dfb\u52a0\uff0c\u590d\u7528\u57ce\u533a\u5efa\u7b51\u7684\u5168\u5c40\u63cf\u8fb9\u6e32\u67d3\u3002")]
        [SerializeField] private CityBuildingOutlineEffect modelOutlineEffect;
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
        private bool canStartStory = true;
        private bool isWaitingForStoryCompletion;
        private string waitingStoryId = string.Empty;
        private Material originalSpriteMaterial;
        private Material runtimeOutlineMaterial;
        private bool isUsingOutlineMaterial;
        private Renderer[] modelRenderers = System.Array.Empty<Renderer>();
        private bool isUsingModelOutline;

        public string SideEventId => sideEventId;

        public bool IsBound => definition != null;

        public bool IsCharacterVisible => IsModelVisualVisible || (characterSpriteRenderer != null && characterSpriteRenderer.enabled);

        public bool IsModelVisualVisible => modelRoot != null && modelRoot.gameObject.activeInHierarchy;

        public bool IsExclamationVisible => exclamationRoot != null && exclamationRoot.gameObject.activeInHierarchy;

        public bool CanStartStory => canStartStory;

        public bool IsHoverOutlineVisible => isUsingOutlineMaterial || isUsingModelOutline;

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

            UnsubscribeStoryCompletion();
            ApplyHoverHighlight(false);
        }

        private void OnDestroy()
        {
            if (runtimeOutlineMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(runtimeOutlineMaterial);
                }
                else
                {
                    DestroyImmediate(runtimeOutlineMaterial);
                }

                runtimeOutlineMaterial = null;
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
            EnsureModelVisual();

            if (characterSpriteRenderer == null && modelRoot == null)
            {
                var spriteObject = new GameObject("CharacterSprite");
                spriteObject.transform.SetParent(transform, false);
                characterSpriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
                characterSpriteRenderer.sortingOrder = 20;
            }

            HideLegacyOutlineRenderer();

            if (clickableCollider == null)
            {
                var boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.size = ClampVector(defaultClickSize);
                clickableCollider = boxCollider;
            }

            RemoveExclamationVisual();
            if (modelRoot == null)
            {
                ApplySprite();
            }
            else if (characterSpriteRenderer != null)
            {
                characterSpriteRenderer.enabled = false;
            }
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
            canStartStory = definition != null;
            isWaitingForStoryCompletion = false;
            waitingStoryId = string.Empty;

            ResolveVisualReferences();
            EnsureDefaultWorldVisuals();
            RefreshLabel();
            ApplySprite();
            ApplyVisible(definition != null);
        }

        public void ClearBinding()
        {
            UnsubscribeStoryCompletion();
            ApplyHoverHighlight(false);
            definition = null;
            service = null;
            inspectorIsBound = false;
            inspectorDisplayCharacterId = string.Empty;
            inspectorStoryId = string.Empty;
            inspectorPointId = string.Empty;
            inspectorLastClickResult = string.Empty;
            canStartStory = false;
            isWaitingForStoryCompletion = false;
            waitingStoryId = string.Empty;
            RefreshLabel();
            ApplyVisible(false);
        }

        public void OnClicked()
        {
            if (!allowClick || !canStartStory || service == null || string.IsNullOrEmpty(sideEventId))
            {
                inspectorLastClickResult = "缺少服务或 SideEventId，无法播放支线剧情。";
                return;
            }

            if (IsStoryBlockingClick())
            {
                inspectorLastClickResult = "\u5267\u60c5\u64ad\u653e\u4e2d\uff0c\u652f\u7ebf\u4e8b\u4ef6\u70b9\u51fb\u5df2\u4e34\u65f6\u7981\u7528\u3002";
                return;
            }

            if (service.TryStartSideEvent(sideEventId, out var resultMessage))
            {
                inspectorLastClickResult = resultMessage;
                canStartStory = false;
                waitingStoryId = inspectorStoryId;
                SubscribeStoryCompletion();
                return;
            }

            inspectorLastClickResult = resultMessage;
        }

        private bool IsStoryBlockingClick()
        {
            var storyService = service != null ? service.StoryService : null;
            return storyService != null && storyService.CurrentPlayback != null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ApplyHoverHighlight(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ApplyHoverHighlight(false);
        }

        private void OnMouseDown()
        {
            OnClicked();
        }

        private void OnMouseEnter()
        {
            ApplyHoverHighlight(true);
        }

        private void OnMouseExit()
        {
            ApplyHoverHighlight(false);
        }

        private void ResolveVisualReferences()
        {
            if (characterSpriteRenderer == null)
            {
                foreach (var candidate in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (candidate != null && candidate.gameObject.name != "CharacterSpriteHoverOutline")
                    {
                        characterSpriteRenderer = candidate;
                        break;
                    }
                }
            }

            if (hoverOutlineSpriteRenderer == null && characterSpriteRenderer != null)
            {
                var outlineTransform = characterSpriteRenderer.transform.Find("CharacterSpriteHoverOutline");
                if (outlineTransform != null)
                {
                    hoverOutlineSpriteRenderer = outlineTransform.GetComponent<SpriteRenderer>();
                }
            }

            ResolveExclamationReferences();

            if (clickableCollider == null)
            {
                clickableCollider = GetComponentInChildren<Collider>(true);
            }
        }

        private void ResolveExclamationReferences()
        {
            if (exclamationText == null)
            {
                foreach (var candidate in GetComponentsInChildren<TMP_Text>(true))
                {
                    if (candidate != null && candidate.text.Trim() == "!")
                    {
                        exclamationText = candidate;
                        break;
                    }
                }
            }

            if (exclamationRoot == null && exclamationText != null)
            {
                exclamationRoot = exclamationText.transform;
            }

            if (exclamationRoot == null)
            {
                var existingRoot = transform.Find("SideEventExclamation");
                if (existingRoot != null)
                {
                    exclamationRoot = existingRoot;
                }
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

        private void EnsureModelVisual()
        {
            if (modelRoot == null)
            {
                var prefab = string.IsNullOrEmpty(modelResourcePath) ? null : Resources.Load<GameObject>(modelResourcePath);
                if (prefab != null)
                {
                    var modelInstance = Instantiate(prefab, transform, false);
                    modelInstance.name = prefab.name;
                    modelRoot = modelInstance.transform;
                }
            }

            if (modelRoot == null)
            {
                return;
            }

            modelRoot.localPosition = modelLocalOffset;
            modelRoot.localScale = ClampVector(modelLocalScale);
            modelRoot.localRotation = Quaternion.identity;
            CacheModelRenderers();
            EnsureModelOutlineEffect();
        }

        private void CacheModelRenderers()
        {
            modelRenderers = modelRoot != null
                ? modelRoot.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer != null && renderer.GetComponent<TMP_Text>() == null)
                    .ToArray()
                : System.Array.Empty<Renderer>();
        }

        private void EnsureModelOutlineEffect()
        {
            if (modelOutlineEffect == null)
            {
                modelOutlineEffect = GetComponent<CityBuildingOutlineEffect>() ??
                    gameObject.AddComponent<CityBuildingOutlineEffect>();
            }
        }

        private void ApplySprite()
        {
            if (characterSpriteRenderer == null)
            {
                return;
            }

            var resolvedSprite = ResolveCharacterSprite();
            if (resolvedSprite != null)
            {
                characterSpriteRenderer.sprite = resolvedSprite;
            }

            HideLegacyOutlineRenderer();
        }

        private Sprite ResolveCharacterSprite()
        {
            if (characterSprite != null)
            {
                return characterSprite;
            }

            if (characterSpriteRenderer != null && characterSpriteRenderer.sprite != null &&
                characterSpriteRenderer.sprite != fallbackCharacterSprite)
            {
                return characterSpriteRenderer.sprite;
            }

            if (!string.IsNullOrEmpty(inspectorDisplayCharacterId))
            {
                var configuredSprite = UI.CharacterPlaceholderPortraitProvider.LoadPortrait(inspectorDisplayCharacterId);
                if (configuredSprite != null)
                {
                    return configuredSprite;
                }
            }

            return GetFallbackCharacterSprite();
        }

        private void RemoveExclamationVisual()
        {
            ResolveExclamationReferences();
            if (exclamationRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(exclamationRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(exclamationRoot.gameObject);
                }
            }

            exclamationRoot = null;
            exclamationText = null;
        }

        private void EnsureExclamation()
        {
            ResolveExclamationReferences();

            if (exclamationRoot == null)
            {
                var exclamationObject = new GameObject("SideEventExclamation");
                exclamationObject.transform.SetParent(transform, false);
                exclamationRoot = exclamationObject.transform;
            }

            exclamationRoot.localPosition = exclamationLocalOffset;

            if (exclamationText == null)
            {
                exclamationText = exclamationRoot.GetComponent<TMP_Text>();
                if (exclamationText == null)
                {
                    exclamationText = exclamationRoot.gameObject.AddComponent<TextMeshPro>();
                }
            }

            exclamationText.text = "!";
            exclamationText.color = new Color(1f, 0.05f, 0.05f, 1f);
            exclamationText.fontSize = 5.2f;
            exclamationText.fontStyle = FontStyles.Bold;
            exclamationText.alignment = TextAlignmentOptions.Center;
            exclamationText.textWrappingMode = TextWrappingModes.NoWrap;
            HideDuplicateExclamationTexts();
        }

        private void HideDuplicateExclamationTexts()
        {
            foreach (var candidate in GetComponentsInChildren<TMP_Text>(true))
            {
                if (candidate == null || candidate == exclamationText || candidate.text.Trim() != "!")
                {
                    continue;
                }

                candidate.gameObject.SetActive(false);
            }
        }

        private void ApplyVisible(bool visible)
        {
            isVisible = visible;

            if (modelRoot != null)
            {
                modelRoot.gameObject.SetActive(visible);
            }

            if (characterSpriteRenderer != null)
            {
                characterSpriteRenderer.enabled = visible && modelRoot == null;
            }

            if (!visible)
            {
                ApplyHoverHighlight(false);
            }

            RemoveExclamationVisual();

            if (clickableCollider != null)
            {
                clickableCollider.enabled = visible && allowClick;
            }

            if (clickButton != null)
            {
                clickButton.gameObject.SetActive(visible);
                clickButton.interactable = visible && canStartStory;
            }
        }

        private void SubscribeStoryCompletion()
        {
            var storyService = service != null ? service.StoryService : null;
            if (storyService == null)
            {
                CompleteTriggeredStoryVisuals();
                return;
            }

            isWaitingForStoryCompletion = true;
            storyService.StoryChanged -= OnStoryChanged;
            storyService.StoryChanged += OnStoryChanged;
            OnStoryChanged();
        }

        private void UnsubscribeStoryCompletion()
        {
            var storyService = service != null ? service.StoryService : null;
            if (storyService != null)
            {
                storyService.StoryChanged -= OnStoryChanged;
            }
        }

        private void OnStoryChanged()
        {
            if (!isWaitingForStoryCompletion || service == null)
            {
                return;
            }

            var storyService = service.StoryService;
            var currentStoryId = storyService != null && storyService.CurrentPlayback != null
                ? storyService.CurrentPlayback.Story.StoryId
                : string.Empty;
            if (!string.IsNullOrEmpty(currentStoryId) &&
                string.Equals(currentStoryId, waitingStoryId, System.StringComparison.Ordinal))
            {
                return;
            }

            CompleteTriggeredStoryVisuals();
        }

        private void CompleteTriggeredStoryVisuals()
        {
            isWaitingForStoryCompletion = false;
            waitingStoryId = string.Empty;
            UnsubscribeStoryCompletion();

            if (exclamationRoot != null)
            {
                exclamationRoot.gameObject.SetActive(false);
            }

            if (clickButton != null)
            {
                clickButton.interactable = false;
            }
        }

        private void ApplyHoverHighlight(bool enabled)
        {
            HideLegacyOutlineRenderer();

            if (modelRoot != null)
            {
                ApplyModelHoverHighlight(enabled);
                return;
            }

            if (!enabled || !isVisible || characterSpriteRenderer == null || !characterSpriteRenderer.enabled)
            {
                RestoreSpriteMaterial();
                return;
            }

            var outlineMaterial = EnsureRuntimeOutlineMaterial();
            if (outlineMaterial == null)
            {
                return;
            }

            if (!isUsingOutlineMaterial)
            {
                originalSpriteMaterial = characterSpriteRenderer.sharedMaterial;
            }

            outlineMaterial.mainTexture = characterSpriteRenderer.sprite != null
                ? characterSpriteRenderer.sprite.texture
                : null;
            outlineMaterial.SetColor(OutlineColorId, hoverOutlineColor);
            outlineMaterial.SetFloat(OutlinePixelWidthId, Mathf.Max(1, hoverOutlinePixelWidth));
            characterSpriteRenderer.sharedMaterial = outlineMaterial;
            isUsingOutlineMaterial = true;
        }

        private void ApplyModelHoverHighlight(bool enabled)
        {
            if (!enabled || !isVisible || modelRoot == null || !modelRoot.gameObject.activeInHierarchy)
            {
                isUsingModelOutline = false;
                modelOutlineEffect?.SetVisible(false);
                return;
            }

            EnsureModelOutlineEffect();
            CacheModelRenderers();
            if (modelOutlineEffect == null || modelRenderers == null || !modelRenderers.Any(renderer => renderer != null))
            {
                isUsingModelOutline = false;
                return;
            }

            modelOutlineEffect.Configure(modelRenderers, hoverOutlineColor, hoverOutlinePixelWidth);
            modelOutlineEffect.SetVisible(true);
            isUsingModelOutline = true;
        }

        private Material EnsureRuntimeOutlineMaterial()
        {
            if (spriteOutlineShader == null)
            {
                spriteOutlineShader = Shader.Find("TwelveMoons/SpriteAlphaOutline");
            }

            if (spriteOutlineShader == null)
            {
                Debug.LogWarning("缺少 TwelveMoons/SpriteAlphaOutline Shader，支线角色无法显示 Sprite 外轮廓高亮。", this);
                return null;
            }

            if (runtimeOutlineMaterial == null)
            {
                runtimeOutlineMaterial = new Material(spriteOutlineShader)
                {
                    name = $"{name}_RuntimeSpriteOutlineMaterial",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            return runtimeOutlineMaterial;
        }

        private void RestoreSpriteMaterial()
        {
            if (!isUsingOutlineMaterial || characterSpriteRenderer == null)
            {
                isUsingOutlineMaterial = false;
                return;
            }

            characterSpriteRenderer.sharedMaterial = originalSpriteMaterial;
            originalSpriteMaterial = null;
            isUsingOutlineMaterial = false;
        }

        private void HideLegacyOutlineRenderer()
        {
            if (hoverOutlineSpriteRenderer != null)
            {
                hoverOutlineSpriteRenderer.enabled = false;
            }
        }

        private void UpdateBillboard()
        {
            if (modelRoot != null)
            {
                return;
            }

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
            hoverOutlinePixelWidth = Mathf.Max(1, hoverOutlinePixelWidth);
            defaultClickSize = ClampVector(defaultClickSize);
            modelLocalScale = ClampVector(modelLocalScale);
        }
    }
}
