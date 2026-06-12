using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TwelveMoons.UI
{
    [DisallowMultipleComponent]
    public sealed class DeskBadgeHoverShake : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("徽章悬停抖动")]
        [Tooltip("是否允许鼠标悬停时循环抖动。关闭后徽章保持静止。")]
        [SerializeField] private bool enableHoverShake = true;

        [Tooltip("每次抖动循环的时长，值越小抖动越急促。")]
        [SerializeField, Min(0.05f)] private float shakeDuration = 0.42f;

        [Tooltip("抖动旋转角度，值越大晃动越明显。")]
        [SerializeField, Range(0f, 20f)] private float shakeRotationStrength = 5.5f;

        [Tooltip("每次循环中的抖动次数，值越大越碎。")]
        [SerializeField, Range(1, 30)] private int shakeVibrato = 12;

        [Tooltip("抖动随机度，值越大越不规则。")]
        [SerializeField, Range(0f, 90f)] private float shakeRandomness = 18f;

        [Header("运行时调试快照")]
        [Tooltip("当前鼠标是否停在徽章上，只读快照。")]
        [SerializeField] private bool isPointerInsideSnapshot;

        [Tooltip("当前循环抖动动画是否正在播放，只读快照。")]
        [SerializeField] private bool isShakingSnapshot;

        private RectTransform rectTransform;
        private Quaternion originalLocalRotation;
        private Tweener shakeTween;

        public bool IsPointerInside => isPointerInsideSnapshot;
        public bool IsShaking => isShakingSnapshot;

        private void Awake()
        {
            ResolveRectTransform();
        }

        private void OnEnable()
        {
            ResolveRectTransform();
            originalLocalRotation = rectTransform.localRotation;
        }

        private void OnDisable()
        {
            StopShake();
            isPointerInsideSnapshot = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInsideSnapshot = true;
            StartShake();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInsideSnapshot = false;
            StopShake();
        }

        private void StartShake()
        {
            if (!enableHoverShake)
            {
                return;
            }

            ResolveRectTransform();
            if (shakeTween != null && shakeTween.IsActive())
            {
                return;
            }

            originalLocalRotation = rectTransform.localRotation;
            shakeTween = rectTransform
                .DOShakeRotation(shakeDuration, new Vector3(0f, 0f, shakeRotationStrength), shakeVibrato, shakeRandomness)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetLoops(-1, LoopType.Restart);
            isShakingSnapshot = true;
        }

        private void StopShake()
        {
            if (shakeTween != null && shakeTween.IsActive())
            {
                shakeTween.Kill();
            }

            shakeTween = null;
            isShakingSnapshot = false;

            if (rectTransform != null)
            {
                rectTransform.localRotation = originalLocalRotation;
            }
        }

        private void ResolveRectTransform()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }
        }
    }
}
