using DG.Tweening;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class LetterRowView : MonoBehaviour
    {
        [Header("信件按钮：点击后打开信件内容")]
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text iconText;

        [Header("收到后的摆动：左右旋转角度与速度")]
        [SerializeField] private float swingMaxAngle = 10f;
        [SerializeField] private float swingDuration = 0.7f;

        private LetterAreaView owner;
        private string letterId;
        private RectTransform rectTransform;
        private Tween swingTween;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            rectTransform = transform as RectTransform;
        }

        private void OnDisable()
        {
            StopSwing();
        }

        private void OnDestroy()
        {
            StopSwing();
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
            StartSwing();
        }

        public void OnClicked()
        {
            if (!string.IsNullOrEmpty(letterId))
            {
                owner?.SelectLetter(letterId);
            }
        }

        private void StartSwing()
        {
            StopSwing();
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            if (rectTransform == null || swingMaxAngle <= 0f || swingDuration <= 0f)
            {
                return;
            }

            var angle = Mathf.Clamp(swingMaxAngle, 0f, 10f);
            rectTransform.localEulerAngles = new Vector3(0f, 0f, -angle);
            swingTween = rectTransform
                .DOLocalRotate(new Vector3(0f, 0f, angle), swingDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void StopSwing()
        {
            if (swingTween != null)
            {
                swingTween.Kill();
                swingTween = null;
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
