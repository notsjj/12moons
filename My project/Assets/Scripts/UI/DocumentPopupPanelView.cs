using System;
using TMPro;
using DG.Tweening;
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
            [Header("公文类型背景：类型名和对应底图")]
            public string documentType;
            public Sprite backgroundSprite;
        }
#pragma warning restore 0649

        [Header("依赖对象：运行时服务与共用人物框")]
        [SerializeField] private DocumentService documentService;
        [SerializeField] private SharedActorSlotView sharedActorSlot;
        [Header("质疑度栏：选项结算后移动手指并显示阵营反馈")]
        [SerializeField] private SuspicionPanelView suspicionPanel;

        [Header("卷轴移动：内容与左卷轴端使用同一距离和速度")]
        [SerializeField] private RectTransform leftScrollEnd;
        [SerializeField] private RectTransform rightScrollEnd;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private float scrollMoveLeftDistance = 700f;
        [SerializeField] private float scrollTweenDuration = 0.8f;

        [Header("公文内容：背景、标题、正文、反馈与盖章")]
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

        [Header("公文按钮：两个选项与处理完后开放的城区按钮")]
        [SerializeField] private Button optionAButton;
        [SerializeField] private Button optionBButton;
        [SerializeField] private Button cityExploreButton;

        [Header("提交栏：需要道具时接收背包卡牌")]
        [SerializeField] private GameObject submitPanel;
        [SerializeField] private DocumentSubmitSlot submitSlot;

        private RuntimeDocumentQueueEntry currentEntry;
        private DocumentDefinition currentDocument;
        private bool waitingForContinue;
        private bool lastSubmitAccepted;
        private Vector2 leftScrollClosedPosition;
        private Vector2 contentClosedPosition;

        private void Awake()
        {
            ResolveDependencies();
            CacheClosedScrollPositions();
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
            if (documentService != null)
            {
                documentService.DocumentsChanged -= RefreshCurrentDocument;
            }
        }

        private void Update()
        {
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
                "This popup is the desk document frame. Document queue and result logic are added by the document system stage.",
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
                SetText(flowStatusText, "本回合没有待处理公文。");
                return;
            }

            if (cityExploreButton != null)
            {
                cityExploreButton.interactable = false;
            }

            ShowDocument(entry, document);
        }

        public void Show(string title, string body, string optionA, string optionB)
        {
            currentEntry = null;
            currentDocument = null;
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
            OpenScroll();
        }

        [ContextMenu("Hide")]
        public void Hide()
        {
            currentEntry = null;
            currentDocument = null;
            waitingForContinue = false;
            sharedActorSlot?.HideToRight();
            submitSlot?.Clear();
            lastSubmitAccepted = false;
            CloseInstant();
            gameObject.SetActive(false);
        }

        public void OnOptionAClicked()
        {
            ResolveCurrentDocument(DocumentOptionType.A);
        }

        public void OnOptionBClicked()
        {
            ResolveCurrentDocument(DocumentOptionType.B);
        }

        private void ShowDocument(RuntimeDocumentQueueEntry entry, DocumentDefinition document)
        {
            currentEntry = entry;
            currentDocument = document;
            waitingForContinue = false;
            lastSubmitAccepted = false;
            SetText(titleText, document.Title);
            SetText(bodyText, document.BodyText);
            SetText(optionAText, document.OptionA.Text);
            SetText(optionBText, document.OptionB.Text);
            SetText(proposerFeedbackText, string.Empty);
            SetText(flowStatusText, "请选择处理方式。");
            ApplyDocumentBackground(document);
            ClearStamps();
            ConfigureSubmitPanel(document);
            RefreshOptionLocks();

            ShowProposer(document);
            gameObject.SetActive(true);
            OpenScroll();
        }

        private void ResolveCurrentDocument(DocumentOptionType optionType)
        {
            if (waitingForContinue)
            {
                return;
            }

            if (documentService == null || currentEntry == null || currentDocument == null)
            {
                SetText(flowStatusText, "没有打开的公文。");
                return;
            }

            var option = currentDocument.GetOption(optionType);
            if (RequiresSubmittedItem(option) && (submitSlot == null || !submitSlot.HasAcceptedItem))
            {
                SetText(flowStatusText, "这个选项需要先把对应卡牌拖入提交栏。");
                RefreshOptionLocks();
                return;
            }

            var result = documentService.ResolveDocument(currentEntry, optionType);
            SetText(proposerFeedbackText, FormatResultFeedback(result));

            if (result.Success)
            {
                suspicionPanel?.ShowDocumentChoiceImpact(result.FeedbackFactionId, result.FactionFeedbackText);
                ShowStamp(optionType);
                SetButtonsInteractable(false);
                SetText(flowStatusText, "已盖章。点击公文空白处继续。");
                currentEntry = null;
                currentDocument = null;
                waitingForContinue = true;
            }
            else
            {
                SetText(flowStatusText, result.Message);
                RefreshOptionLocks();
            }
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

            waitingForContinue = false;
            sharedActorSlot?.HideToRight();
            CloseScroll();
            DOVirtual.DelayedCall(scrollTweenDuration, ShowNextDocumentOrFinish);
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

        private void ShowProposer(DocumentDefinition document)
        {
            if (sharedActorSlot == null)
            {
                return;
            }

            if (documentService != null &&
                documentService.TryGetCharacter(document.ProposerCharacterId, out var character))
            {
                sharedActorSlot.ShowActor(character.CharacterName, "Document proposer", null);
                return;
            }

            if (!string.IsNullOrEmpty(document.ProposerCharacterId))
            {
                sharedActorSlot.ShowActor(document.ProposerCharacterId, "Document proposer", null);
            }
        }

        private void ShowNextDocumentOrFinish()
        {
            submitSlot?.Clear();
            if (documentService != null &&
                documentService.TryGetNextPendingDocument(out var entry, out var document))
            {
                ShowDocument(entry, document);
                return;
            }

            EndDocumentFlow();
        }

        private void EndDocumentFlow()
        {
            currentEntry = null;
            currentDocument = null;
            waitingForContinue = false;
            submitSlot?.Clear();
            SetText(flowStatusText, "本回合公文已全部处理。");
            if (cityExploreButton != null)
            {
                cityExploreButton.interactable = true;
            }

            gameObject.SetActive(false);
        }

        private void OpenScroll()
        {
            if (contentGroup != null)
            {
                contentGroup.DOKill();
                contentGroup.alpha = 1f;
                contentGroup.blocksRaycasts = true;
                contentGroup.interactable = true;
            }

            if (leftScrollEnd != null)
            {
                leftScrollEnd.DOKill();
                leftScrollEnd.anchoredPosition = leftScrollClosedPosition;
                leftScrollEnd.DOAnchorPos(GetOpenedPosition(leftScrollClosedPosition), scrollTweenDuration);
            }

            if (contentRoot != null)
            {
                contentRoot.DOKill();
                contentRoot.anchoredPosition = contentClosedPosition;
                contentRoot.DOAnchorPos(GetOpenedPosition(contentClosedPosition), scrollTweenDuration);
            }
        }

        private void CloseScroll()
        {
            if (contentGroup != null)
            {
                contentGroup.DOKill();
                contentGroup.blocksRaycasts = false;
                contentGroup.interactable = false;
                contentGroup.alpha = 1f;
            }

            if (leftScrollEnd != null)
            {
                leftScrollEnd.DOKill();
                leftScrollEnd.DOAnchorPos(leftScrollClosedPosition, scrollTweenDuration);
            }

            if (contentRoot != null)
            {
                contentRoot.DOKill();
                contentRoot.DOAnchorPos(contentClosedPosition, scrollTweenDuration);
            }
        }

        private void CloseInstant()
        {
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

            if (contentRoot != null)
            {
                contentRoot.anchoredPosition = contentClosedPosition;
            }
        }

        private void CacheClosedScrollPositions()
        {
            leftScrollClosedPosition = leftScrollEnd != null ? leftScrollEnd.anchoredPosition : Vector2.zero;
            contentClosedPosition = contentRoot != null ? contentRoot.anchoredPosition : Vector2.zero;
        }

        private Vector2 GetOpenedPosition(Vector2 closedPosition)
        {
            return closedPosition + (Vector2.left * scrollMoveLeftDistance);
        }

        private void ConfigureSubmitPanel(DocumentDefinition document)
        {
            var optionARequiresItem = RequiresSubmittedItem(document.OptionA);
            var optionBRequiresItem = RequiresSubmittedItem(document.OptionB);
            if (!optionARequiresItem && !optionBRequiresItem)
            {
                HideSubmitPanel();
                return;
            }

            if (submitPanel != null)
            {
                submitPanel.SetActive(true);
            }

            var requiredOption = optionARequiresItem ? document.OptionA : document.OptionB;
            submitSlot?.Configure(requiredOption.RequiredItemId, requiredOption.RequiredItemCount);
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

            SetButtonInteractable(optionAButton, !RequiresSubmittedItem(currentDocument.OptionA) || (submitSlot != null && submitSlot.HasAcceptedItem));
            SetButtonInteractable(optionBButton, !RequiresSubmittedItem(currentDocument.OptionB) || (submitSlot != null && submitSlot.HasAcceptedItem));
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

        private void ResolveDependencies()
        {
            if (documentService == null)
            {
                documentService = FindFirstObjectByType<DocumentService>();
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
                !string.IsNullOrEmpty(option.RequiredItemId) &&
                option.RequiredItemCount > 0;
        }

        private static string FormatResultFeedback(DocumentResolutionResult result)
        {
            if (result == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(result.FactionFeedbackText))
            {
                return result.Message;
            }

            if (string.IsNullOrEmpty(result.Message))
            {
                return result.FactionFeedbackText;
            }

            return $"{result.Message}\n{result.FactionFeedbackText}";
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
