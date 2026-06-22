using TMPro;
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class SharedActorSlotView : MonoBehaviour, IPointerClickHandler
    {
        [Header("人物显示：占位立绘、姓名和身份文本")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text roleText;
        [Tooltip("提出者反馈背景；做出公文选择前隐藏，显示反馈时开启，公文合上时再次隐藏。")]
        [SerializeField] private GameObject proposerFeedbackBackground;
        [SerializeField] private TMP_Text proposerFeedbackText;
        [SerializeField] private Color placeholderPortraitColor = new Color(0.42f, 0.45f, 0.5f, 1f);
        [Header("点击区域：公文前剧情只允许点击人物框触发")]
        [Tooltip("透明点击接收图；如果没有手动指定，会在人物框根节点自动补一个透明 Image，不改变可见布局。")]
        [SerializeField] private Image clickRaycastImage;

        [Header("人物框移动：以当前摆放位置为显示位置")]
        [Header("可视裁剪区域")]
        [Tooltip("固定不动的角色可视区域；角色立绘滑入前和滑出后会被这个区域裁掉。")]
        [SerializeField] private RectTransform visibleClipRoot;
        [Tooltip("可视裁剪区域上的 RectMask2D；用于限制角色立绘只在指定区域内显示。")]
        [SerializeField] private RectMask2D visibleClipMask;

        [Header("角色移动设置")]
        [Tooltip("实际执行滑入滑出的角色根节点；应作为可视裁剪区域的子物体，不能再指向 SharedActorSlot 自己。")]
        [SerializeField] private RectTransform actorRoot;
        [Tooltip("角色隐藏时相对显示位置向左移动的距离。")]
        [SerializeField] private float hiddenMoveLeftDistance = 260f;
        [Header("\u53f3\u4fa7\u9000\u573a\u8bbe\u7f6e")]
        [Tooltip("\u89d2\u8272\u5411\u53f3\u9000\u573a\u65f6\u7684\u79fb\u52a8\u8ddd\u79bb\uff1b\u5c0f\u4e8e\u7b49\u4e8e 0 \u65f6\u4f1a\u81ea\u52a8\u4f7f\u7528\u5de6\u4fa7\u9690\u85cf\u8ddd\u79bb\u3002")]
        [SerializeField] private float hiddenMoveRightDistance = 260f;
        [Tooltip("角色滑入或滑出的动画时长。")]
        [SerializeField] private float slideDuration = 0.8f;
        [Header("角色移动曲线：滑入滑出先快后慢")]
        [Tooltip("角色滑入和滑出时使用的缓动曲线。默认 OutCubic，表现为先快后慢。")]
        [SerializeField] private Ease slideEase = Ease.OutCubic;

        private Vector2 visiblePosition;
        private Tween feedbackTypewriterTween;
        private string feedbackTypewriterFullText = string.Empty;
        private Action feedbackTypewriterCompleteCallback;
        private bool isFeedbackTypewriterPlaying;

        public event Action Clicked;

        public bool IsFeedbackTypewriterPlaying => isFeedbackTypewriterPlaying;

        public float HiddenMoveLeftDistance => hiddenMoveLeftDistance;

        public float SlideDuration => slideDuration;

        private void Awake()
        {
            ApplyRuntimeMotionDefaults();

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (actorRoot == null)
            {
                actorRoot = transform as RectTransform;
            }

            ResolveClipReferences();

            if (proposerFeedbackText == null)
            {
                var feedbackTransform = FindFirstExistingTransform(
                    "ActorRoot/ProposerFeedbackBackground/ProposerFeedbackText",
                    "角色根节点/提出者反馈背景/提出者反馈文本",
                    "ProposerFeedbackBackground/ProposerFeedbackText",
                    "提出者反馈背景/提出者反馈文本",
                    "ProposerFeedbackText",
                    "提出者反馈文本");
                proposerFeedbackText = feedbackTransform != null
                    ? feedbackTransform.GetComponent<TMP_Text>()
                    : null;
            }

            if (proposerFeedbackBackground == null)
            {
                var feedbackBackgroundTransform = FindFirstExistingTransform(
                    "ActorRoot/ProposerFeedbackBackground",
                    "角色根节点/提出者反馈背景",
                    "ProposerFeedbackBackground",
                    "提出者反馈背景");
                proposerFeedbackBackground = feedbackBackgroundTransform != null
                    ? feedbackBackgroundTransform.gameObject
                    : null;
            }

            visiblePosition = actorRoot != null ? actorRoot.anchoredPosition : Vector2.zero;
            ConfigureClickRaycast();
            ClearFeedback();
            SetVisible(false, true);
        }

        private void ApplyRuntimeMotionDefaults()
        {
            hiddenMoveLeftDistance = Mathf.Max(hiddenMoveLeftDistance, 560f);
            slideDuration = Mathf.Min(slideDuration, 0.35f);
        }

        public void ShowActor(string actorName, string role, Sprite portrait)
        {
            ShowActor(actorName, role, portrait, null);
        }

        public void ShowActor(string actorName, string role, Sprite portrait, Action onComplete)
        {
            SetText(nameText, actorName);
            SetText(roleText, role);

            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.color = portrait != null ? Color.white : placeholderPortraitColor;
                portraitImage.enabled = true;

                // 切换到新精灵后恢复原生尺寸，避免因上一张图的拉伸导致比例异常。
                if (portrait != null)
                {
                    portraitImage.SetNativeSize();
                }
            }

            SetVisible(true, false, false, onComplete);
        }

        public void ShowFeedback(string feedback)
        {
            KillFeedbackTypewriter();
            SetText(proposerFeedbackText, feedback);
            SetFeedbackBackgroundVisible(!string.IsNullOrEmpty(feedback));
        }

        public void ShowFeedbackTypewriter(string feedback, float charactersPerSecond, Action onComplete)
        {
            KillFeedbackTypewriter();
            feedbackTypewriterFullText = feedback ?? string.Empty;
            feedbackTypewriterCompleteCallback = onComplete;
            SetFeedbackBackgroundVisible(!string.IsNullOrEmpty(feedbackTypewriterFullText));

            if (proposerFeedbackText == null || string.IsNullOrEmpty(feedbackTypewriterFullText) || charactersPerSecond <= 0f)
            {
                SetText(proposerFeedbackText, feedbackTypewriterFullText);
                CompleteFeedbackTypewriter();
                return;
            }

            proposerFeedbackText.text = feedbackTypewriterFullText;
            proposerFeedbackText.maxVisibleCharacters = 0;
            isFeedbackTypewriterPlaying = true;
            var visibleCount = feedbackTypewriterFullText.Length;
            var duration = visibleCount / Mathf.Max(1f, charactersPerSecond);
            feedbackTypewriterTween = DOTween
                .To(
                    () => proposerFeedbackText.maxVisibleCharacters,
                    value => proposerFeedbackText.maxVisibleCharacters = value,
                    visibleCount,
                    duration)
                .SetEase(Ease.Linear)
                .OnComplete(CompleteFeedbackTypewriter);
        }

        public void CompleteFeedbackTypewriter()
        {
            feedbackTypewriterTween?.Kill();
            feedbackTypewriterTween = null;

            if (proposerFeedbackText != null)
            {
                proposerFeedbackText.text = feedbackTypewriterFullText;
                proposerFeedbackText.maxVisibleCharacters = int.MaxValue;
            }

            if (!isFeedbackTypewriterPlaying && feedbackTypewriterCompleteCallback == null)
            {
                return;
            }

            isFeedbackTypewriterPlaying = false;
            var callback = feedbackTypewriterCompleteCallback;
            feedbackTypewriterCompleteCallback = null;
            callback?.Invoke();
        }

        public void ClearFeedback()
        {
            KillFeedbackTypewriter();
            SetText(proposerFeedbackText, string.Empty);
            SetFeedbackBackgroundVisible(false);
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
            SetVisible(false, false, true);
        }

        public void HideAlongEntryPath(Action onComplete)
        {
            SetVisible(false, false, false, onComplete);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button == PointerEventData.InputButton.Left)
            {
                Clicked?.Invoke();
            }
        }

        private void SetVisible(bool isVisible, bool immediate)
        {
            SetVisible(isVisible, immediate, false, null);
        }

        private void SetVisible(bool isVisible, bool immediate, bool hideToRight)
        {
            SetVisible(isVisible, immediate, hideToRight, null);
        }

        private void SetVisible(bool isVisible, bool immediate, bool hideToRight, Action onComplete)
        {
            var hiddenPosition = hideToRight ? GetHiddenRightPosition() : GetHiddenLeftPosition();
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
                    if (!isVisible && canvasGroup != null)
                    {
                        canvasGroup.alpha = 0f;
                    }
                    onComplete?.Invoke();
                }
                else
                {
                    var tween = actorRoot.DOAnchorPos(targetPosition, slideDuration).SetEase(slideEase);
                    tween.OnComplete(() =>
                    {
                        if (!isVisible && canvasGroup != null)
                        {
                            canvasGroup.alpha = 0f;
                        }
                        onComplete?.Invoke();
                    });
                }
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private Vector2 GetHiddenLeftPosition()
        {
            return visiblePosition + (Vector2.left * hiddenMoveLeftDistance);
        }

        private Vector2 GetHiddenRightPosition()
        {
            var distance = hiddenMoveRightDistance > 0f ? hiddenMoveRightDistance : hiddenMoveLeftDistance;
            return visiblePosition + (Vector2.right * distance);
        }

        private void ConfigureClickRaycast()
        {
            if (clickRaycastImage == null)
            {
                var target = actorRoot != null ? actorRoot.gameObject : gameObject;
                clickRaycastImage = target.GetComponent<Image>();
                if (clickRaycastImage == null)
                {
                    clickRaycastImage = target.AddComponent<Image>();
                    clickRaycastImage.color = new Color(1f, 1f, 1f, 0f);
                }
            }

            clickRaycastImage.raycastTarget = true;
            if (portraitImage != null)
            {
                portraitImage.raycastTarget = false;
            }
        }

        private void ResolveClipReferences()
        {
            if (visibleClipRoot == null)
            {
                visibleClipRoot = transform as RectTransform;
            }

            if (visibleClipMask == null && visibleClipRoot != null)
            {
                visibleClipMask = visibleClipRoot.GetComponent<RectMask2D>();
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
                target.maxVisibleCharacters = int.MaxValue;
            }
        }

        private void KillFeedbackTypewriter()
        {
            feedbackTypewriterTween?.Kill();
            feedbackTypewriterTween = null;
            feedbackTypewriterCompleteCallback = null;
            isFeedbackTypewriterPlaying = false;
        }

        private void SetFeedbackBackgroundVisible(bool visible)
        {
            if (proposerFeedbackBackground != null)
            {
                proposerFeedbackBackground.SetActive(visible);
            }
        }

        private Transform FindFirstExistingTransform(params string[] candidatePaths)
        {
            foreach (var candidatePath in candidatePaths)
            {
                if (string.IsNullOrWhiteSpace(candidatePath))
                {
                    continue;
                }

                var result = transform.Find(candidatePath);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
