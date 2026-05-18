using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class InventoryItemCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Card Parts")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Drag")]
        [SerializeField] private bool allowDragInInventory = true;
        [SerializeField] private Color draggableColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.72f, 0.72f, 0.72f, 1f);

        private RectTransform rectTransform;
        private LayoutElement layoutElement;
        private Vector2 startAnchoredPosition;
        private int startSiblingIndex;
        private bool canDrag;
        private Canvas rootCanvas;
        private Transform originalParent;

        public string ItemId { get; private set; }

        public bool CanDrag => canDrag;

        private void Awake()
        {
            CacheComponents();
        }

        public void Configure(
            Image itemIconImage,
            TMP_Text itemNameText,
            TMP_Text itemCountText,
            TMP_Text itemTypeText,
            TMP_Text itemDescriptionText,
            Image itemBackgroundImage = null)
        {
            iconImage = itemIconImage;
            nameText = itemNameText;
            countText = itemCountText;
            typeText = itemTypeText;
            descriptionText = itemDescriptionText;
            backgroundImage = itemBackgroundImage;
            CacheComponents();
        }

        public void Bind(ItemDefinition definition, RuntimeItemState state)
        {
            CacheComponents();

            ItemId = definition.ItemId;
            canDrag = allowDragInInventory;

            if (iconImage != null)
            {
                iconImage.sprite = InventoryIconProvider.LoadIcon(definition.IconId);
                iconImage.enabled = true;
                iconImage.preserveAspect = true;
            }

            if (nameText != null)
            {
                nameText.text = string.IsNullOrEmpty(definition.ItemName)
                    ? definition.ItemId
                    : definition.ItemName;
            }

            if (countText != null)
            {
                countText.text = state.Count.ToString();
            }

            if (typeText != null)
            {
                typeText.text = definition.ItemType.ToString();
            }

            if (descriptionText != null)
            {
                descriptionText.text = definition.Description;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = definition.CanDrag ? draggableColor : lockedColor;
                backgroundImage.raycastTarget = true;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!canDrag)
            {
                return;
            }

            CacheComponents();
            startAnchoredPosition = rectTransform.anchoredPosition;
            startSiblingIndex = transform.GetSiblingIndex();
            originalParent = transform.parent;
            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = true;
            }

            if (rootCanvas != null)
            {
                transform.SetParent(rootCanvas.transform, true);
            }

            transform.SetAsLastSibling();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.82f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!canDrag || rectTransform == null)
            {
                return;
            }

            var scaleFactor = rootCanvas != null && rootCanvas.scaleFactor > 0f ? rootCanvas.scaleFactor : 1f;
            rectTransform.anchoredPosition += eventData.delta / scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!canDrag)
            {
                return;
            }

            if (rectTransform != null)
            {
                if (originalParent != null)
                {
                    transform.SetParent(originalParent, true);
                    transform.SetSiblingIndex(startSiblingIndex);
                    rectTransform.anchoredPosition = startAnchoredPosition;
                }
            }

            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = false;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
        }

        private void CacheComponents()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (layoutElement == null)
            {
                layoutElement = GetComponent<LayoutElement>();
            }

            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInParent<Canvas>();
            }
        }
    }
}
