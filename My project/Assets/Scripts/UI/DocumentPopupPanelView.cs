using System;
using System.Text;
using DG.Tweening;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class DocumentPopupPanelView : MonoBehaviour, IPointerClickHandler
    {
#pragma warning disable 0649
        [Serializable]
        private struct DocumentTypeBackgroundBinding
        {
            [Header("\u516c\u6587\u7c7b\u578b\u80cc\u666f\uff1a\u7c7b\u578b\u540d\u548c\u5bf9\u5e94\u5e95\u56fe")]
            public string documentType;
            public Sprite backgroundSprite;
        }
#pragma warning restore 0649

        [Header("\u4f9d\u8d56\u5bf9\u8c61\uff1a\u8fd0\u884c\u65f6\u670d\u52a1\u4e0e\u5171\u7528\u4eba\u7269\u69fd")]
        [SerializeField] private DocumentService documentService;
        [SerializeField] private InventoryService inventoryService;
        [SerializeField] private SharedActorSlotView sharedActorSlot;

        [Header("\u8d28\u7591\u5ea6\u680f\uff1a\u9009\u9879\u7ed3\u7b97\u540e\u663e\u793a\u9635\u8425\u53cd\u9988")]
        [SerializeField] private SuspicionPanelView suspicionPanel;

        [Header("\u5377\u8f74\u52a8\u753b\uff1a\u5de6\u53f3\u5377\u8f74\u4e0e\u5185\u5bb9\u9762\u677f")]
        [SerializeField] private RectTransform leftScrollEnd;
        [SerializeField] private RectTransform rightScrollEnd;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private float scrollTweenDuration = 0.8f;
        [Header("\u5377\u8f74\u52a8\u753b\uff1a\u95ed\u5408\u65f6\u5de6\u53f3\u5377\u8f74\u4e4b\u95f4\u7684\u95f4\u9699")]
        [SerializeField] private float closedScrollGap = 0f;
        [Header("\u5377\u8f74\u52a8\u753b\uff1a\u5173\u95ed\u72b6\u6001\u505c\u7559\u65f6\u957f")]
        [SerializeField] private float closedStateHoldDuration = 0.5f;
        [Header("\u516c\u6587\u7ed3\u7b97\uff1a\u76d6\u7ae0\u540e\u81ea\u52a8\u8fdb\u5165\u4e0b\u4e00\u4efd")]
        [Tooltip("\u70b9\u51fb\u9009\u9879\u5e76\u76d6\u7ae0\u540e\uff0c\u505c\u7559\u591a\u4e45\u81ea\u52a8\u8fdb\u5165\u4e0b\u4e00\u4efd\u516c\u6587\u6216\u81ea\u52a8\u5408\u4e0a\u3002")]
        [SerializeField] private float autoAdvanceAfterStampDuration = 0.45f;

        [Header("\u516c\u6587\u5185\u5bb9\uff1a\u80cc\u666f\u3001\u6807\u9898\u3001\u6b63\u6587\u3001\u72b6\u6001\u4e0e\u76d6\u7ae0")]
        [SerializeField] private Image contentBackgroundImage;
        [SerializeField] private DocumentTypeBackgroundBinding[] typeBackgrounds;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text optionAText;
        [SerializeField] private TMP_Text optionBText;
        [SerializeField] private TMP_Text proposerFeedbackText;
        [SerializeField] private TMP_Text flowStatusText;
        [SerializeField] private Image optionAStampImage;
        [SerializeField] private Image optionBStampImage;

        [Header("\u516c\u6587\u6309\u94ae\uff1a\u4e24\u4e2a\u9009\u9879\u4e0e\u5904\u7406\u5b8c\u540e\u5f00\u653e\u7684\u57ce\u533a\u6309\u94ae")]
        [SerializeField] private Button optionAButton;
        [SerializeField] private Button optionBButton;
        [SerializeField] private Button cityExploreButton;

        [Header("\u63d0\u4ea4\u680f\uff1a\u9700\u8981\u9053\u5177\u65f6\u63a5\u6536\u80cc\u5305\u5361\u724c")]
        [SerializeField] private GameObject submitPanel;
        [SerializeField] private DocumentSubmitSlot submitSlot;

        [Header("\u516c\u6587\u4eba\u7269\u5207\u6362\u8c03\u8bd5\u5feb\u7167")]
        [Tooltip("\u53ea\u8bfb\u8fd0\u884c\u65f6\u72b6\u6001\uff1a\u4e0a\u4e00\u4f4d\u63d0\u51fa\u8005\u9000\u573a\u6216\u4e0b\u4e00\u4f4d\u63d0\u51fa\u8005\u5165\u573a\u65f6\u4e3a\u771f\u3002")]
        [SerializeField] private bool isActorTransitioningSnapshot;

        private RuntimeDocumentQueueEntry currentEntry;
        private DocumentDefinition currentDocument;
        private bool waitingForContinue;
        private bool lastSubmitAccepted;
        private Vector2 leftScrollOpenedPosition;
        private Vector2 rightScrollOpenedPosition;
        private Vector2 leftScrollClosedPosition;
        private Vector2 rightScrollClosedPosition;
        private Vector2 contentOpenedPosition;
        private Vector2 contentClosedPosition;
        private Sequence scrollSequence;
        private Tween pendingOpenTween;
        private Tween pendingAutoAdvanceTween;

        public bool IsDocumentFlowActive =>
            gameObject.activeInHierarchy && (currentDocument != null || currentEntry != null || waitingForContinue || isActorTransitioningSnapshot);

        public bool IsActorTransitioning => isActorTransitioningSnapshot;

        public event Action DocumentFlowStateChanged;

        private void Awake()
        {
            ResolveDependencies();
            AutoBindAnimationReferences();
            ConfigureClickCatcher();
            CacheOpenLayout();
            CacheClosedLayout();
            CloseInstant();
        }

        private void OnEnable()
        {
            if (documentService != null)
            {
                documentService.DocumentsChanged += RefreshCurrentDocument;
            }
        }

        private void OnDisable()
        {
            KillPendingOpenTween();
            KillPendingAutoAdvanceTween();
            KillScrollTweens();
            isActorTransitioningSnapshot = false;

            if (documentService != null)
            {
                documentService.DocumentsChanged -= RefreshCurrentDocument;
            }
        }

        private void Update()
        {
            if (isActorTransitioningSnapshot)
            {
                return;
            }

            if (waitingForContinue)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    ContinueAfterResolution();
                }

                return;
            }

            if (currentDocument == null || submitSlot == null)
            {
                return;
            }

            if (lastSubmitAccepted != submitSlot.HasAcceptedItem)
            {
                lastSubmitAccepted = submitSlot.HasAcceptedItem;
                RefreshOptionLocks();
            }
        }

        [ContextMenu("Show Preview")]
        public void ShowPreview()
        {
            Show(
                "Document Preview",
                "This popup is the desk document frame.",
                "Option A",
                "Option B");
        }

        [ContextMenu("Show Next Pending Document")]
        public void ShowNextPendingDocument()
        {
            BeginDocumentFlow();
        }

        public void BeginDocumentFlow()
        {
            if (documentService == null ||
                !documentService.TryGetNextPendingDocument(out var entry, out var document))
            {
                EndDocumentFlow();
                SetText(flowStatusText, "\u672c\u56de\u5408\u6ca1\u6709\u5f85\u5904\u7406\u516c\u6587\u3002");
                return;
            }

            if (cityExploreButton != null)
            {
                cityExploreButton.interactable = false;
            }

            ShowDocument(entry, document, true);
        }

        public void Show(string title, string body, string optionA, string optionB)
        {
            currentEntry = null;
            currentDocument = null;
            ClearTransientFeedback();
            SetText(titleText, title);
            SetText(bodyText, body);
            SetText(optionAText, optionA);
            SetText(optionBText, optionB);
            SetText(proposerFeedbackText, string.Empty);
            SetText(flowStatusText, string.Empty);
            SetButtonsInteractable(true);
            ClearStamps();
            HideSubmitPanel();

            gameObject.SetActive(true);
            CloseInstant();
            ScheduleOpenScroll();
            NotifyDocumentFlowStateChanged();
        }

        [ContextMenu("Hide")]
        public void Hide()
        {
            currentEntry = null;
            currentDocument = null;
            waitingForContinue = false;
            ClearTransientFeedback();
            sharedActorSlot?.HideToRight();
            submitSlot?.Clear();
            lastSubmitAccepted = false;
            KillPendingOpenTween();
            KillPendingAutoAdvanceTween();
            CloseInstant();
            gameObject.SetActive(false);
            NotifyDocumentFlowStateChanged();
        }

        public void OnOptionAClicked()
        {
            ResolveCurrentDocument(DocumentOptionType.A);
        }

        public void OnOptionBClicked()
        {
            ResolveCurrentDocument(DocumentOptionType.B);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (waitingForContinue)
            {
                ContinueAfterResolution();
            }
        }

        public void ContinueAfterResolution()
        {
            if (!waitingForContinue)
            {
                return;
            }

            KillPendingAutoAdvanceTween();
            waitingForContinue = false;
            sharedActorSlot?.ClearFeedback();
            submitSlot?.Clear();
            SetButtonsInteractable(false);

            if (documentService != null &&
                documentService.TryGetNextPendingDocument(out var nextEntry, out var nextDocument))
            {
                TransitionToNextDocument(nextEntry, nextDocument);
                return;
            }

            isActorTransitioningSnapshot = true;
            NotifyDocumentFlowStateChanged();
            HideActorAlongEntryPath(() =>
            {
                isActorTransitioningSnapshot = false;
                CloseScroll();
                DOVirtual.DelayedCall(scrollTweenDuration, EndDocumentFlow);
            });
        }

        private void ShowDocument(RuntimeDocumentQueueEntry entry, DocumentDefinition document, bool playOpenAnimation, bool showProposer = true)
        {
            currentEntry = entry;
            currentDocument = document;
            waitingForContinue = false;
            lastSubmitAccepted = false;
            ClearTransientFeedback();
            SetText(titleText, document.Title);
            SetText(bodyText, BuildBodyTextWithRequirements(document));
            SetText(optionAText, document.OptionA.Text);
            SetText(optionBText, document.OptionB.Text);
            SetText(proposerFeedbackText, string.Empty);
            SetText(flowStatusText, "\u8bf7\u9009\u62e9\u5904\u7406\u65b9\u5f0f\u3002");
            ApplyDocumentBackground(document);
            ClearStamps();
            ConfigureSubmitPanel(document);
            RefreshOptionLocks();

            if (showProposer)
            {
                ShowProposer(document);
            }
            gameObject.SetActive(true);
            if (playOpenAnimation)
            {
                CloseInstant();
                ScheduleOpenScroll();
            }
            else
            {
                OpenInstant();
            }

            NotifyDocumentFlowStateChanged();
        }

        private void ResolveCurrentDocument(DocumentOptionType optionType)
        {
            if (waitingForContinue)
            {
                return;
            }

            if (documentService == null || currentEntry == null || currentDocument == null)
            {
                SetText(flowStatusText, "\u6ca1\u6709\u6253\u5f00\u7684\u516c\u6587\u3002");
                return;
            }

            RefreshCurrentDocument();
            var option = currentDocument.GetOption(optionType);
            if (RequiresSubmittedItem(option) && !HasSubmittedRequirement(option))
            {
                SetText(flowStatusText, "\u8fd9\u4e2a\u9009\u9879\u9700\u8981\u5148\u628a\u5bf9\u5e94\u5361\u724c\u62d6\u5165\u63d0\u4ea4\u533a\u57df\u3002");
                RefreshOptionLocks();
                return;
            }

            var requiredItemAlreadySubmitted = HasSubmittedRequirement(option);
            var result = documentService.ResolveDocument(currentEntry, optionType, requiredItemAlreadySubmitted);
            SetText(proposerFeedbackText, string.Empty);

            if (result.Success)
            {
                if (requiredItemAlreadySubmitted)
                {
                    submitSlot?.MarkAcceptedItemCommitted();
                }
                else
                {
                    submitSlot?.Clear();
                }

                sharedActorSlot?.ShowFeedback(result.ProposerFeedbackText);
                suspicionPanel?.ShowDocumentChoiceImpact(result.FeedbackFactionId, result.FactionFeedbackText);
                ShowStamp(optionType);
                SetButtonsInteractable(false);
                SetText(flowStatusText, "\u5df2\u76d6\u7ae0\u3002");
                currentEntry = null;
                currentDocument = null;
                waitingForContinue = true;
                SetText(
                    flowStatusText,
                    documentService != null && documentService.TryGetNextPendingDocument(out _, out _)
                        ? "\u5df2\u76d6\u7ae0\u3002\u6b63\u5728\u8fdb\u5165\u4e0b\u4e00\u4efd\u516c\u6587\u3002"
                        : "\u5df2\u76d6\u7ae0\u3002\u6b63\u5728\u5408\u4e0a\u516c\u6587\u3002");
                ScheduleAutoAdvanceAfterResolution();
                NotifyDocumentFlowStateChanged();
            }
            else
            {
                SetText(flowStatusText, result.Message);
                RefreshOptionLocks();
            }
        }

        private void RefreshCurrentDocument()
        {
            if (currentEntry == null || currentDocument == null)
            {
                return;
            }

            if (!documentService.TryGetDefinition(currentDocument.DocumentId, out var refreshedDocument))
            {
                Hide();
                return;
            }

            currentDocument = refreshedDocument;
        }

        private void ShowProposer(DocumentDefinition document, Action onComplete = null)
        {
            if (sharedActorSlot == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (documentService != null &&
                documentService.TryGetCharacter(document.ProposerCharacterId, out var character))
            {
                sharedActorSlot.ShowActor(
                    character.CharacterName,
                    "Document proposer",
                    CharacterPlaceholderPortraitProvider.LoadPortrait(character.PortraitId),
                    onComplete);
                return;
            }

            if (!string.IsNullOrEmpty(document.ProposerCharacterId))
            {
                sharedActorSlot.ShowActor(
                    document.ProposerCharacterId,
                    "Document proposer",
                    CharacterPlaceholderPortraitProvider.LoadPortrait(document.ProposerCharacterId),
                    onComplete);
                return;
            }

            sharedActorSlot.ShowActor(
                "\u516c\u6587\u63d0\u51fa\u8005",
                "Document proposer",
                CharacterPlaceholderPortraitProvider.LoadPortrait(string.Empty),
                onComplete);
        }

        private void TransitionToNextDocument(RuntimeDocumentQueueEntry nextEntry, DocumentDefinition nextDocument)
        {
            isActorTransitioningSnapshot = true;
            NotifyDocumentFlowStateChanged();
            HideActorAlongEntryPath(() =>
            {
                ShowProposer(nextDocument, () =>
                {
                    isActorTransitioningSnapshot = false;
                    ShowDocument(nextEntry, nextDocument, false, false);
                });
            });
        }

        private void HideActorAlongEntryPath(Action onComplete)
        {
            if (sharedActorSlot == null)
            {
                onComplete?.Invoke();
                return;
            }

            sharedActorSlot.HideAlongEntryPath(onComplete);
        }

        private void ShowNextDocumentOrFinish()
        {
            ClearTransientFeedback();
            submitSlot?.Clear();
            if (documentService != null &&
                documentService.TryGetNextPendingDocument(out var entry, out var document))
            {
                ShowDocument(entry, document, false);
                return;
            }

            EndDocumentFlow();
        }

        private void EndDocumentFlow()
        {
            currentEntry = null;
            currentDocument = null;
            waitingForContinue = false;
            isActorTransitioningSnapshot = false;
            ClearTransientFeedback();
            submitSlot?.Clear();
            SetText(flowStatusText, "\u672c\u56de\u5408\u516c\u6587\u5df2\u5168\u90e8\u5904\u7406\u3002");
            if (cityExploreButton != null)
            {
                cityExploreButton.interactable = true;
            }

            gameObject.SetActive(false);
            NotifyDocumentFlowStateChanged();
        }

        private void NotifyDocumentFlowStateChanged()
        {
            DocumentFlowStateChanged?.Invoke();
        }

        private void OpenScroll()
        {
            EnsureAnimationLayoutCached();
            KillScrollTweens();
            scrollSequence = DOTween.Sequence();

            if (contentGroup != null)
            {
                contentGroup.alpha = 1f;
                contentGroup.blocksRaycasts = false;
                contentGroup.interactable = false;
            }

            if (leftScrollEnd != null)
            {
                leftScrollEnd.anchoredPosition = leftScrollClosedPosition;
            }

            if (rightScrollEnd != null)
            {
                rightScrollEnd.anchoredPosition = rightScrollClosedPosition;
                scrollSequence.Join(rightScrollEnd.DOAnchorPos(rightScrollOpenedPosition, scrollTweenDuration).SetEase(Ease.OutCubic));
            }

            if (contentRoot != null)
            {
                contentRoot.anchoredPosition = contentClosedPosition;
                scrollSequence.Join(contentRoot.DOAnchorPos(contentOpenedPosition, scrollTweenDuration).SetEase(Ease.OutCubic));
            }

            scrollSequence.OnComplete(EnableContentInteraction);
        }

        private void CloseScroll()
        {
            EnsureAnimationLayoutCached();
            KillScrollTweens();
            scrollSequence = DOTween.Sequence();

            if (contentGroup != null)
            {
                contentGroup.blocksRaycasts = false;
                contentGroup.interactable = false;
                contentGroup.alpha = 1f;
            }

            if (leftScrollEnd != null)
            {
                leftScrollEnd.anchoredPosition = leftScrollClosedPosition;
            }

            if (rightScrollEnd != null)
            {
                scrollSequence.Join(rightScrollEnd.DOAnchorPos(rightScrollClosedPosition, scrollTweenDuration).SetEase(Ease.InCubic));
            }

            if (contentRoot != null)
            {
                scrollSequence.Join(contentRoot.DOAnchorPos(contentClosedPosition, scrollTweenDuration).SetEase(Ease.InCubic));
            }
        }

        private void CloseInstant()
        {
            EnsureAnimationLayoutCached();
            KillScrollTweens();

            if (contentGroup != null)
            {
                contentGroup.alpha = 1f;
                contentGroup.blocksRaycasts = false;
                contentGroup.interactable = false;
            }

            if (leftScrollEnd != null)
            {
                leftScrollEnd.anchoredPosition = leftScrollClosedPosition;
            }

            if (rightScrollEnd != null)
            {
                rightScrollEnd.anchoredPosition = rightScrollClosedPosition;
            }

            if (contentRoot != null)
            {
                contentRoot.anchoredPosition = contentClosedPosition;
            }
        }

        private void OpenInstant()
        {
            EnsureAnimationLayoutCached();
            KillPendingOpenTween();
            KillScrollTweens();

            if (contentGroup != null)
            {
                contentGroup.alpha = 1f;
                contentGroup.blocksRaycasts = true;
                contentGroup.interactable = true;
            }

            if (leftScrollEnd != null)
            {
                leftScrollEnd.anchoredPosition = leftScrollOpenedPosition;
            }

            if (rightScrollEnd != null)
            {
                rightScrollEnd.anchoredPosition = rightScrollOpenedPosition;
            }

            if (contentRoot != null)
            {
                contentRoot.anchoredPosition = contentOpenedPosition;
            }
        }

        private void CacheOpenLayout()
        {
            leftScrollOpenedPosition = leftScrollEnd != null ? leftScrollEnd.anchoredPosition : Vector2.zero;
            rightScrollOpenedPosition = rightScrollEnd != null ? rightScrollEnd.anchoredPosition : Vector2.zero;
            contentOpenedPosition = contentRoot != null ? contentRoot.anchoredPosition : Vector2.zero;
        }

        private void CacheClosedLayout()
        {
            if (leftScrollEnd != null)
            {
                leftScrollClosedPosition = leftScrollOpenedPosition;
            }
            else
            {
                leftScrollClosedPosition = Vector2.zero;
            }

            if (rightScrollEnd != null)
            {
                var leftRightEdge = leftScrollClosedPosition.x;
                if (leftScrollEnd != null)
                {
                    var leftWidth = leftScrollEnd.rect.width * leftScrollEnd.localScale.x;
                    leftRightEdge += leftWidth * (1f - leftScrollEnd.pivot.x);
                }

                var rightWidth = rightScrollEnd.rect.width * rightScrollEnd.localScale.x;
                rightScrollClosedPosition = new Vector2(
                    leftRightEdge + closedScrollGap + (rightWidth * rightScrollEnd.pivot.x),
                    leftScrollClosedPosition.y);
            }
            else
            {
                rightScrollClosedPosition = Vector2.zero;
            }

            if (contentRoot != null)
            {
                var scrollTravelX = rightScrollOpenedPosition.x - rightScrollClosedPosition.x;
                contentClosedPosition = new Vector2(
                    contentOpenedPosition.x - scrollTravelX,
                    contentOpenedPosition.y);
            }
            else
            {
                contentClosedPosition = Vector2.zero;
            }
        }

        private void ConfigureSubmitPanel(DocumentDefinition document)
        {
            var optionARequiresItem = TryGetSubmittedRequirement(document.OptionA, out var optionAItemId, out var optionACount);
            var optionBRequiresItem = TryGetSubmittedRequirement(document.OptionB, out var optionBItemId, out var optionBCount);
            if (!optionARequiresItem && !optionBRequiresItem)
            {
                HideSubmitPanel();
                return;
            }

            if (submitPanel != null)
            {
                submitPanel.SetActive(true);
            }

            submitSlot?.Configure(
                optionARequiresItem ? optionAItemId : optionBItemId,
                optionARequiresItem ? optionACount : optionBCount);
        }

        private void HideSubmitPanel()
        {
            if (submitPanel != null)
            {
                submitPanel.SetActive(false);
            }

            submitSlot?.Clear();
        }

        private void RefreshOptionLocks()
        {
            if (currentDocument == null)
            {
                SetButtonsInteractable(false);
                return;
            }

            SetButtonInteractable(optionAButton, !TryGetSubmittedRequirement(currentDocument.OptionA, out _, out _) || HasSubmittedRequirement(currentDocument.OptionA));
            SetButtonInteractable(optionBButton, !TryGetSubmittedRequirement(currentDocument.OptionB, out _, out _) || HasSubmittedRequirement(currentDocument.OptionB));
        }

        private void ClearStamps()
        {
            if (optionAStampImage != null)
            {
                optionAStampImage.enabled = false;
            }

            if (optionBStampImage != null)
            {
                optionBStampImage.enabled = false;
            }
        }

        private void ShowStamp(DocumentOptionType optionType)
        {
            ClearStamps();
            var stamp = optionType == DocumentOptionType.A ? optionAStampImage : optionBStampImage;
            if (stamp != null)
            {
                stamp.enabled = true;
            }
        }

        private void ApplyDocumentBackground(DocumentDefinition document)
        {
            if (contentBackgroundImage == null || document == null || typeBackgrounds == null)
            {
                return;
            }

            foreach (var binding in typeBackgrounds)
            {
                if (string.Equals(binding.documentType, document.DocumentType, StringComparison.OrdinalIgnoreCase) &&
                    binding.backgroundSprite != null)
                {
                    contentBackgroundImage.sprite = binding.backgroundSprite;
                    return;
                }
            }
        }

        private void ClearTransientFeedback()
        {
            SetText(proposerFeedbackText, string.Empty);
            sharedActorSlot?.ClearFeedback();
            suspicionPanel?.ClearDocumentFeedback();
        }

        private void ResolveDependencies()
        {
            if (documentService == null)
            {
                documentService = FindFirstObjectByType<DocumentService>();
            }

            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<InventoryService>();
            }

            if (sharedActorSlot == null)
            {
                sharedActorSlot = FindFirstObjectByType<SharedActorSlotView>(FindObjectsInactive.Include);
            }

            if (suspicionPanel == null)
            {
                suspicionPanel = FindFirstObjectByType<SuspicionPanelView>(FindObjectsInactive.Include);
            }
        }

        private void AutoBindAnimationReferences()
        {
            if (contentRoot != null && contentGroup == null)
            {
                contentGroup = contentRoot.GetComponent<CanvasGroup>();
            }
        }

        private void EnsureAnimationLayoutCached()
        {
            if (leftScrollEnd == null || rightScrollEnd == null || contentRoot == null)
            {
                return;
            }

            if (leftScrollOpenedPosition == Vector2.zero &&
                rightScrollOpenedPosition == Vector2.zero &&
                contentOpenedPosition == Vector2.zero)
            {
                CacheOpenLayout();
                CacheClosedLayout();
            }
        }

        private void KillScrollTweens()
        {
            scrollSequence?.Kill();
            scrollSequence = null;
            leftScrollEnd?.DOKill();
            rightScrollEnd?.DOKill();
            contentRoot?.DOKill();
            contentGroup?.DOKill();
        }

        private void ScheduleOpenScroll()
        {
            KillPendingOpenTween();

            if (closedStateHoldDuration <= 0f)
            {
                OpenScroll();
                return;
            }

            pendingOpenTween = DOVirtual.DelayedCall(closedStateHoldDuration, OpenScroll);
        }

        private void KillPendingOpenTween()
        {
            pendingOpenTween?.Kill();
            pendingOpenTween = null;
        }

        private void ScheduleAutoAdvanceAfterResolution()
        {
            KillPendingAutoAdvanceTween();

            if (autoAdvanceAfterStampDuration <= 0f)
            {
                ContinueAfterResolution();
                return;
            }

            pendingAutoAdvanceTween = DOVirtual.DelayedCall(autoAdvanceAfterStampDuration, ContinueAfterResolution);
        }

        private void KillPendingAutoAdvanceTween()
        {
            pendingAutoAdvanceTween?.Kill();
            pendingAutoAdvanceTween = null;
        }

        private void EnableContentInteraction()
        {
            if (contentGroup == null)
            {
                return;
            }

            contentGroup.alpha = 1f;
            contentGroup.blocksRaycasts = true;
            contentGroup.interactable = true;
        }

        private void ConfigureClickCatcher()
        {
            var clickCatcher = GetComponent<Image>();
            if (clickCatcher != null)
            {
                clickCatcher.raycastTarget = true;
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (optionAButton != null)
            {
                optionAButton.interactable = interactable;
            }

            if (optionBButton != null)
            {
                optionBButton.interactable = interactable;
            }
        }

        private static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private static bool RequiresSubmittedItem(DocumentOptionDefinition option)
        {
            return option != null &&
                ((!string.IsNullOrEmpty(option.RequiredItemId) && option.RequiredItemCount > 0) ||
                 option.MoneyChange < 0 ||
                 option.MaterialChange < 0 ||
                 option.FoodChange < 0);
        }

        private bool TryGetSubmittedRequirement(DocumentOptionDefinition option, out string itemId, out int count)
        {
            itemId = string.Empty;
            count = 0;
            if (option == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(option.RequiredItemId) && option.RequiredItemCount > 0)
            {
                itemId = option.RequiredItemId;
                count = option.RequiredItemCount;
                return true;
            }

            if (option.MoneyChange < 0 && TryFindItemIdByType(InventoryItemType.Money, out itemId))
            {
                count = -option.MoneyChange;
                return true;
            }

            if (option.MaterialChange < 0 && TryFindItemIdByType(InventoryItemType.Material, out itemId))
            {
                count = -option.MaterialChange;
                return true;
            }

            if (option.FoodChange < 0 && TryFindItemIdByType(InventoryItemType.Food, out itemId))
            {
                count = -option.FoodChange;
                return true;
            }

            return false;
        }

        private bool HasSubmittedRequirement(DocumentOptionDefinition option)
        {
            return TryGetSubmittedRequirement(option, out var itemId, out var count) &&
                submitSlot != null &&
                submitSlot.HasAcceptedItem &&
                submitSlot.AcceptedItemId == itemId &&
                submitSlot.AcceptedItemCount == count;
        }

        private bool TryFindItemIdByType(InventoryItemType itemType, out string itemId)
        {
            itemId = string.Empty;
            if (inventoryService == null)
            {
                return false;
            }

            foreach (var definition in inventoryService.Definitions)
            {
                if (definition.ItemType == itemType)
                {
                    itemId = definition.ItemId;
                    return true;
                }
            }

            return false;
        }

        private string BuildBodyTextWithRequirements(DocumentDefinition document)
        {
            if (document == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(document.BodyText ?? string.Empty);
            var optionARequirements = BuildRequirementText(document.OptionA);
            var optionBRequirements = BuildRequirementText(document.OptionB);
            if (string.IsNullOrEmpty(optionARequirements) && string.IsNullOrEmpty(optionBRequirements))
            {
                return builder.ToString();
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.AppendLine("\u6240\u9700\u7269\u54c1\uff1a");
            if (!string.IsNullOrEmpty(optionARequirements))
            {
                builder.AppendLine($"\u7532\uff1a{optionARequirements}");
            }

            if (!string.IsNullOrEmpty(optionBRequirements))
            {
                builder.AppendLine($"\u4e59\uff1a{optionBRequirements}");
            }

            return builder.ToString();
        }

        private string BuildRequirementText(DocumentOptionDefinition option)
        {
            if (option == null)
            {
                return string.Empty;
            }

            return TryGetSubmittedRequirement(option, out var itemId, out var count)
                ? $"{GetItemDisplayName(itemId)} x{count}"
                : string.Empty;
        }

        private void AppendResourceRequirement(StringBuilder builder, InventoryItemType itemType, int delta)
        {
            if (delta >= 0 || !TryFindItemIdByType(itemType, out var itemId))
            {
                return;
            }

            AppendRequirement(builder, itemId, -delta);
        }

        private void AppendRequirement(StringBuilder builder, string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append("\uff0c");
            }

            builder.Append(GetItemDisplayName(itemId)).Append(" x").Append(count);
        }

        private string GetItemDisplayName(string itemId)
        {
            return inventoryService != null &&
                inventoryService.TryGetDefinition(itemId, out var definition) &&
                !string.IsNullOrEmpty(definition.ItemName)
                ? definition.ItemName
                : itemId;
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
