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
    public sealed class DocumentPopupPanelView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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
        [Header("\u5377\u8f74\u52a8\u753b\uff1a\u53f3\u4fa7\u6ed1\u5165\u4f4d\u7f6e")]
        [Tooltip("\u516c\u6587\u754c\u9762\u6253\u5f00\u65f6\uff0c\u5de6\u6eda\u8f74\u3001\u53f3\u6eda\u8f74\u548c\u5185\u5bb9\u89c6\u53e3\u4ece\u5c4f\u5e55\u53f3\u4fa7\u5916\u6ed1\u5165\u7684\u6c34\u5e73\u504f\u79fb\u3002")]
        [SerializeField] private float rightSideOffscreenOffset = 1200f;
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
        [Tooltip("势力 logo 图片；为空时运行时会自动查找名为“势力logo”的子物体 Image。")]
        [SerializeField] private Image factionLogoImage;
        [Tooltip("势力 logo 的 Resources 根路径；会拼接配表中文键，例如 Art/Art/UI/势力logo/贵族。")]
        [SerializeField] private string factionLogoResourceRoot = "Art/Art/UI/势力logo";

        [Header("势力 Logo 尺寸")]
        [Tooltip("势力 logo 在公文弹窗中的固定显示尺寸；加载不同素材后都会统一设置为这个宽高。")]
        [SerializeField] private Vector2 factionLogoSize = new Vector2(150f, 150f);

        [Header("\u516c\u6587\u6309\u94ae\uff1a\u4e24\u4e2a\u9009\u9879\u4e0e\u5904\u7406\u5b8c\u540e\u5f00\u653e\u7684\u57ce\u533a\u6309\u94ae")]
        [SerializeField] private Button optionAButton;
        [SerializeField] private Button optionBButton;
        [SerializeField] private Button cityExploreButton;

        [Header("公文按钮状态颜色：可点击浅色，不可点击深色")]
        [Tooltip("选项按钮可以点击时的颜色；用于修正 Button ColorBlock 或预制体颜色反向的问题。")]
        [SerializeField] private Color optionButtonInteractableColor = Color.white;
        [Tooltip("选项按钮不能点击时的颜色；需要比可点击颜色更暗，提示玩家当前选项尚未满足条件。")]
        [SerializeField] private Color optionButtonDisabledColor = new Color(0.32f, 0.32f, 0.32f, 1f);

        [Header("\u63d0\u4ea4\u680f\uff1a\u9700\u8981\u9053\u5177\u65f6\u63a5\u6536\u80cc\u5305\u5361\u724c")]
        [SerializeField] private GameObject submitPanel;
        [SerializeField] private DocumentSubmitSlot submitSlot;

        [Header("物品面板：公文需要提交物品时从桌面下方弹出")]
        [Tooltip("可选的场景内物品面板实例；为空时会在需要提交物品的公文打开时，从物品面板预制体创建。")]
        [SerializeField] private InventoryPanelView inventoryPanel;
        [Tooltip("物品面板预制体；进入需要提交物品的公文时才实例化，并放到公文弹窗同级最上层。")]
        [SerializeField] private InventoryPanelView inventoryPanelPrefab;
        [Tooltip("物品面板预制体的 Resources 路径；当 Inspector 未拖预制体时用它加载 Assets/Resources 下的 prefab。")]
        [SerializeField] private string inventoryPanelResourcePath = "Prefabs/UI/物品面板";

        [Header("\u516c\u6587\u9000\u51fa\u63d0\u793a\uff1a\u5168\u90e8\u5904\u7406\u540e\u663e\u793a")]
        [Tooltip("\u5168\u90e8\u516c\u6587\u5904\u7406\u5b8c\u6bd5\u540e\u624d\u663e\u793a\u7684\u63d0\u793a\u56fe\u7247\uff1b\u521d\u59cb\u5fc5\u987b\u5173\u95ed\uff0c\u7528\u4e8e\u63d0\u793a\u73a9\u5bb6\u5411\u53f3\u62d6\u51fa\u516c\u6587\u754c\u9762\u3002")]
        [SerializeField] private GameObject exitHintImage;
        [Tooltip("\u73a9\u5bb6\u5411\u53f3\u62d6\u62fd\u8d85\u8fc7\u8fd9\u4e2a\u8ddd\u79bb\u540e\uff0c\u5224\u5b9a\u516c\u6587\u754c\u9762\u9000\u51fa\u5e76\u5141\u8bb8\u8fdb\u5165\u57ce\u533a\u3002")]
        [SerializeField] private float dragExitDistance = 420f;
        [Tooltip("\u62d6\u62fd\u672a\u8fbe\u5230\u9000\u51fa\u8ddd\u79bb\u65f6\uff0c\u516c\u6587\u754c\u9762\u56de\u5f39\u5230\u6253\u5f00\u4f4d\u7f6e\u7684\u65f6\u957f\u3002")]
        [SerializeField] private float dragReturnDuration = 0.25f;

        [Header("主界面遮罩：公文打开时淡入，拖出关闭时淡出")]
        [Tooltip("公文打开时覆盖主界面的黑色遮罩图片；默认关闭，打开公文时从透明淡入。")]
        [SerializeField] private Image mainInterfaceMaskImage;
        [Tooltip("公文打开后主界面遮罩的目标透明度。")]
        [SerializeField] private float mainInterfaceMaskTargetAlpha = 0.7f;
        [Tooltip("主界面遮罩淡入和淡出的时长。")]
        [SerializeField] private float mainInterfaceMaskFadeDuration = 0.25f;

        [Header("打字机效果：公文正文与角色反馈")]
        [Tooltip("公文正文每秒显示的字符数；正文播放完毕后才显示选项。")]
        [SerializeField] private float bodyTypewriterCharactersPerSecond = 42f;
        [Tooltip("角色反馈每秒显示的字符数；反馈播放完毕后等待 1 秒再进入下一份公文。")]
        [SerializeField] private float feedbackTypewriterCharactersPerSecond = 36f;
        [Tooltip("角色反馈打字机播放完毕后，进入下一份公文前的停留时间。")]
        [SerializeField] private float feedbackHoldAfterTypewriterDuration = 1f;

        [Header("\u516c\u6587\u4eba\u7269\u5207\u6362\u8c03\u8bd5\u5feb\u7167")]
        [Tooltip("\u53ea\u8bfb\u8fd0\u884c\u65f6\u72b6\u6001\uff1a\u4e0a\u4e00\u4f4d\u63d0\u51fa\u8005\u9000\u573a\u6216\u4e0b\u4e00\u4f4d\u63d0\u51fa\u8005\u5165\u573a\u65f6\u4e3a\u771f\u3002")]
        [SerializeField] private bool isActorTransitioningSnapshot;

        [Header("\u516c\u6587\u62d6\u51fa\u8c03\u8bd5\u5feb\u7167")]
        [Tooltip("\u53ea\u8bfb\u8fd0\u884c\u65f6\u72b6\u6001\uff1a\u5168\u90e8\u516c\u6587\u5904\u7406\u5b8c\u6bd5\u540e\uff0c\u7b49\u5f85\u73a9\u5bb6\u5411\u53f3\u62d6\u51fa\u65f6\u4e3a\u771f\u3002")]
        [SerializeField] private bool waitingForDragExitSnapshot;
        [Tooltip("\u53ea\u8bfb\u8fd0\u884c\u65f6\u72b6\u6001\uff1a\u73a9\u5bb6\u6b63\u5728\u62d6\u52a8\u516c\u6587\u754c\u9762\u65f6\u4e3a\u771f\u3002")]
        [SerializeField] private bool isDraggingExitSnapshot;
        [Tooltip("\u53ea\u8bfb\u8fd0\u884c\u65f6\u72b6\u6001\uff1a\u5f53\u524d\u5411\u53f3\u62d6\u52a8\u7684\u6c34\u5e73\u8ddd\u79bb\u3002")]
        [SerializeField] private float currentExitDragOffsetSnapshot;

        private RuntimeDocumentQueueEntry currentEntry;
        private DocumentDefinition currentDocument;
        private bool waitingForContinue;
        private bool waitingForDragExit;
        private bool isDraggingExit;
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
        private Tween dragReturnTween;
        private Tween bodyTypewriterTween;
        private Tween mainInterfaceMaskTween;
        private CanvasGroup rootCanvasGroup;
        private Vector2 dragStartPointerPosition;
        private Vector2 leftScrollDragStartPosition;
        private Vector2 rightScrollDragStartPosition;
        private Vector2 contentDragStartPosition;
        private string bodyTypewriterFullText = string.Empty;
        private bool isBodyTypewriterPlaying;
        private bool isFeedbackTypewriterPlaying;
        private bool startBodyTypewriterWhenOpened;

        public bool IsDocumentFlowActive =>
            gameObject.activeInHierarchy && (currentDocument != null || currentEntry != null || isFeedbackTypewriterPlaying || waitingForContinue || waitingForDragExit || isActorTransitioningSnapshot);

        public bool IsActorTransitioning => isActorTransitioningSnapshot;

        public GameObject ExitHintImageObject => exitHintImage;

        public bool IsWaitingForDragExit => waitingForDragExit;

        public bool IsDraggingExit => isDraggingExitSnapshot;

        public float RightSideOffscreenOffset => rightSideOffscreenOffset;

        public GameObject MainInterfaceMaskObject => mainInterfaceMaskImage != null ? mainInterfaceMaskImage.gameObject : null;

        public GameObject InventoryPanelObject => inventoryPanel != null ? inventoryPanel.gameObject : null;

        public bool AllowsMainInterfaceMaskAutoBinding => true;

        public float MainInterfaceMaskTargetAlpha => mainInterfaceMaskTargetAlpha;

        public float BodyTypewriterCharactersPerSecond => bodyTypewriterCharactersPerSecond;

        public float FeedbackTypewriterCharactersPerSecond => feedbackTypewriterCharactersPerSecond;

        public float FeedbackHoldAfterTypewriterDuration => feedbackHoldAfterTypewriterDuration;

        public bool HidesOptionsUntilBodyTypewriterFinished => bodyTypewriterCharactersPerSecond > 0f;

        public bool ExitHintStartsImmediatelyAfterAllDocuments => true;

        public event Action DocumentFlowStateChanged;

        private void Awake()
        {
            ResolveDependencies();
            rootCanvasGroup = GetComponent<CanvasGroup>();
            AutoBindAnimationReferences();
            AutoBindExitHintImage();
            AutoBindFactionLogoImage();
            AutoBindMainInterfaceMask();
            ConfigureClickCatcher();
            CacheOpenLayout();
            CacheClosedLayout();
            HideExitHint();
            HideMainInterfaceMask(true);
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
            KillDragReturnTween();
            KillBodyTypewriterTween();
            KillMainInterfaceMaskTween();
            KillScrollTweens();
            isActorTransitioningSnapshot = false;
            SetDragExitState(false);
            isFeedbackTypewriterPlaying = false;

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
            SetDragExitState(false);
            HideExitHint();
            ShowMainInterfaceMask();
            SetText(titleText, title);
            PrepareBodyTypewriter(body);
            SetText(optionAText, optionA);
            SetText(optionBText, optionB);
            SetText(proposerFeedbackText, string.Empty);
            SetText(flowStatusText, string.Empty);
            SetButtonsInteractable(true);
            ClearStamps();
            HideSubmitPanel();

            gameObject.SetActive(true);
            SetRootCanvasVisible(true);
            CloseInstant();
            startBodyTypewriterWhenOpened = true;
            ScheduleOpenScroll();
            NotifyDocumentFlowStateChanged();
        }

        [ContextMenu("Hide")]
        public void Hide()
        {
            currentEntry = null;
            currentDocument = null;
            waitingForContinue = false;
            SetDragExitState(false);
            ClearTransientFeedback();
            HideExitHint();
            HideMainInterfaceMask(false);
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
            if (isBodyTypewriterPlaying)
            {
                CompleteBodyTypewriter();
                return;
            }

            if (isFeedbackTypewriterPlaying)
            {
                CompleteFeedbackTypewriter();
                return;
            }

            if (waitingForContinue)
            {
                ContinueAfterResolution();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!waitingForDragExit || eventData == null)
            {
                return;
            }

            KillDragReturnTween();
            KillScrollTweens();
            HideExitHint();
            isDraggingExit = true;
            isDraggingExitSnapshot = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out dragStartPointerPosition);

            leftScrollDragStartPosition = leftScrollEnd != null ? leftScrollEnd.anchoredPosition : Vector2.zero;
            rightScrollDragStartPosition = rightScrollEnd != null ? rightScrollEnd.anchoredPosition : Vector2.zero;
            contentDragStartPosition = contentRoot != null ? contentRoot.anchoredPosition : Vector2.zero;
            currentExitDragOffsetSnapshot = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!waitingForDragExit || !isDraggingExit || eventData == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var currentPointerPosition);

            var dragOffsetX = Mathf.Max(0f, currentPointerPosition.x - dragStartPointerPosition.x);
            ApplyDragOffset(dragOffsetX);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!waitingForDragExit || !isDraggingExit)
            {
                return;
            }

            isDraggingExit = false;
            isDraggingExitSnapshot = false;
            if (currentExitDragOffsetSnapshot >= dragExitDistance)
            {
                CompleteDragExit();
                return;
            }

            ReturnDragToOpenPosition();
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
            BeginDragExitWait();
            HideActorAlongEntryPath(() =>
            {
                isActorTransitioningSnapshot = false;
                NotifyDocumentFlowStateChanged();
            });
        }

        private void ShowDocument(RuntimeDocumentQueueEntry entry, DocumentDefinition document, bool playOpenAnimation, bool showProposer = true)
        {
            currentEntry = entry;
            currentDocument = document;
            waitingForContinue = false;
            SetDragExitState(false);
            lastSubmitAccepted = false;
            ClearTransientFeedback();
            HideExitHint();
            ShowMainInterfaceMask();
            SetText(titleText, document.Title);
            PrepareBodyTypewriter(BuildBodyTextWithRequirements(document));
            SetText(optionAText, document.OptionA.Text);
            SetText(optionBText, document.OptionB.Text);
            SetText(proposerFeedbackText, string.Empty);
            SetText(flowStatusText, "\u8bf7\u9009\u62e9\u5904\u7406\u65b9\u5f0f\u3002");
            ApplyDocumentBackground(document);
            ApplyFactionLogo(document);
            ClearStamps();
            ConfigureSubmitPanel(document);
            RefreshOptionLocks();
            SetOptionsVisible(false);

            if (showProposer)
            {
                ShowProposer(document);
            }
            gameObject.SetActive(true);
            SetRootCanvasVisible(true);
            if (playOpenAnimation)
            {
                CloseInstant();
                startBodyTypewriterWhenOpened = true;
                ScheduleOpenScroll();
            }
            else
            {
                OpenInstant();
                StartBodyTypewriter();
            }

            NotifyDocumentFlowStateChanged();
        }

        private void ResolveCurrentDocument(DocumentOptionType optionType)
        {
            if (isBodyTypewriterPlaying)
            {
                CompleteBodyTypewriter();
                return;
            }

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

                PlayFeedbackTypewriter(result.ProposerFeedbackText);
                suspicionPanel?.ShowDocumentChoiceImpact(result.MostAffectedFactionId, result.FactionFeedbackText);
                ShowStamp(optionType);
                SetButtonsInteractable(false);
                SetOptionsVisible(false);
                SetText(flowStatusText, "\u5df2\u76d6\u7ae0\u3002");
                currentEntry = null;
                currentDocument = null;
                SetText(
                    flowStatusText,
                    documentService != null && documentService.TryGetNextPendingDocument(out _, out _)
                        ? "\u5df2\u76d6\u7ae0\u3002\u63d0\u51fa\u8005\u6b63\u5728\u56de\u5e94\u3002"
                        : "\u5df2\u76d6\u7ae0\u3002\u63d0\u51fa\u8005\u6b63\u5728\u56de\u5e94\u3002");
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

        private void BeginDragExitWait()
        {
            currentEntry = null;
            currentDocument = null;
            waitingForContinue = false;
            SetButtonsInteractable(false);
            submitSlot?.Clear();
            lastSubmitAccepted = false;
            OpenInstant();
            SetDragExitState(true);
            ShowExitHint();
            SetText(flowStatusText, "\u5168\u90e8\u516c\u6587\u5df2\u5904\u7406\u3002\u5411\u53f3\u62d6\u51fa\u516c\u6587\u754c\u9762\u540e\u53ef\u8fdb\u5165\u57ce\u533a\u3002");
            NotifyDocumentFlowStateChanged();
        }

        private void CompleteDragExit()
        {
            KillDragReturnTween();
            KillScrollTweens();
            HideExitHint();
            HideMainInterfaceMask(false);
            SetDragExitState(false);

            if (contentGroup != null)
            {
                contentGroup.blocksRaycasts = false;
                contentGroup.interactable = false;
            }

            scrollSequence = DOTween.Sequence();
            if (leftScrollEnd != null)
            {
                scrollSequence.Join(leftScrollEnd.DOAnchorPos(
                    BuildNonReversingCloseTarget(leftScrollEnd.anchoredPosition, leftScrollClosedPosition),
                    scrollTweenDuration).SetEase(Ease.InCubic));
            }

            if (rightScrollEnd != null)
            {
                scrollSequence.Join(rightScrollEnd.DOAnchorPos(
                    BuildNonReversingCloseTarget(rightScrollEnd.anchoredPosition, rightScrollClosedPosition),
                    scrollTweenDuration).SetEase(Ease.InCubic));
            }

            if (contentRoot != null)
            {
                scrollSequence.Join(contentRoot.DOAnchorPos(
                    BuildNonReversingCloseTarget(contentRoot.anchoredPosition, contentClosedPosition),
                    scrollTweenDuration).SetEase(Ease.InCubic));
            }

            scrollSequence.OnComplete(EndDocumentFlow);
        }

        private static Vector2 BuildNonReversingCloseTarget(Vector2 currentPosition, Vector2 closedPosition)
        {
            return new Vector2(Mathf.Max(closedPosition.x, currentPosition.x), closedPosition.y);
        }

        private void PrepareBodyTypewriter(string body)
        {
            KillBodyTypewriterTween();
            bodyTypewriterFullText = body ?? string.Empty;
            if (bodyText != null)
            {
                bodyText.text = bodyTypewriterFullText;
                bodyText.maxVisibleCharacters = 0;
            }

            isBodyTypewriterPlaying = false;
            startBodyTypewriterWhenOpened = false;
            SetOptionsVisible(false);
        }

        private void StartBodyTypewriter()
        {
            KillBodyTypewriterTween();
            if (bodyText == null || string.IsNullOrEmpty(bodyTypewriterFullText) || bodyTypewriterCharactersPerSecond <= 0f)
            {
                CompleteBodyTypewriter();
                return;
            }

            bodyText.text = bodyTypewriterFullText;
            bodyText.maxVisibleCharacters = 0;
            isBodyTypewriterPlaying = true;
            var visibleCount = bodyTypewriterFullText.Length;
            var duration = visibleCount / Mathf.Max(1f, bodyTypewriterCharactersPerSecond);
            bodyTypewriterTween = DOTween
                .To(
                    () => bodyText.maxVisibleCharacters,
                    value => bodyText.maxVisibleCharacters = value,
                    visibleCount,
                    duration)
                .SetEase(Ease.Linear)
                .OnComplete(CompleteBodyTypewriter);
        }

        private void CompleteBodyTypewriter()
        {
            KillBodyTypewriterTween();
            isBodyTypewriterPlaying = false;
            startBodyTypewriterWhenOpened = false;
            if (bodyText != null)
            {
                bodyText.text = bodyTypewriterFullText;
                bodyText.maxVisibleCharacters = int.MaxValue;
            }

            SetOptionsVisible(true);
            RefreshOptionLocks();
        }

        private void PlayFeedbackTypewriter(string feedback)
        {
            isFeedbackTypewriterPlaying = true;
            if (sharedActorSlot == null)
            {
                OnFeedbackTypewriterFinished();
                return;
            }

            sharedActorSlot.ShowFeedbackTypewriter(
                feedback,
                feedbackTypewriterCharactersPerSecond,
                OnFeedbackTypewriterFinished);
        }

        private void CompleteFeedbackTypewriter()
        {
            if (sharedActorSlot != null && sharedActorSlot.IsFeedbackTypewriterPlaying)
            {
                sharedActorSlot.CompleteFeedbackTypewriter();
                return;
            }

            OnFeedbackTypewriterFinished();
        }

        private void OnFeedbackTypewriterFinished()
        {
            if (!isFeedbackTypewriterPlaying)
            {
                return;
            }

            isFeedbackTypewriterPlaying = false;
            waitingForContinue = true;
            SetText(
                flowStatusText,
                documentService != null && documentService.TryGetNextPendingDocument(out _, out _)
                    ? "\u5df2\u56de\u5e94\u3002\u6b63\u5728\u8fdb\u5165\u4e0b\u4e00\u4efd\u516c\u6587\u3002"
                    : "\u5df2\u56de\u5e94\u3002\u8bf7\u7a0d\u540e\u5411\u53f3\u62d6\u51fa\u516c\u6587\u754c\u9762\u3002");
            ScheduleAutoAdvanceAfterResolution();
            NotifyDocumentFlowStateChanged();
        }

        private void SetOptionsVisible(bool visible)
        {
            if (optionAButton != null)
            {
                optionAButton.gameObject.SetActive(visible);
            }

            if (optionBButton != null)
            {
                optionBButton.gameObject.SetActive(visible);
            }
        }

        private void ShowMainInterfaceMask()
        {
            if (mainInterfaceMaskImage == null)
            {
                return;
            }

            KillMainInterfaceMaskTween();
            var wasActive = mainInterfaceMaskImage.gameObject.activeSelf;
            mainInterfaceMaskImage.gameObject.SetActive(true);
            if (!wasActive)
            {
                SetMainInterfaceMaskAlpha(0f);
            }

            mainInterfaceMaskTween = mainInterfaceMaskImage
                .DOFade(Mathf.Clamp01(mainInterfaceMaskTargetAlpha), Mathf.Max(0f, mainInterfaceMaskFadeDuration))
                .SetEase(Ease.OutCubic);
        }

        private void HideMainInterfaceMask(bool immediate)
        {
            if (mainInterfaceMaskImage == null)
            {
                return;
            }

            KillMainInterfaceMaskTween();
            if (immediate || mainInterfaceMaskFadeDuration <= 0f)
            {
                SetMainInterfaceMaskAlpha(0f);
                mainInterfaceMaskImage.gameObject.SetActive(false);
                return;
            }

            mainInterfaceMaskTween = mainInterfaceMaskImage
                .DOFade(0f, mainInterfaceMaskFadeDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() => mainInterfaceMaskImage.gameObject.SetActive(false));
        }

        private void SetMainInterfaceMaskAlpha(float alpha)
        {
            if (mainInterfaceMaskImage == null)
            {
                return;
            }

            var color = mainInterfaceMaskImage.color;
            color.a = alpha;
            mainInterfaceMaskImage.color = color;
        }

        private void ReturnDragToOpenPosition()
        {
            KillDragReturnTween();
            var sequence = DOTween.Sequence();
            if (leftScrollEnd != null)
            {
                sequence.Join(leftScrollEnd.DOAnchorPos(leftScrollOpenedPosition, dragReturnDuration).SetEase(Ease.OutCubic));
            }

            if (rightScrollEnd != null)
            {
                sequence.Join(rightScrollEnd.DOAnchorPos(rightScrollOpenedPosition, dragReturnDuration).SetEase(Ease.OutCubic));
            }

            if (contentRoot != null)
            {
                sequence.Join(contentRoot.DOAnchorPos(contentOpenedPosition, dragReturnDuration).SetEase(Ease.OutCubic));
            }

            sequence.OnComplete(() => currentExitDragOffsetSnapshot = 0f);
            dragReturnTween = sequence;
        }

        private void ApplyDragOffset(float offsetX)
        {
            currentExitDragOffsetSnapshot = offsetX;
            var offset = new Vector2(offsetX, 0f);
            if (leftScrollEnd != null)
            {
                leftScrollEnd.anchoredPosition = leftScrollDragStartPosition + offset;
            }

            if (rightScrollEnd != null)
            {
                rightScrollEnd.anchoredPosition = rightScrollDragStartPosition + offset;
            }

            if (contentRoot != null)
            {
                contentRoot.anchoredPosition = contentDragStartPosition + offset;
            }
        }

        private void SetDragExitState(bool value)
        {
            waitingForDragExit = value;
            waitingForDragExitSnapshot = value;
            if (!value)
            {
                isDraggingExit = false;
                isDraggingExitSnapshot = false;
                currentExitDragOffsetSnapshot = 0f;
            }
        }

        private void ShowExitHint()
        {
            if (exitHintImage != null && waitingForDragExit)
            {
                exitHintImage.SetActive(true);
                exitHintImage.transform.SetAsLastSibling();
            }
        }

        private void HideExitHint()
        {
            if (exitHintImage != null)
            {
                exitHintImage.SetActive(false);
            }
        }

        private void EndDocumentFlow()
        {
            HideClosedDocumentVisuals();
            currentEntry = null;
            currentDocument = null;
            waitingForContinue = false;
            SetDragExitState(false);
            isActorTransitioningSnapshot = false;
            ClearTransientFeedback();
            HideExitHint();
            submitSlot?.Clear();
            SetText(flowStatusText, "\u672c\u56de\u5408\u516c\u6587\u5df2\u5168\u90e8\u5904\u7406\u3002");
            if (cityExploreButton != null)
            {
                cityExploreButton.interactable = true;
            }

            gameObject.SetActive(false);
            NotifyDocumentFlowStateChanged();
        }

        private void HideClosedDocumentVisuals()
        {
            SetRootCanvasVisible(false);
            if (contentGroup != null)
            {
                contentGroup.alpha = 0f;
                contentGroup.blocksRaycasts = false;
                contentGroup.interactable = false;
            }
        }

        private void SetRootCanvasVisible(bool visible)
        {
            if (rootCanvasGroup == null)
            {
                rootCanvasGroup = GetComponent<CanvasGroup>();
            }

            if (rootCanvasGroup == null)
            {
                return;
            }

            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.blocksRaycasts = visible;
            rootCanvasGroup.interactable = visible;
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
                scrollSequence.Join(leftScrollEnd.DOAnchorPos(leftScrollOpenedPosition, scrollTweenDuration).SetEase(Ease.OutCubic));
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

            scrollSequence.OnComplete(OnScrollOpened);
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
                scrollSequence.Join(leftScrollEnd.DOAnchorPos(leftScrollClosedPosition, scrollTweenDuration).SetEase(Ease.InCubic));
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
            var rightOffset = new Vector2(Mathf.Max(100f, rightSideOffscreenOffset), 0f);
            leftScrollClosedPosition = leftScrollEnd != null ? leftScrollOpenedPosition + rightOffset : Vector2.zero;
            rightScrollClosedPosition = rightScrollEnd != null ? rightScrollOpenedPosition + rightOffset : Vector2.zero;
            contentClosedPosition = contentRoot != null ? contentOpenedPosition + rightOffset : Vector2.zero;
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

            EnsureInventoryPanelInstance();
            inventoryPanel?.ShowForDocumentSubmission();
            submitSlot?.Configure(
                optionARequiresItem ? optionAItemId : optionBItemId,
                optionARequiresItem ? optionACount : optionBCount,
                optionARequiresItem && optionBRequiresItem ? optionBItemId : string.Empty,
                optionARequiresItem && optionBRequiresItem ? optionBCount : 0);
            submitSlot?.ConfigureAnimationAnchors(leftScrollEnd, contentRoot);
        }

        private void HideSubmitPanel()
        {
            if (submitPanel != null)
            {
                submitPanel.SetActive(false);
            }

            inventoryPanel?.HideForDocumentSubmission();
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

        private void ApplyFactionLogo(DocumentDefinition document)
        {
            AutoBindFactionLogoImage();
            if (factionLogoImage == null)
            {
                return;
            }

            var logoName = ResolveFactionLogoName(document);
            if (string.IsNullOrEmpty(logoName))
            {
                factionLogoImage.enabled = false;
                factionLogoImage.sprite = null;
                return;
            }

            var sprite = Resources.Load<Sprite>($"{factionLogoResourceRoot}/{logoName}");
            if (sprite == null)
            {
                Debug.LogWarning($"未找到势力 logo 素材：{factionLogoResourceRoot}/{logoName}", this);
                factionLogoImage.enabled = false;
                factionLogoImage.sprite = null;
                return;
            }

            factionLogoImage.sprite = sprite;
            factionLogoImage.preserveAspect = true;
            ApplyFactionLogoFixedSize();
            factionLogoImage.enabled = true;
        }

        private void ApplyFactionLogoFixedSize()
        {
            if (factionLogoImage == null)
            {
                return;
            }

            var rectTransform = factionLogoImage.rectTransform;
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(
                    Mathf.Max(0f, factionLogoSize.x),
                    Mathf.Max(0f, factionLogoSize.y));
            }
        }

        private string ResolveFactionLogoName(DocumentDefinition document)
        {
            if (document == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(document.FactionLogoName))
            {
                return document.FactionLogoName;
            }

            if (documentService != null &&
                documentService.TryGetCharacter(document.ProposerCharacterId, out var character))
            {
                return GetDefaultLogoNameForFaction(character.FactionId);
            }

            return string.Empty;
        }

        private static string GetDefaultLogoNameForFaction(string factionId)
        {
            switch (factionId)
            {
                case "noble":
                    return "贵族";
                case "academy":
                    return "学院";
                case "church":
                    return "教会";
                case "civilian":
                    return "工会";
                default:
                    return string.Empty;
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

        private void EnsureInventoryPanelInstance()
        {
            if (inventoryPanel != null)
            {
                PlaceInventoryPanelAboveDocument();
                return;
            }

            if (inventoryPanelPrefab == null && !string.IsNullOrEmpty(inventoryPanelResourcePath))
            {
                inventoryPanelPrefab = Resources.Load<InventoryPanelView>(inventoryPanelResourcePath);
            }

            if (inventoryPanelPrefab == null)
            {
                Debug.LogWarning("未找到物品面板预制体，无法在公文提交物品时创建物品面板。", this);
                return;
            }

            var parent = transform.parent != null ? transform.parent : transform;
            inventoryPanel = Instantiate(inventoryPanelPrefab, parent, false);
            inventoryPanel.name = inventoryPanelPrefab.gameObject.name;
            PlaceInventoryPanelAboveDocument();
        }

        private void PlaceInventoryPanelAboveDocument()
        {
            if (inventoryPanel == null)
            {
                return;
            }

            inventoryPanel.transform.SetAsLastSibling();
        }

        private void AutoBindAnimationReferences()
        {
            if (contentRoot != null && contentGroup == null)
            {
                contentGroup = contentRoot.GetComponent<CanvasGroup>();
            }

            submitSlot?.ConfigureAnimationAnchors(leftScrollEnd, contentRoot);
        }

        private void AutoBindExitHintImage()
        {
            if (exitHintImage == null)
            {
                var hintTransform = transform.Find("提示图片");
                if (hintTransform != null)
                {
                    exitHintImage = hintTransform.gameObject;
                }
            }

            if (exitHintImage == null)
            {
                return;
            }

            var hintGraphic = exitHintImage.GetComponent<Graphic>();
            if (hintGraphic != null)
            {
                hintGraphic.raycastTarget = false;
            }
        }

        private void AutoBindFactionLogoImage()
        {
            if (factionLogoImage != null)
            {
                return;
            }

            var logoTransform = transform.Find("内容根节点/内容视口/势力logo");
            if (logoTransform == null)
            {
                logoTransform = FindChildByName(transform, "势力logo");
            }

            if (logoTransform != null)
            {
                factionLogoImage = logoTransform.GetComponent<Image>();
            }
        }

        private void AutoBindMainInterfaceMask()
        {
            if (mainInterfaceMaskImage != null)
            {
                return;
            }

            var images = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var image in images)
            {
                if (image != null && image.gameObject.name == "主界面遮罩")
                {
                    mainInterfaceMaskImage = image;
                    return;
                }
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

        private void KillDragReturnTween()
        {
            dragReturnTween?.Kill();
            dragReturnTween = null;
        }

        private void KillBodyTypewriterTween()
        {
            bodyTypewriterTween?.Kill();
            bodyTypewriterTween = null;
        }

        private void KillMainInterfaceMaskTween()
        {
            mainInterfaceMaskTween?.Kill();
            mainInterfaceMaskTween = null;
            mainInterfaceMaskImage?.DOKill();
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

            var delay = Mathf.Max(feedbackHoldAfterTypewriterDuration, autoAdvanceAfterStampDuration);
            if (delay <= 0f)
            {
                ContinueAfterResolution();
                return;
            }

            pendingAutoAdvanceTween = DOVirtual.DelayedCall(delay, ContinueAfterResolution);
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

        private void OnScrollOpened()
        {
            EnableContentInteraction();
            if (startBodyTypewriterWhenOpened)
            {
                StartBodyTypewriter();
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
                SetButtonInteractable(optionAButton, interactable);
            }

            if (optionBButton != null)
            {
                SetButtonInteractable(optionBButton, interactable);
            }
        }

        private void SetButtonInteractable(Button button, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            var colors = button.colors;
            colors.normalColor = optionButtonInteractableColor;
            colors.highlightedColor = optionButtonInteractableColor;
            colors.selectedColor = optionButtonInteractableColor;
            colors.disabledColor = optionButtonDisabledColor;
            button.colors = colors;

            button.interactable = interactable;
            if (button.targetGraphic != null)
            {
                button.targetGraphic.color = interactable ? optionButtonInteractableColor : optionButtonDisabledColor;
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

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            var children = root.GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (child != null && child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
