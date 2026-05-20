using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class LetterRowView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text iconText;

        private LetterAreaView owner;
        private string letterId;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        public void Configure(TMP_Text icon, Button rowButton)
        {
            iconText = icon;
            button = rowButton;
        }

        public void Bind(LetterAreaView areaView, LetterDefinition definition, RuntimeLetterState state)
        {
            owner = areaView;
            letterId = state != null ? state.LetterId : string.Empty;
            SetText(iconText, state != null && state.IsRead ? "信" : "新");
        }

        public void OnClicked()
        {
            if (!string.IsNullOrEmpty(letterId))
            {
                owner?.SelectLetter(letterId);
            }
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
