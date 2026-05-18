using System;
using System.Collections.Generic;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class SuspicionPanelView : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private FactionService factionService;
        [SerializeField] private RuntimeDataService runtimeDataService;

        [Header("Rows")]
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private FactionSuspicionRow rowPrefab;
        [SerializeField] private FactionIconBinding[] factionIcons = Array.Empty<FactionIconBinding>();

        [Header("Feedback")]
        [SerializeField] private TMP_Text feedbackText;

        private readonly List<FactionSuspicionRow> rows = new List<FactionSuspicionRow>();

        private void Awake()
        {
            if (factionService == null)
            {
                factionService = FindFirstObjectByType<FactionService>();
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
            if (factionService != null)
            {
                factionService.FactionsChanged += Refresh;
                factionService.ThresholdTriggered += ShowThresholdFeedback;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (factionService != null)
            {
                factionService.FactionsChanged -= Refresh;
                factionService.ThresholdTriggered -= ShowThresholdFeedback;
            }
        }

        public void Refresh()
        {
            if (factionService == null || runtimeDataService == null || contentRoot == null)
            {
                return;
            }

            ClearRows();

            foreach (var definition in factionService.Definitions)
            {
                var state = runtimeDataService.Data.GetOrCreateFaction(definition.FactionId, definition.InitSuspicion);
                var row = rowPrefab != null
                    ? Instantiate(rowPrefab, contentRoot)
                    : CreateDefaultRow(contentRoot);
                row.Bind(definition, state, FindFactionIcon(definition.FactionId));
                rows.Add(row);
            }
        }

        private void ShowThresholdFeedback(FactionThresholdResult result)
        {
            if (feedbackText == null || result == null)
            {
                return;
            }

            if (result.GrantedLowSuspicionLetter)
            {
                feedbackText.text = $"{result.FactionId}: low suspicion letter received ({result.LowSuspicionLetterId})";
                return;
            }

            if (result.ActivatedPunishTask)
            {
                feedbackText.text = $"{result.FactionId}: high suspicion task activated ({result.PunishTaskId})";
            }
        }

        private FactionSuspicionRow CreateDefaultRow(Transform parent)
        {
            var rowObject = new GameObject("FactionSuspicionRow", typeof(RectTransform), typeof(Image));
            rowObject.transform.SetParent(parent, false);
            var rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(360f, 54f);

            var background = rowObject.GetComponent<Image>();
            background.color = new Color(0.16f, 0.16f, 0.15f, 0.92f);

            var nameText = CreateText("NameText", rowObject.transform, 16, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(nameText.rectTransform, new Vector2(14f, 0f), new Vector2(92f, 42f), new Vector2(0f, 0.5f));

            var iconImage = CreateImage("FactionIcon", rowObject.transform, new Color(1f, 1f, 1f, 1f));
            iconImage.enabled = false;
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

            var backgroundImage = CreateImage("Background", sliderObject.transform, new Color(0.08f, 0.08f, 0.08f, 1f));
            SetStretchRect(backgroundImage.rectTransform, Vector2.zero, Vector2.zero);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            SetStretchRect(fillArea.GetComponent<RectTransform>(), new Vector2(2f, 2f), new Vector2(-2f, -2f));

            var fillImage = CreateImage("Fill", fillArea.transform, new Color(0.74f, 0.22f, 0.18f, 1f));
            SetStretchRect(fillImage.rectTransform, Vector2.zero, Vector2.zero);

            var slider = sliderObject.GetComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            slider.fillRect = fillImage.rectTransform;
            slider.targetGraphic = backgroundImage;

            var row = rowObject.AddComponent<FactionSuspicionRow>();
            row.Configure(nameText, valueText, slider, iconImage, background, backgroundImage, fillImage);
            return row;
        }

        private Sprite FindFactionIcon(string factionId)
        {
            foreach (var binding in factionIcons)
            {
                if (binding != null && binding.FactionId == factionId)
                {
                    return binding.Icon;
                }
            }

            return null;
        }

        private void ClearRows()
        {
            foreach (var row in rows)
            {
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            rows.Clear();
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, int fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            return text;
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

        private static void SetRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
        {
            rectTransform.anchorMin = new Vector2(pivot.x, 0.5f);
            rectTransform.anchorMax = new Vector2(pivot.x, 0.5f);
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

        [Serializable]
        private sealed class FactionIconBinding
        {
            [SerializeField] private string factionId;
            [SerializeField] private Sprite icon;

            public string FactionId => factionId;

            public Sprite Icon => icon;
        }
    }
}
