using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class SharedActorSlotView : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text roleText;

        [Header("Layout")]
        [SerializeField] private RectTransform actorRoot;
        [SerializeField] private Vector2 hiddenPosition = new Vector2(-260f, 0f);
        [SerializeField] private Vector2 visiblePosition = Vector2.zero;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (actorRoot == null)
            {
                actorRoot = transform as RectTransform;
            }
        }

        public void ShowActor(string actorName, string role, Sprite portrait)
        {
            SetText(nameText, actorName);
            SetText(roleText, role);

            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.enabled = portrait != null;
            }

            SetVisible(true);
        }

        [ContextMenu("Show Test Actor")]
        public void ShowTestActor()
        {
            ShowActor("Presenter", "Shared actor slot", null);
        }

        [ContextMenu("Hide")]
        public void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool isVisible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = isVisible ? 1f : 0f;
                canvasGroup.blocksRaycasts = isVisible;
                canvasGroup.interactable = isVisible;
            }

            if (actorRoot != null)
            {
                actorRoot.anchoredPosition = isVisible ? visiblePosition : hiddenPosition;
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
