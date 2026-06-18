using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class InventoryPanelView : MonoBehaviour
    {
        [Header("依赖服务：背包数据与运行时存档")]
        [SerializeField] private InventoryService inventoryService;
        [SerializeField] private RuntimeDataService runtimeDataService;

        [Header("卡牌显示：内容根节点、卡牌预制体与尺寸")]
        [SerializeField] private RectTransform contentRoot;
        [Header("物品栏前景：压在卡片上方但不拦截点击")]
        [Tooltip("物品栏外框或前景图层；运行时会保持在卡片内容节点上方，并关闭自身及子图形 Raycast，避免挡住卡片拖拽。为空时按名称自动查找。")]
        [SerializeField] private RectTransform itemBarForegroundRoot;
        [Tooltip("自动查找物品栏前景图层时使用的子物体名称。默认匹配当前 Prefab 中的“物品栏”。")]
        [SerializeField] private string itemBarForegroundName = "物品栏";
        [Tooltip("勾选后每次刷新和弹出都会把物品栏前景图层移到最后渲染，让卡片看起来落在栏位内部而不是压在物品栏前面。")]
        [SerializeField] private bool keepItemBarForegroundAboveCards = true;
        [SerializeField] private InventoryItemCard cardPrefab;
        [SerializeField] private bool showZeroCountItems;
        [Header("卡牌尺寸保护：保持预制体原始宽高")]
        [Tooltip("勾选后按物品卡牌预制体自身 RectTransform 尺寸布局，不再把卡牌强行改成 cardSize，避免美术卡面变形。")]
        [SerializeField] private bool preservePrefabCardSize = true;
        [SerializeField] private Vector2 cardSize = new Vector2(180f, 220f);
        [SerializeField] private float minimumVisibleStep = 42f;
        [Header("卡片初始位置：物品栏内基础Y轴偏移")]
        [Tooltip("卡片未被选中时，在物品栏内容区域内的基础 Y 轴偏移。正数向上，负数向下；只影响卡片排布，不改变物品栏本身位置。")]
        [SerializeField] private float cardBaseYOffset;

        [Header("卡牌动画：新增卡牌后旧卡牌移动到新位置")]
        [SerializeField] private float layoutMoveDuration = 0.24f;
        [SerializeField] private Ease layoutMoveEase = Ease.OutCubic;

        [Header("点击抬起：点击卡牌后向上移动的距离")]
        [SerializeField] private float selectedLiftDistance = 36f;

        [Header("公文提交弹出：物品面板默认藏在桌面下方")]
        [Tooltip("勾选后 Awake 时立即把物品面板放到下方隐藏位置；公文需要提交物品时再弹出。")]
        [SerializeField] private bool hideBelowDeskOnAwake = true;
        [Tooltip("物品面板隐藏时相对当前位置向下移动的距离，只影响本面板自身 anchoredPosition。")]
        [SerializeField] private float hiddenBelowOffset = 260f;
        [Tooltip("物品面板从下方弹出到手动摆好位置的时长。")]
        [SerializeField] private float documentSubmitShowDuration = 0.32f;
        [Tooltip("物品面板回落到桌面下方隐藏位置的时长。")]
        [SerializeField] private float documentSubmitHideDuration = 0.24f;
        [Tooltip("弹出时卡牌因为惯性向上冲出的距离，随后会回到物品栏约束范围内。")]
        [SerializeField] private float cardOvershootDistance = 96f;
        [Header("卡片上冲物理感：随机速度、碰撞感与慢速回弹")]
        [Tooltip("卡片弹出时最小上冲距离；每张卡会随机取值，形成不同初速度。")]
        [SerializeField] private float cardLaunchMinDistance = 90f;
        [Tooltip("卡片弹出时最大上冲距离；会与旧的惯性距离取较大值，保证位移更明显。")]
        [SerializeField] private float cardLaunchMaxDistance = 170f;
        [Tooltip("卡片向上冲出的最短时间；时间越短，初速度看起来越快。")]
        [SerializeField] private float cardLaunchMinDuration = 0.14f;
        [Tooltip("卡片向上冲出的最长时间；每张卡不同，制造随机速度。")]
        [SerializeField] private float cardLaunchMaxDuration = 0.30f;
        [Tooltip("卡片上冲时的随机水平偏移；多张卡会短暂交叠，形成碰撞感，最终仍回到栏位约束位置。")]
        [SerializeField] private float cardLaunchHorizontalJitter = 32f;
        [Tooltip("每张卡开始上冲前的最大随机延迟；用于让卡片错峰运动并互相靠近。")]
        [SerializeField] private float cardLaunchDelayMax = 0.08f;
        [Tooltip("弹出时卡片上冲后回落到栏内的时间；数值越大，回弹越慢。")]
        [SerializeField] private float cardSettleDuration = 0.74f;

        [Header("公文提交弹出调试快照")]
        [Tooltip("只读运行时状态：当前物品面板是否处于公文提交弹出状态。")]
        [SerializeField] private bool isDocumentSubmitVisibleSnapshot;

        private readonly List<InventoryItemCard> cards = new List<InventoryItemCard>();
        private readonly Dictionary<string, InventoryItemCard> cardsByItemId = new Dictionary<string, InventoryItemCard>();
        private string selectedItemId;
        private RectTransform panelRectTransform;
        private Vector2 openAnchoredPosition;
        private Vector2 hiddenAnchoredPosition;
        private Tween panelTween;
        private bool panelPositionsCached;

        public bool IsDocumentSubmitVisible => isDocumentSubmitVisibleSnapshot;

        private void Awake()
        {
            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<InventoryService>();
            }

            if (runtimeDataService == null)
            {
                runtimeDataService = FindFirstObjectByType<RuntimeDataService>();
            }

            if (contentRoot == null)
            {
                contentRoot = transform as RectTransform;
            }

            EnsureItemBarForegroundOnTop();
            CachePrefabCardSize();
            CachePanelPositions();
            if (hideBelowDeskOnAwake)
            {
                HideForDocumentSubmission(true);
            }
        }

        private void OnEnable()
        {
            if (inventoryService != null)
            {
                inventoryService.InventoryChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            KillPanelTween();
            KillCardTweens();
            if (inventoryService != null)
            {
                inventoryService.InventoryChanged -= Refresh;
            }
        }

        public void ShowForDocumentSubmission()
        {
            CachePanelPositions();
            gameObject.SetActive(true);
            RestoreInteraction();
            EnsureItemBarForegroundOnTop();
            isDocumentSubmitVisibleSnapshot = true;
            KillPanelTween();

            if (panelRectTransform == null)
            {
                return;
            }

            panelRectTransform.anchoredPosition = hiddenAnchoredPosition;
            if (Application.isPlaying && documentSubmitShowDuration > 0f)
            {
                panelTween = panelRectTransform
                    .DOAnchorPos(openAnchoredPosition, documentSubmitShowDuration)
                    .SetEase(Ease.OutCubic)
                    .OnComplete(PlayCardsOvershoot);
            }
            else
            {
                panelRectTransform.anchoredPosition = openAnchoredPosition;
                PlayCardsOvershoot();
            }
        }

        public void HideForDocumentSubmission()
        {
            HideForDocumentSubmission(false);
        }

        public void HideForDocumentSubmission(bool instant)
        {
            CachePanelPositions();
            isDocumentSubmitVisibleSnapshot = false;
            KillPanelTween();
            KillCardTweens();

            if (panelRectTransform == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (!instant && Application.isPlaying && documentSubmitHideDuration > 0f && gameObject.activeInHierarchy)
            {
                panelTween = panelRectTransform
                    .DOAnchorPos(hiddenAnchoredPosition, documentSubmitHideDuration)
                    .SetEase(Ease.InCubic)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                panelRectTransform.anchoredPosition = hiddenAnchoredPosition;
                gameObject.SetActive(false);
            }
        }

        public void Refresh()
        {
            if (inventoryService == null || runtimeDataService == null || contentRoot == null)
            {
                return;
            }

            EnsureItemBarForegroundOnTop();
            var activeItemIds = new HashSet<string>();

            foreach (var definition in inventoryService.Definitions)
            {
                var state = runtimeDataService.Data.GetOrCreateItem(definition.ItemId);
                if (!showZeroCountItems && state.Count <= 0)
                {
                    continue;
                }

                activeItemIds.Add(definition.ItemId);
                var card = GetOrCreateCard(definition, state);
                card.Bind(definition, state);
            }

            RemoveInactiveCards(activeItemIds);
            SortCardsByDefinitions();
            LayoutCardsInSingleRow(true);
        }

        private InventoryItemCard GetOrCreateCard(ItemDefinition definition, RuntimeItemState state)
        {
            if (cardsByItemId.TryGetValue(definition.ItemId, out var existingCard) && existingCard != null)
            {
                return existingCard;
            }

            var card = CreateCard(definition, state);
            cardsByItemId[definition.ItemId] = card;
            return card;
        }

        private InventoryItemCard CreateCard(ItemDefinition definition, RuntimeItemState state)
        {
            var card = cardPrefab != null
                ? Instantiate(cardPrefab, contentRoot)
                : CreateDefaultCard(contentRoot);

            EnsureCardRaycast(card);
            card.transform.SetAsLastSibling();
            card.Bind(definition, state);
            card.Clicked += HandleCardClicked;
            cards.Add(card);
            return card;
        }

        private InventoryItemCard CreateDefaultCard(Transform parent)
        {
            var cardObject = new GameObject("InventoryItemCard");
            cardObject.transform.SetParent(parent, false);
            var cardRect = cardObject.AddComponent<RectTransform>();
            cardRect.sizeDelta = cardSize;

            var background = cardObject.AddComponent<Image>();
            background.color = new Color(0.18f, 0.18f, 0.16f, 1f);
            background.raycastTarget = true;
            cardObject.AddComponent<CanvasGroup>();
            var cardLayout = cardObject.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = cardSize.x;
            cardLayout.preferredHeight = cardSize.y;
            cardLayout.minWidth = cardSize.x;
            cardLayout.minHeight = cardSize.y;

            var iconImage = CreateImage("IconImage", cardObject.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            SetRect(iconImage.rectTransform, new Vector2(0f, -18f), new Vector2(76f, 76f), new Vector2(0.5f, 1f));

            var countBadge = CreateImage("CountBadge", cardObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f));
            countBadge.color = new Color(0.07f, 0.07f, 0.07f, 0.9f);
            SetRect(countBadge.rectTransform, new Vector2(-14f, -14f), new Vector2(44f, 28f), new Vector2(1f, 1f));

            var countText = CreateText("CountText", countBadge.transform, 16, FontStyles.Bold, TextAlignmentOptions.Center);
            SetStretchRect(countText.rectTransform, Vector2.zero, Vector2.zero);

            var nameText = CreateText("NameText", cardObject.transform, 16, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(nameText.rectTransform, new Vector2(0f, -105f), new Vector2(156f, 34f), new Vector2(0.5f, 1f));

            var typeText = CreateText("TypeText", cardObject.transform, 13, FontStyles.Normal, TextAlignmentOptions.Center);
            typeText.color = new Color(0.85f, 0.82f, 0.68f, 1f);
            SetRect(typeText.rectTransform, new Vector2(0f, -140f), new Vector2(156f, 24f), new Vector2(0.5f, 1f));

            var descriptionText = CreateText("DescriptionText", cardObject.transform, 11, FontStyles.Normal, TextAlignmentOptions.Top);
            descriptionText.color = new Color(0.82f, 0.82f, 0.78f, 1f);
            SetRect(descriptionText.rectTransform, new Vector2(0f, -166f), new Vector2(150f, 44f), new Vector2(0.5f, 1f));

            var card = cardObject.AddComponent<InventoryItemCard>();
            card.Configure(iconImage, nameText, countText, typeText, descriptionText, background);
            return card;
        }

        public void RefreshLayout()
        {
            LayoutCardsInSingleRow(true);
        }

        private void LayoutCardsInSingleRow(bool animate)
        {
            if (contentRoot == null || cards.Count == 0)
            {
                return;
            }

            var layoutGroup = contentRoot.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.enabled = false;
            }

            var layoutCardSize = GetLayoutCardSize();
            var availableWidth = Mathf.Max(layoutCardSize.x, contentRoot.rect.width, contentRoot.sizeDelta.x);
            var availableHeight = Mathf.Max(layoutCardSize.y, contentRoot.rect.height, contentRoot.sizeDelta.y);
            var maxNonOverflowStep = cards.Count <= 1
                ? 0f
                : (availableWidth - layoutCardSize.x) / (cards.Count - 1);
            var naturalStep = layoutCardSize.x + 12f;
            var step = cards.Count <= 1
                ? 0f
                : Mathf.Min(naturalStep, Mathf.Max(minimumVisibleStep, maxNonOverflowStep));

            if (cards.Count > 1 && step * (cards.Count - 1) + layoutCardSize.x > availableWidth)
            {
                step = Mathf.Max(0f, (availableWidth - layoutCardSize.x) / (cards.Count - 1));
            }

            var totalWidth = layoutCardSize.x + step * Mathf.Max(0, cards.Count - 1);
            var startX = Mathf.Max(0f, (availableWidth - totalWidth) * 0.5f);
            var y = Mathf.Max(0f, (availableHeight - layoutCardSize.y) * 0.5f) + cardBaseYOffset;
            for (var index = 0; index < cards.Count; index++)
            {
                var rectTransform = cards[index].transform as RectTransform;
                if (rectTransform == null)
                {
                    continue;
                }

                rectTransform.anchorMin = new Vector2(0f, 0f);
                rectTransform.anchorMax = new Vector2(0f, 0f);
                rectTransform.pivot = new Vector2(0f, 0f);
                var targetPosition = new Vector2(startX + step * index, y + GetCardLift(cards[index]));
                rectTransform.DOKill();
                if (animate && Application.isPlaying && layoutMoveDuration > 0f && gameObject.activeInHierarchy)
                {
                    rectTransform.DOAnchorPos(targetPosition, layoutMoveDuration).SetEase(layoutMoveEase);
                }
                else
                {
                    rectTransform.anchoredPosition = targetPosition;
                }

                rectTransform.SetSiblingIndex(index);
            }
        }

        private float GetCardLift(InventoryItemCard card)
        {
            return card != null && card.ItemId == selectedItemId
                ? Mathf.Max(0f, card.SelectedLiftDistance, selectedLiftDistance)
                : 0f;
        }

        private void PlayCardsOvershoot()
        {
            LayoutCardsInSingleRow(false);
            if (!Application.isPlaying || cardOvershootDistance <= 0f || cardSettleDuration <= 0f)
            {
                return;
            }

            foreach (var card in cards)
            {
                var rectTransform = card != null ? card.transform as RectTransform : null;
                if (rectTransform == null)
                {
                    continue;
                }

                var settledPosition = rectTransform.anchoredPosition;
                var maxDistance = Mathf.Max(0f, cardOvershootDistance, cardLaunchMaxDistance);
                var minDistance = Mathf.Clamp(cardLaunchMinDistance, 0f, maxDistance);
                var launchDistance = Random.Range(minDistance, maxDistance);
                var minLaunchDuration = Mathf.Max(0.01f, Mathf.Min(cardLaunchMinDuration, cardLaunchMaxDuration));
                var maxLaunchDuration = Mathf.Max(0.01f, Mathf.Max(cardLaunchMinDuration, cardLaunchMaxDuration));
                var launchDuration = Random.Range(minLaunchDuration, maxLaunchDuration);
                var delay = Random.Range(0f, Mathf.Max(0f, cardLaunchDelayMax));
                var horizontalJitter = Mathf.Max(0f, cardLaunchHorizontalJitter);
                var horizontalOffset = Random.Range(-horizontalJitter, horizontalJitter);
                var settleDuration = Mathf.Max(0.01f, cardSettleDuration * Random.Range(0.9f, 1.18f));
                var overshootPosition = settledPosition + new Vector2(horizontalOffset, launchDistance);
                rectTransform.DOKill();
                var sequence = DOTween.Sequence().SetTarget(rectTransform);
                sequence.AppendInterval(delay);
                sequence.Append(rectTransform
                    .DOAnchorPos(overshootPosition, launchDuration)
                    .SetEase(Ease.OutQuad));
                sequence.Append(rectTransform
                    .DOAnchorPos(settledPosition, settleDuration)
                    .SetEase(Ease.OutBounce));
            }
        }

        private void CachePanelPositions()
        {
            if (panelRectTransform == null)
            {
                panelRectTransform = transform as RectTransform;
            }

            if (panelRectTransform == null)
            {
                return;
            }

            if (!panelPositionsCached)
            {
                openAnchoredPosition = panelRectTransform.anchoredPosition;
                panelPositionsCached = true;
            }

            hiddenAnchoredPosition = openAnchoredPosition - new Vector2(0f, Mathf.Max(0f, hiddenBelowOffset));
        }

        private void CachePrefabCardSize()
        {
            if (!preservePrefabCardSize || cardPrefab == null)
            {
                return;
            }

            var prefabRect = cardPrefab.transform as RectTransform;
            if (prefabRect != null && prefabRect.sizeDelta.x > 0f && prefabRect.sizeDelta.y > 0f)
            {
                cardSize = prefabRect.sizeDelta;
            }
        }

        private Vector2 GetLayoutCardSize()
        {
            if (preservePrefabCardSize && cards.Count > 0)
            {
                foreach (var card in cards)
                {
                    var rectTransform = card != null ? card.transform as RectTransform : null;
                    if (rectTransform != null && rectTransform.sizeDelta.x > 0f && rectTransform.sizeDelta.y > 0f)
                    {
                        return rectTransform.sizeDelta;
                    }
                }
            }

            return cardSize;
        }

        private static void EnsureCardRaycast(InventoryItemCard card)
        {
            if (card == null)
            {
                return;
            }

            var graphic = card.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
            }

            var canvasGroup = card.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        private void RestoreInteraction()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void EnsureItemBarForegroundOnTop()
        {
            if (!keepItemBarForegroundAboveCards)
            {
                return;
            }

            if (itemBarForegroundRoot == null)
            {
                itemBarForegroundRoot = FindDirectChildRectTransform(itemBarForegroundName);
            }

            if (itemBarForegroundRoot == null)
            {
                return;
            }

            itemBarForegroundRoot.SetAsLastSibling();
            foreach (var graphic in itemBarForegroundRoot.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }

        private RectTransform FindDirectChildRectTransform(string childName)
        {
            if (string.IsNullOrEmpty(childName))
            {
                return null;
            }

            foreach (Transform child in transform)
            {
                if (child.name == childName)
                {
                    return child as RectTransform;
                }
            }

            return null;
        }

        private void KillPanelTween()
        {
            panelTween?.Kill();
            panelTween = null;
            panelRectTransform?.DOKill();
        }

        private void KillCardTweens()
        {
            foreach (var card in cards)
            {
                var rectTransform = card != null ? card.transform as RectTransform : null;
                rectTransform?.DOKill();
            }
        }

        private void HandleCardClicked(InventoryItemCard card)
        {
            if (card == null || string.IsNullOrEmpty(card.ItemId))
            {
                return;
            }

            selectedItemId = selectedItemId == card.ItemId ? string.Empty : card.ItemId;
            LayoutCardsInSingleRow(true);
        }

        private void RemoveInactiveCards(HashSet<string> activeItemIds)
        {
            for (var index = cards.Count - 1; index >= 0; index--)
            {
                var card = cards[index];
                if (card == null || activeItemIds.Contains(card.ItemId))
                {
                    continue;
                }

                card.Clicked -= HandleCardClicked;
                cardsByItemId.Remove(card.ItemId);
                if (selectedItemId == card.ItemId)
                {
                    selectedItemId = string.Empty;
                }

                Destroy(card.gameObject);
                cards.RemoveAt(index);
            }
        }

        private void SortCardsByDefinitions()
        {
            cards.Clear();
            foreach (var definition in inventoryService.Definitions)
            {
                if (cardsByItemId.TryGetValue(definition.ItemId, out var card) && card != null)
                {
                    cards.Add(card);
                }
            }
        }

        private void ClearRows()
        {
            foreach (var card in cards)
            {
                if (card != null)
                {
                    card.Clicked -= HandleCardClicked;
                    Destroy(card.gameObject);
                }
            }

            cards.Clear();
            cardsByItemId.Clear();
        }

        private static Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            var rectTransform = imageObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            var image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, int fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.AddComponent<RectTransform>();
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
        {
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }

        private static void SetStretchRect(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }
}
