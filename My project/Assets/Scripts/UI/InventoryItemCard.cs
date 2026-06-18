using System;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class InventoryItemCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("卡牌组件：背景、图标和文本引用")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("拖拽设置：是否允许从物品栏拖动")]
        [SerializeField] private bool allowDragInInventory = true;
        [SerializeField] private Color draggableColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.72f, 0.72f, 0.72f, 1f);

        [Header("选中上浮：点击卡片后向上抬起的距离")]
        [Tooltip("卡片被点击选中后，在物品栏内向上移动的距离。数值越大，上浮越高；设为 0 时不产生选中上浮。")]
        [Min(0f)]
        [SerializeField] private float selectedLiftDistance = 36f;

        private RectTransform rectTransform;
        private LayoutElement layoutElement;
        private Vector2 startAnchoredPosition;
        private Vector2 startAnchorMin;
        private Vector2 startAnchorMax;
        private Vector2 startPivot;
        private Vector2 startSizeDelta;
        private Vector3 startLocalScale;
        private int startSiblingIndex;
        private bool canDrag;
        private Canvas rootCanvas;
        private Transform originalParent;
        private bool isDragging;
        private bool wasDragged;
        private bool isSubmitAnimationLocked;

        public string ItemId { get; private set; }

        public bool CanDrag => canDrag;

        public float SelectedLiftDistance => Mathf.Max(0f, selectedLiftDistance);

        public event Action<InventoryItemCard> Clicked;

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
            if (!canDrag || isSubmitAnimationLocked)
            {
                return;
            }

            CacheComponents();
            startAnchoredPosition = rectTransform.anchoredPosition;
            startAnchorMin = rectTransform.anchorMin;
            startAnchorMax = rectTransform.anchorMax;
            startPivot = rectTransform.pivot;
            startSizeDelta = rectTransform.sizeDelta;
            startLocalScale = rectTransform.localScale;
            startSiblingIndex = transform.GetSiblingIndex();
            originalParent = transform.parent;
            isDragging = true;
            wasDragged = false;
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
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!canDrag || isSubmitAnimationLocked || rectTransform == null)
            {
                return;
            }

            var scaleFactor = rootCanvas != null && rootCanvas.scaleFactor > 0f ? rootCanvas.scaleFactor : 1f;
            rectTransform.anchoredPosition += eventData.delta / scaleFactor;
            wasDragged = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!canDrag)
            {
                return;
            }

            if (isSubmitAnimationLocked)
            {
                isDragging = false;
                return;
            }

            if (TrySubmitReleasedCard(eventData))
            {
                isDragging = false;
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

            isDragging = false;
        }

        private bool TrySubmitReleasedCard(PointerEventData eventData)
        {
            var submitSlots = FindObjectsByType<DocumentSubmitSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var submitSlot in submitSlots)
            {
                if (submitSlot != null &&
                    submitSlot.CanReceiveReleasedCard(this, eventData) &&
                    submitSlot.TryAcceptDraggedCard(this))
                {
                    return true;
                }
            }

            return false;
        }

        public void LockForSubmitAnimation(Transform animationParent)
        {
            CacheComponents();
            isSubmitAnimationLocked = true;
            isDragging = false;
            var worldCenter = rectTransform != null
                ? rectTransform.TransformPoint(rectTransform.rect.center)
                : transform.position;
            var currentSize = rectTransform != null && rectTransform.rect.width > 0f && rectTransform.rect.height > 0f
                ? rectTransform.rect.size
                : startSizeDelta;

            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = true;
            }

            if (animationParent != null)
            {
                transform.SetParent(animationParent, true);
            }

            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = currentSize;
                rectTransform.localScale = Vector3.one;
                rectTransform.anchoredPosition = WorldPointToAnchoredPosition(rectTransform, worldCenter);
            }

            transform.SetAsLastSibling();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void RestoreAfterSubmitAnimation()
        {
            CacheComponents();
            isSubmitAnimationLocked = false;

            if (originalParent != null)
            {
                transform.SetParent(originalParent, true);
                transform.SetSiblingIndex(startSiblingIndex);
            }

            if (rectTransform != null)
            {
                rectTransform.anchorMin = startAnchorMin;
                rectTransform.anchorMax = startAnchorMax;
                rectTransform.pivot = startPivot;
                rectTransform.sizeDelta = startSizeDelta;
                rectTransform.localScale = startLocalScale;
                rectTransform.anchoredPosition = startAnchoredPosition;
            }

            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = false;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isSubmitAnimationLocked && !isDragging && !wasDragged)
            {
                Clicked?.Invoke(this);
            }

            wasDragged = false;
        }

        private static Vector2 WorldPointToAnchoredPosition(RectTransform target, Vector3 worldPoint)
        {
            var parentRect = target != null ? target.parent as RectTransform : null;
            if (parentRect == null || target == null)
            {
                return Vector2.zero;
            }

            var localPoint = parentRect.InverseTransformPoint(worldPoint);
            var anchorReference = new Vector2(
                (target.anchorMin.x - parentRect.pivot.x) * parentRect.rect.width,
                (target.anchorMin.y - parentRect.pivot.y) * parentRect.rect.height);
            return new Vector2(localPoint.x, localPoint.y) - anchorReference;
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
