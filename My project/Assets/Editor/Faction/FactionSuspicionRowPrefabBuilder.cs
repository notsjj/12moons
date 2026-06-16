using System.IO;
using TMPro;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools
{
    public static class FactionSuspicionRowPrefabBuilder
    {
        private const string PrefabDirectory = "Assets/Prefabs/UI";
        private const string PrefabPath = PrefabDirectory + "/阵营质疑行.prefab";

        [MenuItem("Twelve Moons/Setup/Create Faction Suspicion Row Prefab")]
        public static void CreateFactionSuspicionRowPrefab()
        {
            Directory.CreateDirectory(PrefabDirectory);

            var rowObject = new GameObject("阵营质疑行", typeof(RectTransform), typeof(Image));
            try
            {
                var rowRect = rowObject.GetComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(360f, 54f);

                var rowBackground = rowObject.GetComponent<Image>();
                rowBackground.color = new Color(0.16f, 0.16f, 0.15f, 0.92f);
                rowBackground.raycastTarget = false;

                var nameText = CreateText("NameText", rowObject.transform, 16, FontStyles.Bold, TextAlignmentOptions.Left);
                SetRect(nameText.rectTransform, new Vector2(14f, 0f), new Vector2(92f, 42f), new Vector2(0f, 0.5f));

                var iconImage = CreateImage("FactionIcon", rowObject.transform, Color.white);
                iconImage.enabled = false;
                iconImage.preserveAspect = true;
                SetRect(iconImage.rectTransform, new Vector2(112f, 0f), new Vector2(24f, 24f), new Vector2(0.5f, 0.5f));

                var valueText = CreateText("ValueText", rowObject.transform, 14, FontStyles.Normal, TextAlignmentOptions.Right);
                SetRect(valueText.rectTransform, new Vector2(-14f, 0f), new Vector2(72f, 42f), new Vector2(1f, 0.5f));

                var sliderObject = new GameObject("SuspicionSlider", typeof(RectTransform), typeof(Slider));
                sliderObject.transform.SetParent(rowObject.transform, false);
                var sliderRect = sliderObject.GetComponent<RectTransform>();
                sliderRect.anchorMin = new Vector2(0f, 0.5f);
                sliderRect.anchorMax = new Vector2(1f, 0.5f);
                sliderRect.pivot = new Vector2(0.5f, 0.5f);
                sliderRect.offsetMin = new Vector2(146f, -8f);
                sliderRect.offsetMax = new Vector2(-92f, 8f);

                var sliderBackground = CreateImage("Background", sliderObject.transform, new Color(0.08f, 0.08f, 0.08f, 1f));
                Stretch(sliderBackground.rectTransform);

                var fillArea = new GameObject("Fill Area", typeof(RectTransform));
                fillArea.transform.SetParent(sliderObject.transform, false);
                StretchWithOffsets(fillArea.GetComponent<RectTransform>(), new Vector2(2f, 2f), new Vector2(-2f, -2f));

                var sliderFill = CreateImage("Fill", fillArea.transform, new Color(0.74f, 0.22f, 0.18f, 1f));
                Stretch(sliderFill.rectTransform);

                var slider = sliderObject.GetComponent<Slider>();
                slider.transition = Selectable.Transition.None;
                slider.interactable = false;
                slider.fillRect = sliderFill.rectTransform;
                slider.targetGraphic = sliderBackground;

                var row = rowObject.AddComponent<FactionSuspicionRow>();
                row.Configure(nameText, valueText, slider, iconImage, rowBackground, sliderBackground, sliderFill);

                var prefab = PrefabUtility.SaveAsPrefabAsset(rowObject, PrefabPath);
                Selection.activeObject = prefab;
                Debug.Log($"Faction suspicion row prefab created at {PrefabPath}.");
            }
            finally
            {
                Object.DestroyImmediate(rowObject);
            }
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.color = color;
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
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
        {
            rectTransform.anchorMin = new Vector2(pivot.x, 0.5f);
            rectTransform.anchorMax = new Vector2(pivot.x, 0.5f);
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            StretchWithOffsets(rectTransform, Vector2.zero, Vector2.zero);
        }

        private static void StretchWithOffsets(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }
}
