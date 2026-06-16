using System.IO;
using TMPro;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools
{
    public static class InventoryPrefabBuilder
    {
        private const string PrefabDirectory = "Assets/Prefabs/UI";
        private const string PrefabPath = PrefabDirectory + "/物品卡片.prefab";

        [MenuItem("Twelve Moons/Setup/Create Inventory Item Card Prefab")]
        public static void CreateInventoryItemCardPrefab()
        {
            Directory.CreateDirectory(PrefabDirectory);

            var cardObject = new GameObject("物品卡片", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            try
            {
                var cardRect = cardObject.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(180f, 220f);

                var backgroundImage = cardObject.GetComponent<Image>();
                backgroundImage.color = new Color(0.18f, 0.18f, 0.16f, 1f);
                backgroundImage.raycastTarget = true;

                var iconImage = CreateImage("IconImage", cardObject.transform);
                SetRect(iconImage.rectTransform, new Vector2(0f, -18f), new Vector2(76f, 76f), new Vector2(0.5f, 1f));
                iconImage.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                iconImage.rectTransform.anchorMax = new Vector2(0.5f, 1f);

                var countBadge = CreateImage("CountBadge", cardObject.transform);
                countBadge.color = new Color(0.07f, 0.07f, 0.07f, 0.9f);
                countBadge.rectTransform.anchorMin = Vector2.one;
                countBadge.rectTransform.anchorMax = Vector2.one;
                SetRect(countBadge.rectTransform, new Vector2(-14f, -14f), new Vector2(44f, 28f), Vector2.one);

                var countText = CreateText("CountText", countBadge.transform, 16, FontStyles.Bold, TextAlignmentOptions.Center);
                Stretch(countText.rectTransform);

                var nameText = CreateText("NameText", cardObject.transform, 16, FontStyles.Bold, TextAlignmentOptions.Center);
                nameText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                nameText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                SetRect(nameText.rectTransform, new Vector2(0f, -105f), new Vector2(156f, 34f), new Vector2(0.5f, 1f));

                var typeText = CreateText("TypeText", cardObject.transform, 13, FontStyles.Normal, TextAlignmentOptions.Center);
                typeText.color = new Color(0.85f, 0.82f, 0.68f, 1f);
                typeText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                typeText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                SetRect(typeText.rectTransform, new Vector2(0f, -140f), new Vector2(156f, 24f), new Vector2(0.5f, 1f));

                var descriptionText = CreateText("DescriptionText", cardObject.transform, 11, FontStyles.Normal, TextAlignmentOptions.Top);
                descriptionText.color = new Color(0.82f, 0.82f, 0.78f, 1f);
                descriptionText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                descriptionText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                SetRect(descriptionText.rectTransform, new Vector2(0f, -166f), new Vector2(150f, 44f), new Vector2(0.5f, 1f));

                var card = cardObject.AddComponent<InventoryItemCard>();
                card.Configure(iconImage, nameText, countText, typeText, descriptionText, backgroundImage);

                var prefab = PrefabUtility.SaveAsPrefabAsset(cardObject, PrefabPath);
                Selection.activeObject = prefab;
                Debug.Log($"Inventory item card prefab created at {PrefabPath}.");
            }
            finally
            {
                Object.DestroyImmediate(cardObject);
            }
        }

        private static Image CreateImage(string name, Transform parent)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, int fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
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

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
