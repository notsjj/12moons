using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    [DisallowMultipleComponent]
    public sealed class LoadingPanelTransitionView : MonoBehaviour
    {
        private sealed class LayerState
        {
            public RectTransform RectTransform;
            public Vector2 CoveredPosition;
            public Vector2 OffscreenPosition;
        }

        private sealed class LayerGroupState
        {
            public readonly List<LayerState> Layers = new List<LayerState>(2);
        }

        [Header("成组滑动过场")]
        [Tooltip("每一组图层从屏幕外滑入或原路滑出的持续时间。")]
        [SerializeField, Min(0.01f)] private float groupSlideDuration = 0.65f;

        [Tooltip("相邻两组开始滑动的时间间隔。排序靠前的组先滑入，排序靠后的组先滑出。")]
        [SerializeField, Min(0f)] private float groupInterval = 0.12f;

        [Tooltip("所有图层覆盖屏幕后停顿的时间。停顿结束时执行场景切换回调。")]
        [SerializeField, Min(0f)] private float coveredHoldDuration = 1f;

        [Tooltip("图层完全移出屏幕后继续保留的额外距离，避免屏幕边缘残留。")]
        [SerializeField, Min(0f)] private float offscreenPadding = 80f;

        [Tooltip("图层滑入屏幕时使用的缓动类型。")]
        [SerializeField] private Ease enterEase = Ease.OutCubic;

        [Tooltip("图层原路滑出屏幕时使用的缓动类型。")]
        [SerializeField] private Ease exitEase = Ease.InCubic;

        [Header("运行时调试快照")]
        [Tooltip("运行时自动识别到的左右图片图层总数。")]
        [SerializeField] private int resolvedLayerCount;

        [Tooltip("运行时识别到的左侧图片图层数量。")]
        [SerializeField] private int resolvedLeftLayerCount;

        [Tooltip("运行时识别到的右侧图片图层数量。")]
        [SerializeField] private int resolvedRightLayerCount;

        [Tooltip("按左右 Hierarchy 顺序配对后得到的动画组数量。")]
        [SerializeField] private int resolvedGroupCount;

        [Tooltip("图层组滑入顺序快照。数字代表配对后的组索引。")]
        [SerializeField] private string enterGroupOrderSnapshot;

        [Tooltip("图层组滑出顺序快照。数字代表配对后的组索引。")]
        [SerializeField] private string exitGroupOrderSnapshot;

        [Tooltip("当前是否正在播放 LoadingPanel 过场动画。")]
        [SerializeField] private bool isPlayingSnapshot;

        private readonly List<LayerState> layerStates = new List<LayerState>();
        private readonly List<LayerGroupState> layerGroups = new List<LayerGroupState>();
        private CanvasGroup canvasGroup;
        private Sequence playingSequence;
        private bool isInitialized;

        public int ResolvedLayerCount => resolvedLayerCount;
        public int ResolvedLeftLayerCount => resolvedLeftLayerCount;
        public int ResolvedRightLayerCount => resolvedRightLayerCount;
        public int ResolvedGroupCount => resolvedGroupCount;
        public string EnterGroupOrderSnapshot => enterGroupOrderSnapshot;
        public string ExitGroupOrderSnapshot => exitGroupOrderSnapshot;
        public bool IsPlayingTransition => isPlayingSnapshot;
        public float CloseDuration => GetPhaseDuration();
        public float CoveredHoldDuration => coveredHoldDuration;
        public float OpenDuration => GetPhaseDuration();

        private void Awake()
        {
            EnsureInitialized();
            ApplyCoveredState();
            SetVisible(false);
        }

        private void OnDisable()
        {
            KillPlayingSequence();
            if (isInitialized)
            {
                ApplyCoveredState();
            }
        }

        public void EditorForceInitialize()
        {
            isInitialized = false;
            EnsureInitialized();
        }

        public void PlayDebugTransition()
        {
            PlayEnterCityTransition(null, () => SetVisible(false));
        }

        public void EditorPreviewBegin()
        {
            isInitialized = false;
            EnsureInitialized();
            isPlayingSnapshot = true;
            SetVisible(true);
            ApplyOffscreenState();
        }

        public void EditorPreviewSampleClose(float progress)
        {
            EnsureInitialized();
            ApplyEnterSample(Mathf.Clamp01(progress));
        }

        public void EditorPreviewHoldCovered()
        {
            EnsureInitialized();
            ApplyCoveredState();
        }

        public void EditorPreviewSampleOpen(float progress)
        {
            EnsureInitialized();
            ApplyExitSample(Mathf.Clamp01(progress));
        }

        public void EditorPreviewEnd()
        {
            isPlayingSnapshot = false;
            ApplyCoveredState();
            SetVisible(false);
        }

        public void PlayEnterCityTransition(Action onCovered, Action onCompleted)
        {
            KillPlayingSequence();
            if (isInitialized)
            {
                ApplyCoveredState();
            }

            isInitialized = false;
            EnsureInitialized();

            isPlayingSnapshot = true;
            SetVisible(true);
            ApplyOffscreenState();

            playingSequence = DOTween.Sequence().SetUpdate(true);
            playingSequence.Append(BuildEnterSequence());
            playingSequence.AppendInterval(Mathf.Max(0f, coveredHoldDuration));
            playingSequence.AppendCallback(() => onCovered?.Invoke());
            playingSequence.Append(BuildExitSequence());
            playingSequence.OnComplete(() =>
            {
                playingSequence = null;
                isPlayingSnapshot = false;
                ApplyCoveredState();
                SetVisible(false);
                onCompleted?.Invoke();
            });
        }

        private Sequence BuildEnterSequence()
        {
            var sequence = DOTween.Sequence();
            for (var groupIndex = 0; groupIndex < layerGroups.Count; groupIndex++)
            {
                InsertGroupTweens(
                    sequence,
                    layerGroups[groupIndex],
                    groupIndex * Mathf.Max(0f, groupInterval),
                    true);
            }

            return sequence;
        }

        private Sequence BuildExitSequence()
        {
            var sequence = DOTween.Sequence();
            for (var orderIndex = 0; orderIndex < layerGroups.Count; orderIndex++)
            {
                var groupIndex = layerGroups.Count - 1 - orderIndex;
                InsertGroupTweens(
                    sequence,
                    layerGroups[groupIndex],
                    orderIndex * Mathf.Max(0f, groupInterval),
                    false);
            }

            return sequence;
        }

        private void InsertGroupTweens(Sequence sequence, LayerGroupState group, float startTime, bool isEntering)
        {
            foreach (var layer in group.Layers)
            {
                if (layer?.RectTransform == null)
                {
                    continue;
                }

                var target = isEntering ? layer.CoveredPosition : layer.OffscreenPosition;
                var ease = isEntering ? enterEase : exitEase;
                sequence.Insert(
                    startTime,
                    layer.RectTransform
                        .DOAnchorPos(target, Mathf.Max(0.01f, groupSlideDuration))
                        .SetEase(ease));
            }
        }

        private void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            layerStates.Clear();
            layerGroups.Clear();

            var leftLayers = FindDirectImageChildren("左侧图层");
            var rightLayers = FindDirectImageChildren("右侧图层");
            resolvedLeftLayerCount = leftLayers.Count;
            resolvedRightLayerCount = rightLayers.Count;
            resolvedLayerCount = resolvedLeftLayerCount + resolvedRightLayerCount;

            var groupCount = Mathf.Max(leftLayers.Count, rightLayers.Count);
            for (var groupIndex = 0; groupIndex < groupCount; groupIndex++)
            {
                var group = new LayerGroupState();
                if (groupIndex < leftLayers.Count)
                {
                    AddLayerToGroup(group, leftLayers[groupIndex], -1f);
                }

                if (groupIndex < rightLayers.Count)
                {
                    AddLayerToGroup(group, rightLayers[groupIndex], 1f);
                }

                layerGroups.Add(group);
            }

            resolvedGroupCount = layerGroups.Count;
            enterGroupOrderSnapshot = BuildOrderSnapshot(false);
            exitGroupOrderSnapshot = BuildOrderSnapshot(true);
            isInitialized = true;
        }

        private List<RectTransform> FindDirectImageChildren(string containerName)
        {
            var result = new List<RectTransform>();
            var container = transform.Find(containerName);
            if (container == null)
            {
                return result;
            }

            for (var childIndex = 0; childIndex < container.childCount; childIndex++)
            {
                var child = container.GetChild(childIndex) as RectTransform;
                if (child != null && child.GetComponent<Image>() != null)
                {
                    result.Add(child);
                }
            }

            return result;
        }

        private void AddLayerToGroup(LayerGroupState group, RectTransform rectTransform, float directionSign)
        {
            var state = new LayerState
            {
                RectTransform = rectTransform,
                CoveredPosition = rectTransform.anchoredPosition
            };
            state.OffscreenPosition = GetOffscreenPosition(state, directionSign);
            group.Layers.Add(state);
            layerStates.Add(state);
        }

        private Vector2 GetOffscreenPosition(LayerState layer, float directionSign)
        {
            var rootRect = transform as RectTransform;
            if (rootRect == null || layer?.RectTransform == null)
            {
                return layer?.CoveredPosition ?? Vector2.zero;
            }

            var rect = layer.RectTransform;
            var width = Mathf.Abs(rect.rect.width * rect.localScale.x);
            var rootLocalX = directionSign < 0f
                ? rootRect.rect.xMin - width * (1f - rect.pivot.x) - offscreenPadding
                : rootRect.rect.xMax + width * rect.pivot.x + offscreenPadding;
            var worldPoint = rootRect.TransformPoint(new Vector3(rootLocalX, layer.CoveredPosition.y, 0f));
            var parentLocalPoint = rect.parent.InverseTransformPoint(worldPoint);
            return new Vector2(parentLocalPoint.x, layer.CoveredPosition.y);
        }

        private void ApplyOffscreenState()
        {
            foreach (var layer in layerStates)
            {
                if (layer?.RectTransform != null)
                {
                    layer.RectTransform.anchoredPosition = layer.OffscreenPosition;
                }
            }
        }

        private void ApplyCoveredState()
        {
            foreach (var layer in layerStates)
            {
                if (layer?.RectTransform != null)
                {
                    layer.RectTransform.anchoredPosition = layer.CoveredPosition;
                }
            }
        }

        private void ApplyEnterSample(float progress)
        {
            var elapsed = progress * GetPhaseDuration();
            for (var groupIndex = 0; groupIndex < layerGroups.Count; groupIndex++)
            {
                var localProgress = GetGroupProgress(elapsed, groupIndex);
                ApplyGroupSample(layerGroups[groupIndex], localProgress, true);
            }
        }

        private void ApplyExitSample(float progress)
        {
            var elapsed = progress * GetPhaseDuration();
            for (var orderIndex = 0; orderIndex < layerGroups.Count; orderIndex++)
            {
                var groupIndex = layerGroups.Count - 1 - orderIndex;
                var localProgress = GetGroupProgress(elapsed, orderIndex);
                ApplyGroupSample(layerGroups[groupIndex], localProgress, false);
            }
        }

        private void ApplyGroupSample(LayerGroupState group, float progress, bool isEntering)
        {
            var easedProgress = DOVirtual.EasedValue(
                0f,
                1f,
                progress,
                isEntering ? enterEase : exitEase);
            foreach (var layer in group.Layers)
            {
                if (layer?.RectTransform == null)
                {
                    continue;
                }

                var from = isEntering ? layer.OffscreenPosition : layer.CoveredPosition;
                var to = isEntering ? layer.CoveredPosition : layer.OffscreenPosition;
                layer.RectTransform.anchoredPosition = Vector2.LerpUnclamped(from, to, easedProgress);
            }
        }

        private float GetGroupProgress(float elapsed, int orderIndex)
        {
            var startTime = orderIndex * Mathf.Max(0f, groupInterval);
            return Mathf.Clamp01((elapsed - startTime) / Mathf.Max(0.01f, groupSlideDuration));
        }

        private float GetPhaseDuration()
        {
            return Mathf.Max(0.01f, groupSlideDuration) +
                Mathf.Max(0, layerGroups.Count - 1) * Mathf.Max(0f, groupInterval);
        }

        private string BuildOrderSnapshot(bool reverse)
        {
            var order = new string[layerGroups.Count];
            for (var index = 0; index < layerGroups.Count; index++)
            {
                order[index] = (reverse ? layerGroups.Count - 1 - index : index).ToString();
            }

            return string.Join(",", order);
        }

        private void KillPlayingSequence()
        {
            if (playingSequence != null && playingSequence.IsActive())
            {
                playingSequence.Kill();
            }

            playingSequence = null;
            isPlayingSnapshot = false;
        }

        private void SetVisible(bool isVisible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.blocksRaycasts = isVisible;
            canvasGroup.interactable = isVisible;
        }
    }
}
