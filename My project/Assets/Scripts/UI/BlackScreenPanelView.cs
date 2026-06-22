using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class BlackScreenPanelView : MonoBehaviour
    {
        [Header("\u9ed1\u573a\u6de1\u5165\u6de1\u51fa\uff1a\u6839\u8282\u70b9\u900f\u660e\u63a7\u5236")]
        [Tooltip("\u9ed1\u573a\u9762\u677f\u6839\u8282\u70b9\u7684 CanvasGroup\uff1b\u4e3a\u7a7a\u65f6\u8fd0\u884c\u65f6\u81ea\u52a8\u4ece\u5f53\u524d\u7269\u4f53\u67e5\u627e\u6216\u8865\u9f50\uff0c\u7528\u4e8e\u63a7\u5236\u6574\u4f53\u900f\u660e\u5ea6\u548c\u70b9\u51fb\u62e6\u622a\u3002")]
        [SerializeField] private CanvasGroup rootCanvasGroup;

        [Header("\u9ed1\u573a\u6de1\u5165\u6de1\u51fa\uff1a\u80cc\u666f\u56fe\u7247")]
        [Tooltip("\u9ed1\u573a\u9762\u677f\u4e2d\u7684\u9ed1\u8272 Image\uff1b\u4e3a\u7a7a\u65f6\u8fd0\u884c\u65f6\u81ea\u52a8\u4ece\u5f53\u524d\u7269\u4f53\u67e5\u627e\uff0c\u7528\u4e8e\u4fdd\u8bc1\u6de1\u5165\u6de1\u51fa\u671f\u95f4\u663e\u793a\u7eaf\u9ed1\u5e95\u3002")]
        [SerializeField] private Image blackImage;

        [Header("\u9ed1\u573a\u6de1\u5165\u6de1\u51fa\uff1a\u9ed8\u8ba4\u65f6\u957f")]
        [Tooltip("\u6ca1\u6709\u4f20\u5165\u65f6\u957f\u65f6\u4f7f\u7528\u7684\u9ed8\u8ba4\u6de1\u5165\u6216\u6de1\u51fa\u65f6\u95f4\u3002\u6570\u503c\u8d8a\u5927\uff0c\u9ed1\u573a\u53d8\u5316\u8d8a\u6162\u3002")]
        [SerializeField, Min(0f)] private float defaultFadeDuration = 0.45f;

        [Header("\u9ed1\u573a\u6587\u672c\uff1a\u590d\u7528\u9884\u5236\u4f53\u65c1\u767d TMP")]
        [Tooltip("\u9ed1\u573a\u9762\u677f\u4e2d\u5df2\u7ecf\u521b\u5efa\u597d\u7684\u201c\u65c1\u767d\u201d TMP\uff1b\u4e3a\u7a7a\u65f6\u8fd0\u884c\u65f6\u53ea\u67e5\u627e\u540d\u4e3a\u201c\u65c1\u767d\u201d\u7684 TMP\uff0c\u4e0d\u65b0\u5efa\u7269\u4f53\u3001\u4e0d\u6539\u5b57\u4f53\u3001\u4e0d\u6539\u989c\u8272\u3002")]
        [SerializeField] private TMP_Text narrationBodyText;
        [Tooltip("\u9ed1\u573a\u65c1\u767d\u6253\u5b57\u673a\u901f\u5ea6\uff1b\u6bcf\u79d2\u663e\u793a\u5b57\u7b26\u6570\u3002")]
        [SerializeField, Min(1f)] private float narrationCharactersPerSecond = 38f;
        [Tooltip("\u6bcf\u53e5\u65c1\u767d\u64ad\u5b8c\u540e\u7ee7\u7eed\u4e0b\u4e00\u53e5\u524d\u7684\u505c\u987f\u65f6\u95f4\u3002")]
        [SerializeField, Min(0f)] private float narrationLineHoldDuration = 0.35f;
        [Tooltip("\u6240\u6709\u9ed1\u573a\u65c1\u767d\u6587\u672c\u64ad\u653e\u5b8c\u6210\u540e\uff0c\u5728\u6de1\u51fa\u9000\u51fa\u9ed1\u573a\u524d\u7ee7\u7eed\u505c\u7559\u7684\u65f6\u95f4\uff0c\u7528\u4e8e\u8ba9\u73a9\u5bb6\u770b\u5b8c\u6700\u540e\u4e00\u53e5\u3002")]
        [SerializeField, Min(0f)] private float narrationCompleteHoldDuration = 2f;

        public float DefaultFadeDuration => defaultFadeDuration;

        private void Awake()
        {
            ResolveReferences();
            SetAlpha(0f);
            SetRaycastEnabled(false);
        }

        public IEnumerator FadeIn(float duration = -1f)
        {
            yield return FadeTo(1f, ResolveDuration(duration));
        }

        public IEnumerator FadeOut(float duration = -1f)
        {
            yield return FadeTo(0f, ResolveDuration(duration));
        }

        public IEnumerator PlayNarrationLines(IReadOnlyList<string> lines, float charactersPerSecond = -1f)
        {
            ResolveReferences();
            ResolveNarrationText();
            gameObject.SetActive(true);
            SetRaycastEnabled(true);
            SetAlpha(1f);
            SetNarrationVisible(true);

            var safeSpeed = charactersPerSecond > 0f ? charactersPerSecond : narrationCharactersPerSecond;
            var accumulatedText = string.Empty;
            for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                var line = lines[lineIndex] ?? string.Empty;
                var prefix = accumulatedText;
                var elapsedCharacters = 0f;
                while (elapsedCharacters < line.Length)
                {
                    elapsedCharacters += Time.unscaledDeltaTime * Mathf.Max(1f, safeSpeed);
                    var visibleCount = Mathf.Clamp(Mathf.FloorToInt(elapsedCharacters), 0, line.Length);
                    if (narrationBodyText != null)
                    {
                        narrationBodyText.text = prefix + line.Substring(0, visibleCount);
                    }
                    yield return null;
                }

                accumulatedText += line;
                if (lineIndex < lines.Count - 1)
                {
                    accumulatedText += "\n";
                    if (narrationBodyText != null)
                    {
                        narrationBodyText.text = accumulatedText;
                    }
                    if (narrationLineHoldDuration > 0f)
                    {
                        yield return new WaitForSecondsRealtime(narrationLineHoldDuration);
                    }
                }
            }

            if (narrationCompleteHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(narrationCompleteHoldDuration);
            }
        }

        public void ClearNarration()
        {
            ResolveReferences();
            SetNarrationVisible(false);
            if (narrationBodyText != null)
            {
                narrationBodyText.text = string.Empty;
            }
        }

        public void SetVisibleInstant(bool visible)
        {
            ResolveReferences();
            SetAlpha(visible ? 1f : 0f);
            SetRaycastEnabled(visible);
        }

        public IEnumerator FadeTo(float targetAlpha, float duration)
        {
            ResolveReferences();
            gameObject.SetActive(true);
            SetRaycastEnabled(true);

            var startAlpha = rootCanvasGroup != null ? rootCanvasGroup.alpha : 0f;
            var safeDuration = Mathf.Max(0f, duration);
            if (safeDuration <= 0f)
            {
                SetAlpha(targetAlpha);
                SetRaycastEnabled(targetAlpha > 0.001f);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / safeDuration)));
                yield return null;
            }

            SetAlpha(targetAlpha);
            SetRaycastEnabled(targetAlpha > 0.001f);
            if (targetAlpha <= 0.001f)
            {
                ClearNarration();
            }
        }

        private void ResolveReferences()
        {
            if (rootCanvasGroup == null)
            {
                rootCanvasGroup = GetComponent<CanvasGroup>();
            }

            if (rootCanvasGroup == null)
            {
                rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (blackImage == null)
            {
                blackImage = GetComponent<Image>();
            }

            if (blackImage != null)
            {
                blackImage.color = Color.black;
            }

            SetNarrationVisible(false);
        }

        private void ResolveNarrationText()
        {
            if (narrationBodyText != null)
            {
                return;
            }

            foreach (var candidate in GetComponentsInChildren<TMP_Text>(true))
            {
                if (candidate != null && candidate.gameObject.name == "\u65c1\u767d")
                {
                    narrationBodyText = candidate;
                    return;
                }
            }

            Debug.LogWarning("\u9ed1\u573a\u9762\u677f\u672a\u627e\u5230\u540d\u4e3a\u201c\u65c1\u767d\u201d\u7684 TMP\uff0c\u9ed1\u573a\u5267\u60c5\u65c1\u767d\u65e0\u6cd5\u64ad\u653e\u3002", this);
        }

        private void SetNarrationVisible(bool visible)
        {
            if (narrationBodyText != null)
            {
                narrationBodyText.gameObject.SetActive(visible);
            }
        }

        private float ResolveDuration(float duration)
        {
            return duration >= 0f ? duration : defaultFadeDuration;
        }

        private void SetAlpha(float alpha)
        {
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = Mathf.Clamp01(alpha);
            }

            if (blackImage != null)
            {
                var color = blackImage.color;
                color.a = Mathf.Clamp01(alpha);
                blackImage.color = color;
            }
        }

        private void SetRaycastEnabled(bool enabled)
        {
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.blocksRaycasts = enabled;
                rootCanvasGroup.interactable = enabled;
            }

            if (blackImage != null)
            {
                blackImage.raycastTarget = enabled;
            }
        }
    }
}
