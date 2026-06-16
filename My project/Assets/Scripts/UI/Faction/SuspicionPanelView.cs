using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class SuspicionPanelView : MonoBehaviour
    {
        [Header("依赖服务：阵营配置与运行时存档")]
        [SerializeField] private FactionService factionService;
        [SerializeField] private RuntimeDataService runtimeDataService;

        [Header("Hierarchy 行：直接拖入 SuspicionContent 下的四个阵营行")]
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private FactionSuspicionRow[] factionRows = Array.Empty<FactionSuspicionRow>();
        [SerializeField] private FactionIconBinding[] factionIcons = Array.Empty<FactionIconBinding>();

        [Header("反馈文本：显示当前公文选项对阵营的反馈")]
        [SerializeField] private TMP_Text feedbackText;

        [Header("手指图标：移动到受影响最大的阵营行")]
        [SerializeField] private RectTransform pointerIcon;
        [SerializeField] private float pointerMoveDuration = 0.25f;
        [SerializeField] private Ease pointerMoveEase = Ease.OutCubic;
        [Tooltip("质疑度变化后，指针移动到对应质疑行，再围绕右侧轴心旋转摆动的持续时间。")]
        [SerializeField] private float pointerShakeDuration = 2f;
        [Tooltip("质疑指针围绕右侧轴心旋转摆动的最大角度。")]
        [FormerlySerializedAs("pointerShakeDistance")]
        [SerializeField] private float pointerSwingAngle = 8f;
        [Tooltip("质疑指针每次旋转摆动的基础时长；数值越小，摆动频率越快。")]
        [FormerlySerializedAs("pointerShakeStepDuration")]
        [SerializeField] private float pointerSwingStepDuration = 0.08f;

        [Header("运行时调试：质疑指针对齐快照")]
        [Tooltip("记录最近一次质疑指针移动的目标阵营、目标图片节点、移动前纵坐标和目标中心纵坐标，便于在 Inspector 中检查对齐结果。")]
        [SerializeField] private string inspectorPointerAlignmentSnapshot;

        private readonly Dictionary<string, FactionSuspicionRow> rowsByFactionId =
            new Dictionary<string, FactionSuspicionRow>(StringComparer.Ordinal);

        private Tween pointerMoveTween;
        private Tween pointerShakeTween;
        private float pointerInitialX;
        private Quaternion pointerInitialRotation;
        private RectTransform panelRectTransform;
        private RectTransformSnapshot panelSnapshot;
        private RectTransformSnapshot contentSnapshot;
        private RectTransformSnapshot[] rowSnapshots = Array.Empty<RectTransformSnapshot>();
        private bool shouldLockDeskPanelRect;
        private int remainingDeskPanelLockFrames;
        private LayoutGroup contentLayoutGroup;

        public float PointerShakeDuration => pointerShakeDuration;

        public float PointerSwingAngle => pointerSwingAngle;

        public float PointerSwingStepDuration => pointerSwingStepDuration;

        private void Awake()
        {
            if (factionService == null)
            {
                factionService = FindFirstObjectByType<FactionService>();
            }

            if (runtimeDataService == null)
            {
                runtimeDataService = FindFirstObjectByType<RuntimeDataService>();
            }

            if (contentRoot == null)
            {
                contentRoot = transform as RectTransform;
            }

            if (pointerIcon != null)
            {
                pointerInitialX = pointerIcon.anchoredPosition.x;
                pointerInitialRotation = pointerIcon.localRotation;
            }

            panelRectTransform = transform as RectTransform;
            shouldLockDeskPanelRect = GetComponentInParent<DeskPanelView>(true) != null &&
                GetComponentInParent<City.CityOverlayPanelView>(true) == null;
            remainingDeskPanelLockFrames = shouldLockDeskPanelRect ? 12 : 0;
            panelSnapshot = RectTransformSnapshot.Capture(panelRectTransform);
            contentSnapshot = RectTransformSnapshot.Capture(contentRoot);
            rowSnapshots = CaptureRowSnapshots();
            contentLayoutGroup = contentRoot != null ? contentRoot.GetComponent<LayoutGroup>() : null;
            if (shouldLockDeskPanelRect && contentLayoutGroup != null)
            {
                contentLayoutGroup.enabled = false;
            }

            ApplyDeskPanelRectIfNeeded();
        }

        private void OnEnable()
        {
            if (factionService != null)
            {
                factionService.FactionsChanged += Refresh;
                factionService.ThresholdTriggered += ShowThresholdFeedback;
            }

            Refresh();
            ApplyDeskPanelRectIfNeeded();
        }

        private void OnDisable()
        {
            if (factionService != null)
            {
                factionService.FactionsChanged -= Refresh;
                factionService.ThresholdTriggered -= ShowThresholdFeedback;
            }

            StopPointerTweens();
        }

        public void Refresh()
        {
            if (factionService == null || runtimeDataService == null || contentRoot == null)
            {
                return;
            }

            BuildRowLookup();

            for (var index = 0; index < factionService.Definitions.Count; index++)
            {
                var definition = factionService.Definitions[index];
                var state = runtimeDataService.Data.GetOrCreateFaction(definition.FactionId, definition.InitSuspicion);
                var row = FindRowForDefinition(definition, index);
                if (row == null)
                {
                    continue;
                }

                row.Bind(definition, state, FindFactionIcon(definition.FactionId));
                rowsByFactionId[definition.FactionId] = row;
            }
        }

        public void ShowDocumentChoiceImpact(string factionId, string feedback)
        {
            if (feedbackText != null)
            {
                feedbackText.text = feedback ?? string.Empty;
            }

            if (string.IsNullOrEmpty(factionId))
            {
                return;
            }

            MovePointerToFaction(factionId);
        }

        public void ClearDocumentFeedback()
        {
            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }
        }

        private void ShowThresholdFeedback(FactionThresholdResult result)
        {
            if (feedbackText == null || result == null)
            {
                return;
            }

            if (result.GrantedLowSuspicionLetter)
            {
                feedbackText.text = $"{result.FactionId}: low suspicion letter received ({result.LowSuspicionLetterId})";
                MovePointerToFaction(result.FactionId);
                return;
            }

            if (result.ActivatedPunishTask)
            {
                feedbackText.text = $"{result.FactionId}: high suspicion task activated ({result.PunishTaskId})";
                MovePointerToFaction(result.FactionId);
            }
        }

        private void BuildRowLookup()
        {
            rowsByFactionId.Clear();
            foreach (var row in factionRows)
            {
                if (row == null || string.IsNullOrEmpty(row.FactionId))
                {
                    continue;
                }

                rowsByFactionId[row.FactionId] = row;
            }
        }

        private FactionSuspicionRow FindRowForDefinition(FactionDefinition definition, int definitionIndex)
        {
            if (definition != null &&
                rowsByFactionId.TryGetValue(definition.FactionId, out var rowById) &&
                rowById != null)
            {
                return rowById;
            }

            return factionRows != null && definitionIndex >= 0 && definitionIndex < factionRows.Length
                ? factionRows[definitionIndex]
                : null;
        }

        private void MovePointerToFaction(string factionId)
        {
            if (pointerIcon == null ||
                !rowsByFactionId.TryGetValue(factionId, out var row) ||
                row == null ||
                row.PointerTargetRectTransform == null ||
                pointerIcon.parent == null)
            {
                return;
            }

            StopPointerTweens();
            Canvas.ForceUpdateCanvases();
            var targetPosition = GetPointerTargetPosition(row.PointerTargetRectTransform);
            inspectorPointerAlignmentSnapshot =
                $"阵营={factionId}，目标={row.PointerTargetRectTransform.name}，移动前Y={pointerIcon.localPosition.y:F2}，目标中心Y={targetPosition.y:F2}";
            pointerMoveTween = pointerIcon
                .DOLocalMoveY(targetPosition.y, Mathf.Max(0f, pointerMoveDuration))
                .SetEase(pointerMoveEase)
                .OnComplete(StartPointerShake);
        }

        private Vector2 GetPointerTargetPosition(RectTransform rowRect)
        {
            var parentRect = pointerIcon.parent as RectTransform;
            if (parentRect == null)
            {
                return new Vector2(pointerInitialX, pointerIcon.anchoredPosition.y);
            }

            var relativeBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parentRect, rowRect);
            return new Vector2(pointerIcon.localPosition.x, relativeBounds.center.y);
        }

        private void StartPointerShake()
        {
            if (pointerIcon == null || pointerSwingAngle <= 0f || pointerShakeDuration <= 0f)
            {
                return;
            }

            var startRotation = pointerIcon.localRotation;
            var vibrato = Mathf.Max(1, Mathf.RoundToInt(pointerShakeDuration / Mathf.Max(0.01f, pointerSwingStepDuration)));
            pointerShakeTween = pointerIcon
                .DOPunchRotation(new Vector3(0f, 0f, pointerSwingAngle), pointerShakeDuration, vibrato, 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => pointerIcon.localRotation = startRotation);
        }

        private Sprite FindFactionIcon(string factionId)
        {
            foreach (var binding in factionIcons)
            {
                if (binding != null && binding.FactionId == factionId)
                {
                    return binding.Icon;
                }
            }

            return null;
        }

        private void StopPointerTweens()
        {
            pointerMoveTween?.Kill();
            pointerShakeTween?.Kill();
            pointerMoveTween = null;
            pointerShakeTween = null;
            if (pointerIcon != null)
            {
                pointerIcon.localRotation = pointerInitialRotation;
            }
        }

        private void LateUpdate()
        {
            if (remainingDeskPanelLockFrames <= 0)
            {
                return;
            }

            ApplyDeskPanelRectIfNeeded();
            remainingDeskPanelLockFrames--;
        }

        private void ApplyDeskPanelRectIfNeeded()
        {
            if (!shouldLockDeskPanelRect || panelRectTransform == null)
            {
                return;
            }

            panelSnapshot.Apply(panelRectTransform);
            contentSnapshot.Apply(contentRoot);

            if (factionRows == null)
            {
                return;
            }

            for (var index = 0; index < factionRows.Length && index < rowSnapshots.Length; index++)
            {
                var rowRect = factionRows[index] != null ? factionRows[index].RectTransform : null;
                rowSnapshots[index].Apply(rowRect);
            }
        }

        private RectTransformSnapshot[] CaptureRowSnapshots()
        {
            if (factionRows == null || factionRows.Length == 0)
            {
                return Array.Empty<RectTransformSnapshot>();
            }

            var snapshots = new RectTransformSnapshot[factionRows.Length];
            for (var index = 0; index < factionRows.Length; index++)
            {
                var rowRect = factionRows[index] != null ? factionRows[index].RectTransform : null;
                snapshots[index] = RectTransformSnapshot.Capture(rowRect);
            }

            return snapshots;
        }

        private struct RectTransformSnapshot
        {
            private readonly bool isValid;
            private readonly Vector2 anchorMin;
            private readonly Vector2 anchorMax;
            private readonly Vector2 anchoredPosition;
            private readonly Vector2 sizeDelta;
            private readonly Vector2 pivot;
            private readonly Vector3 localScale;

            private RectTransformSnapshot(
                bool isValid,
                Vector2 anchorMin,
                Vector2 anchorMax,
                Vector2 anchoredPosition,
                Vector2 sizeDelta,
                Vector2 pivot,
                Vector3 localScale)
            {
                this.isValid = isValid;
                this.anchorMin = anchorMin;
                this.anchorMax = anchorMax;
                this.anchoredPosition = anchoredPosition;
                this.sizeDelta = sizeDelta;
                this.pivot = pivot;
                this.localScale = localScale;
            }

            public static RectTransformSnapshot Capture(RectTransform target)
            {
                if (target == null)
                {
                    return default;
                }

                return new RectTransformSnapshot(
                    true,
                    target.anchorMin,
                    target.anchorMax,
                    target.anchoredPosition,
                    target.sizeDelta,
                    target.pivot,
                    target.localScale);
            }

            public void Apply(RectTransform target)
            {
                if (!isValid || target == null)
                {
                    return;
                }

                target.anchorMin = anchorMin;
                target.anchorMax = anchorMax;
                target.anchoredPosition = anchoredPosition;
                target.sizeDelta = sizeDelta;
                target.pivot = pivot;
                target.localScale = localScale;
            }
        }

        [Serializable]
        private sealed class FactionIconBinding
        {
            [SerializeField] private string factionId;
            [SerializeField] private Sprite icon;

            public string FactionId => factionId;

            public Sprite Icon => icon;
        }
    }
}
