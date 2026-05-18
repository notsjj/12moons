using System.Collections.Generic;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class InventoryPanelView : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private InventoryService inventoryService;
        [SerializeField] private RuntimeDataService runtimeDataService;

        [Header("Cards")]
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private InventoryItemCard cardPrefab;
        [SerializeField] private bool showZeroCountItems;
        [SerializeField] private Vector2 cardSize = new Vector2(180f, 220f);
        [SerializeField] private float minimumVisibleStep = 42f;

        private readonly List<InventoryItemCard> cards = new List<InventoryItemCard>();

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
            if (inventoryService != null)
            {
                inventoryService.InventoryChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            if (inventoryService == null || runtimeDataService == null || contentRoot == null)
            {
                return;
            }

            ClearRows();

            foreach (var definition in inventoryService.Definitions)
            {
                var state = runtimeDataService.Data.GetOrCreateItem(definition.ItemId);
                if (!showZeroCountItems && state.Count <= 0)
                {
                    continue;
                }

                CreateCard(definition, state);
            }

            LayoutCardsInSingleRow();
        }

        private void CreateCard(ItemDefinition definition, RuntimeItemState state)
        {
            var card = cardPrefab != null
                ? Instantiate(cardPrefab, contentRoot)
                : CreateDefaultCard(contentRoot);

            card.Bind(definition, state);
            cards.Add(card);
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
            LayoutCardsInSingleRow();
        }

        private void LayoutCardsInSingleRow()
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

            var availableWidth = Mathf.Max(cardSize.x, contentRoot.rect.width, contentRoot.sizeDelta.x);
            var availableHeight = Mathf.Max(cardSize.y, contentRoot.rect.height, contentRoot.sizeDelta.y);
            var maxNonOverflowStep = cards.Count <= 1
                ? 0f
                : (availableWidth - cardSize.x) / (cards.Count - 1);
            var naturalStep = cardSize.x + 12f;
            var step = cards.Count <= 1
                ? 0f
                : Mathf.Min(naturalStep, Mathf.Max(minimumVisibleStep, maxNonOverflowStep));

            if (cards.Count > 1 && step * (cards.Count - 1) + cardSize.x > availableWidth)
            {
                step = Mathf.Max(0f, (availableWidth - cardSize.x) / (cards.Count - 1));
            }

            var totalWidth = cardSize.x + step * Mathf.Max(0, cards.Count - 1);
            var startX = Mathf.Max(0f, (availableWidth - totalWidth) * 0.5f);
            var y = Mathf.Max(0f, (availableHeight - cardSize.y) * 0.5f);
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
                rectTransform.sizeDelta = cardSize;
                rectTransform.anchoredPosition = new Vector2(startX + step * index, y);
                rectTransform.SetSiblingIndex(index);
            }
        }

        private void ClearRows()
        {
            foreach (var card in cards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            cards.Clear();
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
