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
            [Header("公文类型背景：类型名和对应底图")]
            public string documentType;
            public Sprite backgroundSprite;
        }
#pragma warning restore 0649

        [Header("依赖对象：运行时服务与共用人物框")]
        [SerializeField] private DocumentService documentService;
        [SerializeField] private InventoryService inventoryService;
        [SerializeField] private SharedActorSlotView sharedActorSlot;

        [Header("质疑度栏：选项结算后显示阵营反馈")]
        [SerializeField] private SuspicionPanelView suspicionPanel;

        [Header("卷轴移动：内容与左卷轴端使用同一距离和速度")]
        [SerializeField] private RectTransform leftScrollEnd;
        [SerializeField] private RectTransform rightScrollEnd;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private float scrollMoveLeftDistance = 700f;
        [SerializeField] private float scrollTweenDuration = 0.8f;

        [Header("公文内容：背景、标题、正文、状态与盖章")]
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
            ConfigureClickCatcher();
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
            OpenScroll();
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

        private void ShowDocument(RuntimeDocumentQueueEntry entry, DocumentDefinition document)
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
            if (RequiresSubmittedItem(option) && !HasSubmittedRequirement(option))
            {
                SetText(flowStatusText, "这个选项需要先把对应卡牌拖入提交区域。");
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
            ClearTransientFeedback();
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
            ClearTransientFeedback();
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

            builder.AppendLine("所需物品：");
            if (!string.IsNullOrEmpty(optionARequirements))
            {
                builder.AppendLine($"甲：{optionARequirements}");
            }

            if (!string.IsNullOrEmpty(optionBRequirements))
            {
                builder.AppendLine($"乙：{optionBRequirements}");
            }

            return builder.ToString();
        }

        private string BuildRequirementText(DocumentOptionDefinition option)
        {
            if (option == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            AppendRequirement(builder, option.RequiredItemId, option.RequiredItemCount);
            AppendResourceRequirement(builder, InventoryItemType.Money, option.MoneyChange);
            AppendResourceRequirement(builder, InventoryItemType.Material, option.MaterialChange);
            AppendResourceRequirement(builder, InventoryItemType.Food, option.FoodChange);
            return builder.ToString();
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
                builder.Append("，");
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
