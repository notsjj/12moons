using DG.Tweening;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class DocumentSubmitSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        [Header("依赖服务：拖入卡牌时扣除或返还背包数量")]
        [SerializeField] private InventoryService inventoryService;

        [Header("提交区域：实际接收卡牌拖放的射线目标")]
        [SerializeField] private Image dropAreaImage;
        [Header("释放判定：允许卡片碰到槽位边缘时提交")]
        [Tooltip("拖拽松手时，卡片矩形与提交槽位矩形允许额外扩张的像素范围；用于避免刚碰到边缘却因为坐标精度被判失败。")]
        [SerializeField] private float releaseOverlapPadding = 18f;

        [Header("提交卡牌：复用背包卡牌预制体并缩小显示")]
        [SerializeField] private InventoryItemCard submittedCardPrefab;
        [Tooltip("提交动效中卡牌的显示尺寸；默认使用物品卡牌原始设计尺寸，不再压缩成小卡。")]
        [SerializeField] private Vector2 submittedCardSize = new Vector2(180f, 220f);

        [Header("提交动效：卡牌吸附到左滚轴并滑入")]
        [Tooltip("公文左滚轴 RectTransform；提交卡牌会先吸附到它的右边界，再滑入到左边界对齐。")]
        [SerializeField] private RectTransform leftScrollEnd;
        [Tooltip("公文内容视口 RectTransform；提交卡牌初始会放在内容视口和左滚轴之间。")]
        [SerializeField] private RectTransform contentViewport;
        [Tooltip("拖拽提交后，卡牌吸附到左滚轴右边界的时长。")]
        [SerializeField] private float snapToScrollDuration = 0.62f;
        [Tooltip("提交后卡牌在空中悬停、等待吸力增强的时长。")]
        [SerializeField] private float preSnapHoverDuration = 0.16f;
        [Tooltip("卡牌吸附到左滚轴右边界后停顿的时长。")]
        [SerializeField] private float snapPauseDuration = 0.5f;
        [Tooltip("吸附完成后，卡牌滑入左滚轴内部的时长。")]
        [SerializeField] private float slideIntoScrollDuration = 1.125f;
        [Header("吸入抖动调参：滑入左滚轴时的震动手感")]
        [Tooltip("卡牌被左滚轴吸入时的轻微横向抖动幅度。")]
        [SerializeField] private float slideAbsorbShakeDistance = 4f;
        [Tooltip("卡牌被左滚轴吸入时的抖动频率；数值越高，抖动越密。")]
        [SerializeField] private float slideAbsorbShakeFrequency = 92f;

        [Header("状态文本：提示当前需要的物品和拖放结果")]
        [SerializeField] private TMP_Text statusText;
        [Header("提交调试快照：运行时只读")]
        [Tooltip("只读：当前槽位额外可接受的第二种物品 ID，通常来自另一个选项的提交需求。")]
        [SerializeField] private string alternateAcceptedItemIdSnapshot;
        [Tooltip("只读：当前槽位额外可接受的第二种物品数量。")]
        [SerializeField] private int alternateAcceptedItemCountSnapshot;
        [Tooltip("只读：最近一次拖拽释放检测到的卡片物品 ID。")]
        [SerializeField] private string lastReleasedCardItemIdSnapshot;
        [Tooltip("只读：最近一次拖拽释放判定或提交失败的原因。")]
        [SerializeField] private string lastRejectReasonSnapshot;
        [Tooltip("只读：最近一次释放时是否碰到了提交槽位矩形。")]
        [SerializeField] private bool lastReleaseOverlappedSlotSnapshot;

        public string AcceptedItemId { get; private set; }

        public int AcceptedItemCount { get; private set; }

        public bool HasAcceptedItem { get; private set; }

        private static readonly Vector2 InventoryCardDesignSize = new Vector2(180f, 220f);
        private bool acceptedItemCommitted;
        private InventoryItemCard submittedCardInstance;
        private Sequence submittedCardSequence;
        private string alternateAcceptedItemId;
        private int alternateAcceptedItemCount;
        private bool submitAnimationInProgress;

        public GameObject SubmittedCardPreviewObject => submittedCardInstance != null ? submittedCardInstance.gameObject : null;

        private void Awake()
        {
            ResolveDependencies();
            ConfigureDropRaycast();
            HideSubmittedCard();
        }

        public void Configure(string requiredItemId, int requiredCount)
        {
            Configure(requiredItemId, requiredCount, string.Empty, 0);
        }

        public void Configure(string requiredItemId, int requiredCount, string alternateRequiredItemId, int alternateRequiredCount)
        {
            Clear();
            AcceptedItemId = NormalizeItemId(requiredItemId, requiredCount);
            AcceptedItemCount = string.IsNullOrEmpty(AcceptedItemId) ? 0 : Mathf.Max(0, requiredCount);
            alternateAcceptedItemId = NormalizeItemId(alternateRequiredItemId, alternateRequiredCount);
            alternateAcceptedItemCount = string.IsNullOrEmpty(alternateAcceptedItemId) ? 0 : Mathf.Max(0, alternateRequiredCount);
            RefreshDebugRequirementSnapshot();
            SetStatus(string.IsNullOrEmpty(AcceptedItemId) && string.IsNullOrEmpty(alternateAcceptedItemId)
                ? string.Empty
                : "拖入公文正文要求的物品。");
        }

        public void Clear()
        {
            RefundUncommittedAcceptedItem();
            AcceptedItemId = string.Empty;
            AcceptedItemCount = 0;
            alternateAcceptedItemId = string.Empty;
            alternateAcceptedItemCount = 0;
            HasAcceptedItem = false;
            acceptedItemCommitted = false;
            RefreshDebugRequirementSnapshot();
            HideSubmittedCard();
            SetStatus(string.Empty);
        }

        public void MarkAcceptedItemCommitted()
        {
            acceptedItemCommitted = true;
        }

        public void ConfigureAnimationAnchors(RectTransform leftScroll, RectTransform viewport)
        {
            leftScrollEnd = leftScroll;
            contentViewport = viewport;
        }

        public void OnDrop(PointerEventData eventData)
        {
            ResolveDependencies();
            var card = eventData != null && eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<InventoryItemCard>()
                : null;

            TryAcceptDraggedCard(card);
        }

        public bool CanReceiveReleasedCard(InventoryItemCard card, PointerEventData eventData)
        {
            lastReleasedCardItemIdSnapshot = card != null ? card.ItemId : string.Empty;
            lastReleaseOverlappedSlotSnapshot = false;
            if (card == null)
            {
                lastRejectReasonSnapshot = "没有检测到释放的物品卡片。";
                return false;
            }

            if (!isActiveAndEnabled)
            {
                lastRejectReasonSnapshot = "提交槽位未启用。";
                return false;
            }

            if (!TryResolveAcceptedRequirement(card, out _, out _))
            {
                lastRejectReasonSnapshot = $"释放的物品 {card.ItemId} 不属于当前公文任一提交需求。";
                return false;
            }

            var dropRect = GetDropRectTransform();
            var slotRect = transform as RectTransform;
            var cardRect = card.transform as RectTransform;
            if (cardRect == null || (dropRect == null && slotRect == null))
            {
                lastRejectReasonSnapshot = "提交槽位或卡片缺少 RectTransform。";
                return false;
            }

            var eventCamera = eventData != null ? eventData.pressEventCamera : null;
            if (eventData != null &&
                ((dropRect != null && RectTransformUtility.RectangleContainsScreenPoint(dropRect, eventData.position, eventCamera)) ||
                 (slotRect != null && RectTransformUtility.RectangleContainsScreenPoint(slotRect, eventData.position, eventCamera))))
            {
                lastReleaseOverlappedSlotSnapshot = true;
                lastRejectReasonSnapshot = string.Empty;
                return true;
            }

            var overlapped = (dropRect != null && WorldRectsOverlap(cardRect, dropRect, releaseOverlapPadding)) ||
                (slotRect != null && WorldRectsOverlap(cardRect, slotRect, releaseOverlapPadding));
            lastReleaseOverlappedSlotSnapshot = overlapped;
            lastRejectReasonSnapshot = overlapped ? string.Empty : "释放时卡片矩形没有碰到提交槽位矩形。";
            return overlapped;
        }

        public bool TryAcceptCard(InventoryItemCard card)
        {
            ResolveDependencies();
            lastReleasedCardItemIdSnapshot = card != null ? card.ItemId : string.Empty;
            if (card == null || !TryResolveAcceptedRequirement(card, out var resolvedItemId, out var resolvedItemCount))
            {
                SetStatus(string.IsNullOrEmpty(AcceptedItemId)
                    ? string.Empty
                    : "拖入物品与公文正文要求不符。");
                lastRejectReasonSnapshot = card == null
                    ? "没有检测到释放的物品卡片。"
                    : $"释放的物品 {card.ItemId} 不属于当前公文任一提交需求。";
                return false;
            }

            if (inventoryService == null || !inventoryService.HasItem(resolvedItemId, resolvedItemCount))
            {
                SetStatus("数量不足，请查看公文正文要求。");
                lastRejectReasonSnapshot = $"背包数量不足：{resolvedItemId} x{resolvedItemCount}。";
                return false;
            }

            RefundUncommittedAcceptedItem();
            if (!inventoryService.TryRemoveItem(resolvedItemId, resolvedItemCount))
            {
                SetStatus("提交失败，请查看公文正文要求。");
                lastRejectReasonSnapshot = $"扣除物品失败：{resolvedItemId} x{resolvedItemCount}。";
                return false;
            }

            AcceptedItemId = resolvedItemId;
            AcceptedItemCount = resolvedItemCount;
            HasAcceptedItem = true;
            acceptedItemCommitted = false;
            lastRejectReasonSnapshot = string.Empty;
            ShowSubmittedCard();
            SetStatus("已放入公文要求的物品。");
            return true;
        }

        public bool TryAcceptDraggedCard(InventoryItemCard card)
        {
            ResolveDependencies();
            lastReleasedCardItemIdSnapshot = card != null ? card.ItemId : string.Empty;
            if (submitAnimationInProgress)
            {
                lastRejectReasonSnapshot = "已有卡牌正在被卷轴吸附。";
                return false;
            }

            if (card == null || !TryResolveAcceptedRequirement(card, out var resolvedItemId, out var resolvedItemCount))
            {
                SetStatus(string.IsNullOrEmpty(AcceptedItemId)
                    ? string.Empty
                    : "拖入物品与公文正文要求不符。");
                lastRejectReasonSnapshot = card == null
                    ? "没有检测到释放的物品卡片。"
                    : $"释放的物品 {card.ItemId} 不属于当前公文任一提交需求。";
                return false;
            }

            if (inventoryService == null || !inventoryService.HasItem(resolvedItemId, resolvedItemCount))
            {
                SetStatus("数量不足，请查看公文正文要求。");
                lastRejectReasonSnapshot = $"背包数量不足：{resolvedItemId} x{resolvedItemCount}。";
                return false;
            }

            RefundUncommittedAcceptedItem();
            AcceptedItemId = resolvedItemId;
            AcceptedItemCount = resolvedItemCount;
            HasAcceptedItem = false;
            acceptedItemCommitted = false;
            lastRejectReasonSnapshot = string.Empty;
            BeginSubmittedCardAnimation(card, resolvedItemId, resolvedItemCount);
            SetStatus("卡牌正在被卷轴吸附。");
            return true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Right || !HasAcceptedItem)
            {
                return;
            }

            RefundUncommittedAcceptedItem();
            HasAcceptedItem = false;
            acceptedItemCommitted = false;
            HideSubmittedCard();
            SetStatus("已退回提交卡牌。");
        }

        private void RefundUncommittedAcceptedItem()
        {
            if (!HasAcceptedItem ||
                acceptedItemCommitted ||
                string.IsNullOrEmpty(AcceptedItemId) ||
                AcceptedItemCount <= 0)
            {
                return;
            }

            ResolveDependencies();
            inventoryService?.AddItem(AcceptedItemId, AcceptedItemCount);
        }

        private void BeginSubmittedCardAnimation(InventoryItemCard card, string itemId, int itemCount)
        {
            KillSubmittedCardTween();
            submittedCardInstance = card;
            submitAnimationInProgress = true;
            card.LockForSubmitAnimation(transform);
            PlaySubmittedCardScrollAnimation(() => CompleteSubmittedCardAnimation(card, itemId, itemCount));
        }

        private void CompleteSubmittedCardAnimation(InventoryItemCard card, string itemId, int itemCount)
        {
            submitAnimationInProgress = false;
            if (card != null)
            {
                card.RestoreAfterSubmitAnimation();
            }

            submittedCardInstance = null;
            ResolveDependencies();
            if (inventoryService == null || !inventoryService.TryRemoveItem(itemId, itemCount))
            {
                HasAcceptedItem = false;
                lastRejectReasonSnapshot = $"扣除物品失败：{itemId} x{itemCount}。";
                SetStatus("提交失败，请查看公文正文要求。");
                return;
            }

            AcceptedItemId = itemId;
            AcceptedItemCount = itemCount;
            HasAcceptedItem = true;
            acceptedItemCommitted = false;
            lastRejectReasonSnapshot = string.Empty;
            SetStatus("已放入公文要求的物品。");
        }

        private void ShowSubmittedCard()
        {
            KillSubmittedCardTween();
            if (submittedCardPrefab == null ||
                inventoryService == null ||
                !inventoryService.TryGetDefinition(AcceptedItemId, out var definition))
            {
                HideSubmittedCard();
                return;
            }

            if (submittedCardInstance == null)
            {
                submittedCardInstance = Instantiate(submittedCardPrefab, transform);
                submittedCardInstance.name = "SubmittedInventoryItemCard";
                DisablePreviewCardInput(submittedCardInstance.gameObject);
            }

            submittedCardInstance.gameObject.SetActive(true);
            submittedCardInstance.Bind(definition, new RuntimeItemState(AcceptedItemId, AcceptedItemCount));
            var rectTransform = submittedCardInstance.transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = GetSubmittedCardDisplaySize(rectTransform);
                rectTransform.localScale = Vector3.one;
            }

            var layoutElement = submittedCardInstance.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = true;
            }

            PlaySubmittedCardScrollAnimation();
        }

        private Vector2 GetSubmittedCardDisplaySize(RectTransform rectTransform)
        {
            if (submittedCardSize.x > 0f && submittedCardSize.y > 0f)
            {
                return submittedCardSize;
            }

            return rectTransform != null && rectTransform.sizeDelta.x > 0f && rectTransform.sizeDelta.y > 0f
                ? rectTransform.sizeDelta
                : InventoryCardDesignSize;
        }

        private void HideSubmittedCard()
        {
            KillSubmittedCardTween();
            if (submittedCardInstance != null)
            {
                if (submitAnimationInProgress)
                {
                    submittedCardInstance.RestoreAfterSubmitAnimation();
                    submitAnimationInProgress = false;
                }
                else if (submittedCardInstance.gameObject != null)
                {
                    submittedCardInstance.gameObject.SetActive(false);
                }
            }

            submittedCardInstance = null;
        }

        private void PlaySubmittedCardScrollAnimation(System.Action onComplete = null)
        {
            var rectTransform = submittedCardInstance != null ? submittedCardInstance.transform as RectTransform : null;
            if (rectTransform == null || leftScrollEnd == null || contentViewport == null)
            {
                return;
            }

            var parentRect = rectTransform.parent as RectTransform;
            if (parentRect == null)
            {
                return;
            }

            var scrollCorners = new Vector3[4];
            leftScrollEnd.GetWorldCorners(scrollCorners);

            var scrollLeft = scrollCorners[0].x;
            var scrollRight = scrollCorners[2].x;
            var centerY = (scrollCorners[0].y + scrollCorners[2].y) * 0.5f;
            var visualHalfWidth = Mathf.Max(1f, rectTransform.rect.width, rectTransform.sizeDelta.x) * 0.5f;

            var snapWorld = new Vector3(scrollRight + visualHalfWidth, centerY, scrollCorners[0].z);
            var insideWorld = new Vector3(scrollLeft + visualHalfWidth, centerY, scrollCorners[0].z);

            if (!Application.isPlaying)
            {
                rectTransform.anchoredPosition = WorldPointToAnchoredPosition(rectTransform, parentRect, insideWorld);
                onComplete?.Invoke();
                return;
            }

            submittedCardSequence = DOTween.Sequence();
            submittedCardSequence.AppendInterval(Mathf.Max(0f, preSnapHoverDuration));
            var snapLocal = WorldPointToAnchoredPosition(rectTransform, parentRect, snapWorld);
            submittedCardSequence.Append(rectTransform
                .DOAnchorPos(snapLocal, Mathf.Max(0f, snapToScrollDuration))
                .SetEase(Ease.InCubic));
            submittedCardSequence.AppendCallback(() => rectTransform.anchoredPosition = snapLocal);
            submittedCardSequence.AppendInterval(Mathf.Max(0f, snapPauseDuration));
            var slideStart = WorldPointToAnchoredPosition(rectTransform, parentRect, snapWorld);
            var slideEnd = WorldPointToAnchoredPosition(rectTransform, parentRect, insideWorld);
            var slideDuration = Mathf.Max(0f, slideIntoScrollDuration);
            submittedCardSequence.Append(rectTransform
                .DOAnchorPos(slideEnd, slideDuration)
                .SetEase(Ease.InOutSine)
                .OnUpdate(() =>
                {
                    if (slideAbsorbShakeDistance <= 0f || slideDuration <= 0f)
                    {
                        return;
                    }

                    var progress = Mathf.InverseLerp(slideStart.x, slideEnd.x, rectTransform.anchoredPosition.x);
                    var shake = Mathf.Sin(Time.time * Mathf.Max(0f, slideAbsorbShakeFrequency)) *
                        slideAbsorbShakeDistance *
                        Mathf.Sin(progress * Mathf.PI);
                    rectTransform.anchoredPosition += new Vector2(shake, 0f);
                }));
            submittedCardSequence.AppendCallback(() => rectTransform.anchoredPosition = slideEnd);
            submittedCardSequence.OnComplete(() => onComplete?.Invoke());
        }

        private static Vector2 WorldPointToAnchoredPosition(RectTransform target, RectTransform parentRect, Vector3 worldPoint)
        {
            if (target == null || parentRect == null)
            {
                return Vector2.zero;
            }

            var localPoint = parentRect.InverseTransformPoint(worldPoint);
            var anchorReference = new Vector2(
                (target.anchorMin.x - parentRect.pivot.x) * parentRect.rect.width,
                (target.anchorMin.y - parentRect.pivot.y) * parentRect.rect.height);
            return new Vector2(localPoint.x, localPoint.y) - anchorReference;
        }

        private RectTransform GetDropRectTransform()
        {
            if (dropAreaImage != null)
            {
                return dropAreaImage.rectTransform;
            }

            return transform as RectTransform;
        }

        private static bool WorldRectsOverlap(RectTransform first, RectTransform second, float padding)
        {
            var firstRect = ExpandRect(GetWorldAxisAlignedRect(first), Mathf.Max(0f, padding));
            var secondRect = ExpandRect(GetWorldAxisAlignedRect(second), Mathf.Max(0f, padding));
            return firstRect.xMin <= secondRect.xMax &&
                firstRect.xMax >= secondRect.xMin &&
                firstRect.yMin <= secondRect.yMax &&
                firstRect.yMax >= secondRect.yMin;
        }

        private static Rect GetWorldAxisAlignedRect(RectTransform rectTransform)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            var minX = corners[0].x;
            var maxX = corners[0].x;
            var minY = corners[0].y;
            var maxY = corners[0].y;

            for (var index = 1; index < corners.Length; index++)
            {
                minX = Mathf.Min(minX, corners[index].x);
                maxX = Mathf.Max(maxX, corners[index].x);
                minY = Mathf.Min(minY, corners[index].y);
                maxY = Mathf.Max(maxY, corners[index].y);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private static Rect ExpandRect(Rect rect, float padding)
        {
            rect.xMin -= padding;
            rect.xMax += padding;
            rect.yMin -= padding;
            rect.yMax += padding;
            return rect;
        }

        private bool TryResolveAcceptedRequirement(InventoryItemCard card, out string itemId, out int count)
        {
            itemId = string.Empty;
            count = 0;
            if (card == null || string.IsNullOrEmpty(card.ItemId))
            {
                return false;
            }

            if (MatchesRequirement(card.ItemId, AcceptedItemId, AcceptedItemCount))
            {
                itemId = AcceptedItemId;
                count = AcceptedItemCount;
                return true;
            }

            if (MatchesRequirement(card.ItemId, alternateAcceptedItemId, alternateAcceptedItemCount))
            {
                itemId = alternateAcceptedItemId;
                count = alternateAcceptedItemCount;
                return true;
            }

            return false;
        }

        private static bool MatchesRequirement(string cardItemId, string requiredItemId, int requiredCount)
        {
            return !string.IsNullOrEmpty(cardItemId) &&
                !string.IsNullOrEmpty(requiredItemId) &&
                requiredCount > 0 &&
                string.Equals(cardItemId, requiredItemId, System.StringComparison.Ordinal);
        }

        private static string NormalizeItemId(string itemId, int count)
        {
            return count > 0 ? itemId ?? string.Empty : string.Empty;
        }

        private void RefreshDebugRequirementSnapshot()
        {
            alternateAcceptedItemIdSnapshot = alternateAcceptedItemId ?? string.Empty;
            alternateAcceptedItemCountSnapshot = alternateAcceptedItemCount;
        }

        private void KillSubmittedCardTween()
        {
            submittedCardSequence?.Kill();
            submittedCardSequence = null;
            if (submittedCardInstance != null)
            {
                (submittedCardInstance.transform as RectTransform)?.DOKill();
            }
        }

        private void ResolveDependencies()
        {
            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<InventoryService>();
            }
        }

        private void ConfigureDropRaycast()
        {
            if (dropAreaImage == null)
            {
                dropAreaImage = GetComponent<Image>();
            }

            if (dropAreaImage != null)
            {
                dropAreaImage.raycastTarget = true;
            }
        }

        private static void DisablePreviewCardInput(GameObject cardObject)
        {
            var card = cardObject.GetComponent<InventoryItemCard>();
            if (card != null)
            {
                card.enabled = false;
            }

            var canvasGroup = cardObject.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            foreach (var graphic in cardObject.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
            {
                statusText.text = value ?? string.Empty;
            }
        }
    }
}
