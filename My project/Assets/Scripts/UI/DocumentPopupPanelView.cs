using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class DocumentPopupPanelView : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text optionAText;
        [SerializeField] private TMP_Text optionBText;
        [SerializeField] private TMP_Text proposerFeedbackText;
        [SerializeField] private Image stampImage;

        [Header("Buttons")]
        [SerializeField] private Button optionAButton;
        [SerializeField] private Button optionBButton;

        private void Awake()
        {
            Hide();
        }

        [ContextMenu("Show Preview")]
        public void ShowPreview()
        {
            Show(
                "Document Preview",
                "This popup is the desk document frame. Document queue and result logic are added by the document system stage.",
                "Option A",
                "Option B");
        }

        public void Show(string title, string body, string optionA, string optionB)
        {
            SetText(titleText, title);
            SetText(bodyText, body);
            SetText(optionAText, optionA);
            SetText(optionBText, optionB);
            SetText(proposerFeedbackText, string.Empty);

            if (stampImage != null)
            {
                stampImage.enabled = false;
            }

            gameObject.SetActive(true);
        }

        [ContextMenu("Hide")]
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void OnOptionAClicked()
        {
            SetText(proposerFeedbackText, "Option A clicked. Document logic is not connected in this stage.");
        }

        public void OnOptionBClicked()
        {
            SetText(proposerFeedbackText, "Option B clicked. Document logic is not connected in this stage.");
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
