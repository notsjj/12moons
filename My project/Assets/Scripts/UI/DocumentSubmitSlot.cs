using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class DocumentSubmitSlot : MonoBehaviour, IDropHandler
    {
        [Header("依赖服务：拖入卡牌时扣除或返还背包数量")]
        [SerializeField] private InventoryService inventoryService;

        [Header("提交区域：实际接收卡牌拖放的射线目标")]
        [SerializeField] private Image dropAreaImage;

        [Header("提交卡牌：复用背包卡牌预制体并缩小显示")]
        [SerializeField] private InventoryItemCard submittedCardPrefab;
        [SerializeField] private Vector2 submittedCardSize = new Vector2(96f, 118f);

        [Header("状态文本：提示当前需要的物品和拖放结果")]
        [SerializeField] private TMP_Text statusText;

        public string AcceptedItemId { get; private set; }

        public int AcceptedItemCount { get; private set; }

        public bool HasAcceptedItem { get; private set; }

        private static readonly Vector2 InventoryCardDesignSize = new Vector2(180f, 220f);
        private bool acceptedItemCommitted;
        private InventoryItemCard submittedCardInstance;

        private void Awake()
        {
            ResolveDependencies();
            ConfigureDropRaycast();
            HideSubmittedCard();
        }

        public void Configure(string requiredItemId, int requiredCount)
        {
            Clear();
            AcceptedItemId = requiredItemId ?? string.Empty;
            AcceptedItemCount = Mathf.Max(0, requiredCount);
            SetStatus(string.IsNullOrEmpty(AcceptedItemId)
                ? string.Empty
                : "拖入公文正文要求的物品。");
        }

        public void Clear()
        {
            RefundUncommittedAcceptedItem();
            AcceptedItemId = string.Empty;
            AcceptedItemCount = 0;
            HasAcceptedItem = false;
            acceptedItemCommitted = false;
            HideSubmittedCard();
            SetStatus(string.Empty);
        }

        public void MarkAcceptedItemCommitted()
        {
            acceptedItemCommitted = true;
        }

        public void OnDrop(PointerEventData eventData)
        {
            ResolveDependencies();
            var card = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<InventoryItemCard>()
                : null;

            if (card == null || string.IsNullOrEmpty(AcceptedItemId) || card.ItemId != AcceptedItemId)
            {
                SetStatus(string.IsNullOrEmpty(AcceptedItemId)
                    ? string.Empty
                    : "拖入物品与公文正文要求不符。");
                return;
            }

            if (inventoryService == null || !inventoryService.HasItem(AcceptedItemId, AcceptedItemCount))
            {
                SetStatus("数量不足，请查看公文正文要求。");
                return;
            }

            RefundUncommittedAcceptedItem();
            if (!inventoryService.TryRemoveItem(AcceptedItemId, AcceptedItemCount))
            {
                SetStatus("提交失败，请查看公文正文要求。");
                return;
            }

            HasAcceptedItem = true;
            acceptedItemCommitted = false;
            ShowSubmittedCard();
            SetStatus("已放入公文要求的物品。");
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

        private void ShowSubmittedCard()
        {
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
                var scale = CalculateSubmittedCardScale();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = InventoryCardDesignSize;
                rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            var layoutElement = submittedCardInstance.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = true;
            }
        }

        private float CalculateSubmittedCardScale()
        {
            var widthScale = submittedCardSize.x > 0f
                ? submittedCardSize.x / InventoryCardDesignSize.x
                : 1f;
            var heightScale = submittedCardSize.y > 0f
                ? submittedCardSize.y / InventoryCardDesignSize.y
                : 1f;
            return Mathf.Max(0.01f, Mathf.Min(widthScale, heightScale));
        }

        private void HideSubmittedCard()
        {
            if (submittedCardInstance != null)
            {
                submittedCardInstance.gameObject.SetActive(false);
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
