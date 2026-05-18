using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class FactionSuspicionRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Slider suspicionSlider;
        [SerializeField] private Image rowBackgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image sliderBackgroundImage;
        [SerializeField] private Image sliderFillImage;
        [SerializeField] private Sprite factionIcon;

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

                iconImage.enabled = iconImage.sprite != null;
                iconImage.preserveAspect = true;
            }
        }
    }
}
