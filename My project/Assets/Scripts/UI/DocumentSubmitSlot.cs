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

        [Header("提交预览：拖入后在槽中心显示需要提交的那一张卡牌")]
        [SerializeField] private GameObject submittedCardRoot;
        [SerializeField] private Image submittedIconImage;
        [SerializeField] private TMP_Text submittedNameText;
        [SerializeField] private TMP_Text submittedCountText;

        [Header("状态文本：提示当前需要的物品和拖放结果")]
        [SerializeField] private TMP_Text statusText;

        public string AcceptedItemId { get; private set; }

        public int AcceptedItemCount { get; private set; }

        public bool HasAcceptedItem { get; private set; }

        private bool acceptedItemCommitted;

        private void Awake()
        {
            ResolveDependencies();
            ConfigureDropRaycast();
            HideSubmittedPreview();
        }

        public void Configure(string requiredItemId, int requiredCount)
        {
            Clear();
            AcceptedItemId = requiredItemId ?? string.Empty;
            AcceptedItemCount = Mathf.Max(0, requiredCount);
            SetStatus(string.IsNullOrEmpty(AcceptedItemId)
                ? string.Empty
                : $"拖入：{AcceptedItemId} x{AcceptedItemCount}");
        }

        public void Clear()
        {
            RefundUncommittedAcceptedItem();
            AcceptedItemId = string.Empty;
            AcceptedItemCount = 0;
            HasAcceptedItem = false;
            acceptedItemCommitted = false;
            HideSubmittedPreview();
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
                    : $"请拖入：{AcceptedItemId} x{AcceptedItemCount}");
                return;
            }

            if (inventoryService == null || !inventoryService.HasItem(AcceptedItemId, AcceptedItemCount))
            {
                SetStatus($"数量不足：{AcceptedItemId} x{AcceptedItemCount}");
                return;
            }

            RefundUncommittedAcceptedItem();
            if (!inventoryService.TryRemoveItem(AcceptedItemId, AcceptedItemCount))
            {
                SetStatus($"提交失败：{AcceptedItemId} x{AcceptedItemCount}");
                return;
            }

            HasAcceptedItem = true;
            acceptedItemCommitted = false;
            ShowSubmittedPreview();
            SetStatus($"已放入：{AcceptedItemId} x{AcceptedItemCount}");
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

        private void ShowSubmittedPreview()
        {
            if (submittedCardRoot != null)
            {
                submittedCardRoot.SetActive(true);
            }

            if (inventoryService != null &&
                inventoryService.TryGetDefinition(AcceptedItemId, out var definition))
            {
                if (submittedIconImage != null)
                {
                    submittedIconImage.sprite = InventoryIconProvider.LoadIcon(definition.IconId);
                    submittedIconImage.enabled = submittedIconImage.sprite != null;
                    submittedIconImage.preserveAspect = true;
                }

                SetText(submittedNameText, string.IsNullOrEmpty(definition.ItemName) ? definition.ItemId : definition.ItemName);
            }
            else
            {
                SetText(submittedNameText, AcceptedItemId);
            }

            SetText(submittedCountText, AcceptedItemCount.ToString());
        }

        private void HideSubmittedPreview()
        {
            if (submittedCardRoot != null)
            {
                submittedCardRoot.SetActive(false);
            }

            if (submittedIconImage != null)
            {
                submittedIconImage.sprite = null;
                submittedIconImage.enabled = false;
            }

            SetText(submittedNameText, string.Empty);
            SetText(submittedCountText, string.Empty);
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

        private void SetStatus(string value)
        {
            SetText(statusText, value);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }
}
