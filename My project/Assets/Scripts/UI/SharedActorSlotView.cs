using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class SharedActorSlotView : MonoBehaviour
    {
        [Header("人物显示：占位立绘、姓名和身份文本")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private Color placeholderPortraitColor = new Color(0.42f, 0.45f, 0.5f, 1f);

        [Header("人物框移动：以当前摆放位置为显示位置")]
        [SerializeField] private RectTransform actorRoot;
        [SerializeField] private float hiddenMoveLeftDistance = 260f;
        [SerializeField] private float slideDuration = 0.8f;

        private Vector2 visiblePosition;

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

            visiblePosition = actorRoot != null ? actorRoot.anchoredPosition : Vector2.zero;
            SetVisible(false, true);
        }

        public void ShowActor(string actorName, string role, Sprite portrait)
        {
            SetText(nameText, actorName);
            SetText(roleText, role);

            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.color = portrait != null ? Color.white : placeholderPortraitColor;
                portraitImage.enabled = true;
            }

            SetVisible(true, false);
        }

        [ContextMenu("Show Test Actor")]
        public void ShowTestActor()
        {
            ShowActor("Presenter", "Shared actor slot", null);
        }

        [ContextMenu("Hide")]
        public void Hide()
        {
            SetVisible(false, false);
        }

        public void HideToRight()
        {
            Hide();
        }

        private void SetVisible(bool isVisible, bool immediate)
        {
            var hiddenPosition = GetHiddenLeftPosition();
            var targetPosition = isVisible ? visiblePosition : hiddenPosition;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = isVisible;
                canvasGroup.interactable = isVisible;
                canvasGroup.DOKill();
                canvasGroup.alpha = isVisible || !immediate ? 1f : 0f;
            }

            if (actorRoot != null)
            {
                actorRoot.DOKill();
                if (isVisible)
                {
                    actorRoot.anchoredPosition = hiddenPosition;
                }

                if (immediate || slideDuration <= 0f)
                {
                    actorRoot.anchoredPosition = targetPosition;
                }
                else
                {
                    var tween = actorRoot.DOAnchorPos(targetPosition, slideDuration);
                    if (!isVisible && canvasGroup != null)
                    {
                        tween.OnComplete(() => canvasGroup.alpha = 0f);
                    }
                }
            }
        }

        private Vector2 GetHiddenLeftPosition()
        {
            return visiblePosition + (Vector2.left * hiddenMoveLeftDistance);
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
