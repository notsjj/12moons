using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class LetterDebugControls : MonoBehaviour
    {
        [SerializeField] private LetterService letterService;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private string demoLetterIdA = "letter_relief_start";
        [SerializeField] private string demoLetterIdB = "letter_relief_prepare_end";
        [SerializeField] private string demoLetterIdC = "letter_relief_deliver_start";

        private void Awake()
        {
            if (letterService == null)
            {
                letterService = FindFirstObjectByType<LetterService>();
            }
        }

        public void ReceiveDemoLetterA()
        {
            ReceiveLetter(demoLetterIdA);
        }

        public void ReceiveDemoLetterB()
        {
            ReceiveLetter(demoLetterIdB);
        }

        public void ReceiveDemoLetterC()
        {
            ReceiveLetter(demoLetterIdC);
        }

        public void RefreshLetters()
        {
            letterService?.Refresh();
            SetFeedback("Letter config refreshed.");
        }

        private void ReceiveLetter(string letterId)
        {
            if (letterService != null && letterService.ReceiveLetter(letterId) != null)
            {
                SetFeedback($"Received letter {letterId}.");
                return;
            }

            SetFeedback($"Cannot receive letter {letterId}.");
        }

        private void SetFeedback(string value)
        {
            if (feedbackText != null)
            {
                feedbackText.text = value;
            }
        }
    }
}
