using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class FactionSuspicionRow : MonoBehaviour
    {
        [Header("阵营标识：用于把表格中的阵营绑定到这一行")]
        [SerializeField] private string factionId;

        [Header("阵营显示：名称、数值、滑条和图标")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Slider suspicionSlider;
        [SerializeField] private Image rowBackgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image sliderBackgroundImage;
        [SerializeField] private Image sliderFillImage;
        [SerializeField] private Sprite factionIcon;

        public string FactionId => factionId;

        public RectTransform RectTransform => transform as RectTransform;

        public void SetFactionId(string value)
        {
            factionId = value ?? string.Empty;
        }

        public void Configure(
            TMP_Text factionNameText,
            TMP_Text suspicionValueText,
            Slider slider,
            Image factionIconImage = null,
            Image rowBackground = null,
            Image sliderBackground = null,
            Image sliderFill = null)
        {
            nameText = factionNameText;
            valueText = suspicionValueText;
            suspicionSlider = slider;
            iconImage = factionIconImage;
            rowBackgroundImage = rowBackground;
            sliderBackgroundImage = sliderBackground;
            sliderFillImage = sliderFill;
        }

        public void Bind(FactionDefinition definition, RuntimeFactionState state, Sprite iconOverride = null)
        {
            if (definition != null && string.IsNullOrEmpty(factionId))
            {
                factionId = definition.FactionId;
            }

            if (nameText != null)
            {
                nameText.text = string.IsNullOrEmpty(definition.FactionName)
                    ? definition.FactionId
                    : definition.FactionName;
            }

            if (valueText != null)
            {
                valueText.text = $"{state.Suspicion}/{definition.MaxSuspicion}";
            }

            if (suspicionSlider != null)
            {
                suspicionSlider.minValue = 0;
                suspicionSlider.maxValue = Mathf.Max(1, definition.MaxSuspicion);
                suspicionSlider.value = Mathf.Clamp(state.Suspicion, 0, definition.MaxSuspicion);
            }

            if (iconImage != null)
            {
                if (iconOverride != null)
                {
                    iconImage.sprite = iconOverride;
                }
                else if (factionIcon != null)
                {
                    iconImage.sprite = factionIcon;
                }

                iconImage.enabled = true;
                iconImage.preserveAspect = iconImage.sprite != null;
            }
        }
    }
}
