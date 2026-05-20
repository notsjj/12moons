using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;

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
        [SerializeField] private float pointerBobDistance = 12f;
        [SerializeField] private float pointerBobDuration = 0.45f;

        private readonly Dictionary<string, FactionSuspicionRow> rowsByFactionId =
            new Dictionary<string, FactionSuspicionRow>(StringComparer.Ordinal);

        private Tween pointerMoveTween;
        private Tween pointerBobTween;
        private float pointerInitialX;

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
            }
        }

        private void OnEnable()
        {
            if (factionService != null)
            {
                factionService.FactionsChanged += Refresh;
                factionService.ThresholdTriggered += ShowThresholdFeedback;
            }

            Refresh();
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
                row.RectTransform == null ||
                pointerIcon.parent == null)
            {
                return;
            }

            StopPointerTweens();
            var targetPosition = GetPointerTargetPosition(row.RectTransform);
            pointerMoveTween = pointerIcon
                .DOAnchorPos(targetPosition, Mathf.Max(0f, pointerMoveDuration))
                .SetEase(pointerMoveEase)
                .OnComplete(StartPointerBob);
        }

        private Vector2 GetPointerTargetPosition(RectTransform rowRect)
        {
            var parentRect = pointerIcon.parent as RectTransform;
            if (parentRect == null)
            {
                return new Vector2(pointerInitialX, pointerIcon.anchoredPosition.y);
            }

            var rowWorldCenter = rowRect.TransformPoint(rowRect.rect.center);
            var localCenter = parentRect.InverseTransformPoint(rowWorldCenter);
            return new Vector2(pointerInitialX, localCenter.y);
        }

        private void StartPointerBob()
        {
            if (pointerIcon == null || pointerBobDistance <= 0f || pointerBobDuration <= 0f)
            {
                return;
            }

            var targetY = pointerIcon.anchoredPosition.y + pointerBobDistance;
            pointerBobTween = pointerIcon
                .DOAnchorPosY(targetY, pointerBobDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
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
            pointerBobTween?.Kill();
            pointerMoveTween = null;
            pointerBobTween = null;
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
