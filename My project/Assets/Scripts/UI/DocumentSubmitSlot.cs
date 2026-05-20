using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TwelveMoons.UI
{
    public sealed class DocumentSubmitSlot : MonoBehaviour, IDropHandler
    {
        [Header("提交栏显示：拖入道具后的状态文本")]
        [SerializeField] private TMP_Text statusText;

        public string AcceptedItemId { get; private set; }

        public bool HasAcceptedItem { get; private set; }

        public void Configure(string requiredItemId, int requiredCount)
        {
            AcceptedItemId = requiredItemId ?? string.Empty;
            HasAcceptedItem = false;
            SetStatus(string.IsNullOrEmpty(AcceptedItemId)
                ? string.Empty
                : $"需要提交：{AcceptedItemId} x{requiredCount}");
        }

        public void Clear()
        {
            AcceptedItemId = string.Empty;
            HasAcceptedItem = false;
            SetStatus(string.Empty);
        }

        public void OnDrop(PointerEventData eventData)
        {
            var card = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<InventoryItemCard>()
                : null;

            if (card == null || string.IsNullOrEmpty(AcceptedItemId) || card.ItemId != AcceptedItemId)
            {
                SetStatus(string.IsNullOrEmpty(AcceptedItemId)
                    ? string.Empty
                    : $"请拖入：{AcceptedItemId}");
                return;
            }

            HasAcceptedItem = true;
            SetStatus($"已提交：{AcceptedItemId}");
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
