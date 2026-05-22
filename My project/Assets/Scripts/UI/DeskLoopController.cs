using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class DeskLoopController : MonoBehaviour
    {
        [Header("依赖服务：驱动桌面最小回合循环")]
        [Tooltip("运行时数据服务；用于读取当前回合、报纸和队列状态。")]
        [SerializeField] private RuntimeDataService runtimeDataService;
        [Tooltip("回合服务；用于推进当前回合。")]
        [SerializeField] private RoundService roundService;
        [Tooltip("任务服务；用于处理回合开始和结束的任务阶段内容。")]
        [SerializeField] private TaskService taskService;
        [Tooltip("剧情服务；用于播放当前回合剧情队列。")]
        [SerializeField] private StoryService storyService;
        [Tooltip("公文服务；用于生成并读取当前回合公文队列。")]
        [SerializeField] private DocumentService documentService;

        [Header("界面引用：流程按钮和面板")]
        [Tooltip("公文弹窗；点击处理公文时逐份展示当前回合公文。")]
        [SerializeField] private DocumentPopupPanelView documentPopupPanel;
        [Tooltip("报纸面板；用于显示上一回合结算。")]
        [SerializeField] private NewspaperPanelView newspaperPanel;
        [Tooltip("播放剧情或继续剧情队列的按钮。")]
        [SerializeField] private Button storyButton;
        [Tooltip("处理当前回合公文的按钮。")]
        [SerializeField] private Button documentButton;
        [Tooltip("结束当前回合并进入下一回合的按钮。")]
        [SerializeField] private Button endRoundButton;
        [Tooltip("查看上一回合报纸的按钮。")]
        [SerializeField] private Button newspaperButton;
        [Tooltip("桌面流程状态文本；用于显示当前可执行动作。")]
        [SerializeField] private TMP_Text statusText;
        [Tooltip("公文前剧情人物立绘框；只允许点击该人物进入公文前剧情。")]
        [SerializeField] private SharedActorSlotView sharedActorSlot;

        private int roundWaitingForAdvance;
        private bool waitingToAdvanceAfterEndStories;
        private int roundPendingAdvanceAfterEndStories;
        private RuntimeStoryQueueEntry pendingBeforeDocumentStory;
        private bool waitingForBeforeDocumentActorClick;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void Start()
        {
            BeginCurrentRound();
            RefreshButtons();
        }

        private void OnEnable()
        {
            if (storyService != null)
            {
                storyService.StoryChanged += HandleStoryChanged;
            }

            if (documentService != null)
            {
                documentService.DocumentsChanged += RefreshButtons;
            }

            if (documentPopupPanel != null)
            {
                documentPopupPanel.DocumentFlowStateChanged += RefreshButtons;
            }

            if (sharedActorSlot != null)
            {
                sharedActorSlot.Clicked += HandleSharedActorSlotClicked;
            }
        }

        private void OnDisable()
        {
            if (storyService != null)
            {
                storyService.StoryChanged -= HandleStoryChanged;
            }

            if (documentService != null)
            {
                documentService.DocumentsChanged -= RefreshButtons;
            }

            if (documentPopupPanel != null)
            {
                documentPopupPanel.DocumentFlowStateChanged -= RefreshButtons;
            }

            if (sharedActorSlot != null)
            {
                sharedActorSlot.Clicked -= HandleSharedActorSlotClicked;
            }
        }

        public void StartOrContinueStoryQueue()
        {
            ResolveDependencies();
            if (IsDocumentFlowActive())
            {
                SetStatus("正在处理公文，请先完成当前公文。");
                RefreshButtons();
                return;
            }

            newspaperPanel?.Hide();
            storyService?.Continue();
            RefreshButtons();
        }

        public void BeginDocumentFlow()
        {
            ResolveDependencies();
            if (IsDocumentFlowActive())
            {
                SetStatus("正在处理公文，请先完成当前公文。");
                RefreshButtons();
                return;
            }

            if (HasActiveStory())
            {
                SetStatus("仍有剧情正在播放，请先结束剧情。");
                RefreshButtons();
                return;
            }

            if (HasQueuedGameplayStories() || HasPendingBeforeDocumentStory())
            {
                SetStatus("仍有剧情或公文前人物未处理，请先完成剧情。");
                TryShowBeforeDocumentActor();
                RefreshButtons();
                return;
            }

            newspaperPanel?.Hide();
            documentService?.GenerateCurrentRoundDocumentQueue();
            documentPopupPanel?.BeginDocumentFlow();
            RefreshButtons();
        }

        public void EndCurrentRound()
        {
            ResolveDependencies();
            if (IsDocumentFlowActive())
            {
                SetStatus("正在处理公文，请先完成当前公文。");
                RefreshButtons();
                return;
            }

            if (runtimeDataService == null || roundService == null)
            {
                SetStatus("缺少回合服务，无法结束回合。");
                return;
            }

            if (HasActiveStory())
            {
                SetStatus("仍有剧情正在播放，请先继续剧情。");
                return;
            }

            if (HasPendingDocuments())
            {
                SetStatus("仍有待处理公文，请先处理公文。");
                return;
            }

            var endingRound = runtimeDataService.Data.CurrentRound;
            if (roundWaitingForAdvance != endingRound)
            {
                taskService?.ProcessCurrentRoundEnd();
                roundWaitingForAdvance = endingRound;
                if (HasQueuedStories(RuntimeStoryQueueTiming.StageEnd))
                {
                    waitingToAdvanceAfterEndStories = true;
                    roundPendingAdvanceAfterEndStories = endingRound;
                    storyService?.StartNextQueuedStory(RuntimeStoryQueueTiming.StageEnd);
                    SetStatus("已进入回合结束剧情，请先播放剧情后再推进回合。");
                    RefreshButtons();
                    return;
                }
            }

            runtimeDataService.Data.EnsureNewspaperEntry(endingRound, "本回合事务已结算。");

            var advanced = roundService.NextRound();
            if (!advanced)
            {
                SetStatus("已到灾难最后一回合，无法继续推进。");
                RefreshButtons();
                return;
            }

            BeginCurrentRound();
            roundWaitingForAdvance = 0;
            SetStatus($"进入第 {runtimeDataService.Data.CurrentRound} 回合。");
            RefreshButtons();
        }

        public void ShowPreviousRoundNewspaper()
        {
            if (IsDocumentFlowActive())
            {
                SetStatus("正在处理公文，请先完成当前公文。");
                RefreshButtons();
                return;
            }

            newspaperPanel?.ShowPreviousRound();
        }

        public void HideNewspaper()
        {
            newspaperPanel?.Hide();
        }

        private void BeginCurrentRound()
        {
            ResolveDependencies();
            taskService?.ProcessCurrentRoundStart();
            documentService?.GenerateCurrentRoundDocumentQueue();
            if (HasQueuedStories(RuntimeStoryQueueTiming.StageStart))
            {
                storyService?.StartNextQueuedStory(RuntimeStoryQueueTiming.StageStart);
            }

            roundWaitingForAdvance = 0;
            SetStatus(runtimeDataService != null
                ? $"第 {runtimeDataService.Data.CurrentRound} 回合：先播放剧情，再处理公文。"
                : "桌面流程已准备。");
            TryShowBeforeDocumentActor();
        }

        private void RefreshButtons()
        {
            var isDocumentFlowActive = IsDocumentFlowActive();
            var hasActiveStory = HasActiveStory();
            var hasQueuedGameplayStories = HasQueuedGameplayStories();
            var hasPendingBeforeDocumentStory = HasPendingBeforeDocumentStory();
            var hasPendingDocuments = HasPendingDocuments();
            SetButtonInteractable(storyButton, !isDocumentFlowActive && !hasPendingBeforeDocumentStory && (hasActiveStory || hasQueuedGameplayStories));
            SetButtonInteractable(documentButton, !isDocumentFlowActive && !hasActiveStory && !hasQueuedGameplayStories && !hasPendingBeforeDocumentStory && hasPendingDocuments);
            SetButtonInteractable(endRoundButton, !isDocumentFlowActive && !hasActiveStory && !hasQueuedGameplayStories && !hasPendingBeforeDocumentStory && !hasPendingDocuments);
            SetButtonInteractable(newspaperButton, !isDocumentFlowActive && HasPreviousNewspaper());
        }

        private bool HasActiveStory()
        {
            return storyService != null && storyService.CurrentPlayback != null;
        }

        private bool HasQueuedStories()
        {
            if (runtimeDataService == null)
            {
                return false;
            }

            var currentRound = runtimeDataService.Data.CurrentRound;
            foreach (var entry in runtimeDataService.Data.StoryQueue)
            {
                if (entry.QueuedRound <= currentRound)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasQueuedStories(RuntimeStoryQueueTiming timing)
        {
            if (runtimeDataService == null)
            {
                return false;
            }

            var currentRound = runtimeDataService.Data.CurrentRound;
            foreach (var entry in runtimeDataService.Data.StoryQueue)
            {
                if (entry.QueuedRound <= currentRound && entry.Timing == timing)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasQueuedGameplayStories()
        {
            return HasQueuedStories(RuntimeStoryQueueTiming.StageEnd) ||
                HasQueuedStories(RuntimeStoryQueueTiming.StageStart);
        }

        private bool HasPendingBeforeDocumentStory()
        {
            return waitingForBeforeDocumentActorClick ||
                HasQueuedStories(RuntimeStoryQueueTiming.BeforeDocument);
        }

        private bool HasPendingDocuments()
        {
            if (documentService == null)
            {
                return false;
            }

            return documentService.TryGetNextPendingDocument(out _, out _);
        }

        private bool HasPreviousNewspaper()
        {
            return runtimeDataService != null &&
                runtimeDataService.Data.TryGetNewspaper(runtimeDataService.Data.CurrentRound - 1, out _);
        }

        private bool IsDocumentFlowActive()
        {
            return documentPopupPanel != null && documentPopupPanel.IsDocumentFlowActive;
        }

        private void HandleStoryChanged()
        {
            if (TryStartNextRequiredStory())
            {
                RefreshButtons();
                return;
            }

            TryAdvanceAfterEndStories();
            TryShowBeforeDocumentActor();
            RefreshButtons();
        }

        private bool TryStartNextRequiredStory()
        {
            if (HasActiveStory())
            {
                return false;
            }

            if (waitingToAdvanceAfterEndStories && HasQueuedStories(RuntimeStoryQueueTiming.StageEnd))
            {
                storyService?.StartNextQueuedStory(RuntimeStoryQueueTiming.StageEnd);
                return true;
            }

            if (!waitingToAdvanceAfterEndStories && HasQueuedStories(RuntimeStoryQueueTiming.StageStart))
            {
                storyService?.StartNextQueuedStory(RuntimeStoryQueueTiming.StageStart);
                return true;
            }

            return false;
        }

        private void TryAdvanceAfterEndStories()
        {
            if (!waitingToAdvanceAfterEndStories || HasActiveStory() || HasQueuedStories(RuntimeStoryQueueTiming.StageEnd))
            {
                return;
            }

            waitingToAdvanceAfterEndStories = false;
            AdvanceAfterRoundEndStories(roundPendingAdvanceAfterEndStories);
        }

        private void AdvanceAfterRoundEndStories(int endingRound)
        {
            if (runtimeDataService == null || roundService == null)
            {
                return;
            }

            runtimeDataService.Data.EnsureNewspaperEntry(endingRound, "本回合事务已结算。");
            var advanced = roundService.NextRound();
            if (!advanced)
            {
                SetStatus("已到灾难最后一回合，无法继续推进。");
                RefreshButtons();
                return;
            }

            BeginCurrentRound();
            roundWaitingForAdvance = 0;
            SetStatus($"进入第 {runtimeDataService.Data.CurrentRound} 回合。");
            RefreshButtons();
        }

        private void TryShowBeforeDocumentActor()
        {
            if (HasActiveStory() ||
                HasQueuedGameplayStories() ||
                waitingForBeforeDocumentActorClick ||
                !TryGetNextBeforeDocumentStory(out var entry))
            {
                return;
            }

            pendingBeforeDocumentStory = entry;
            waitingForBeforeDocumentActorClick = true;
            var characterId = GetBeforeDocumentCharacterId(entry);
            if (sharedActorSlot != null)
            {
                if (documentService != null &&
                    documentService.TryGetCharacter(characterId, out var character))
                {
                    sharedActorSlot.ShowActor(character.CharacterName, "公文前剧情", null);
                }
                else
                {
                    sharedActorSlot.ShowActor(string.IsNullOrEmpty(characterId) ? "公文前人物" : characterId, "公文前剧情", null);
                }
            }

            SetStatus("公文前人物已出现，请点击人物立绘进入剧情。");
        }

        private void HandleSharedActorSlotClicked()
        {
            if (!waitingForBeforeDocumentActorClick || pendingBeforeDocumentStory == null)
            {
                return;
            }

            waitingForBeforeDocumentActorClick = false;
            pendingBeforeDocumentStory = null;
            sharedActorSlot?.HideToRight();
            storyService?.StartNextQueuedStory(RuntimeStoryQueueTiming.BeforeDocument);
            RefreshButtons();
        }

        private bool TryGetNextBeforeDocumentStory(out RuntimeStoryQueueEntry entry)
        {
            entry = null;
            if (runtimeDataService == null)
            {
                return false;
            }

            var currentRound = runtimeDataService.Data.CurrentRound;
            foreach (var candidate in runtimeDataService.Data.StoryQueue)
            {
                if (candidate.QueuedRound <= currentRound && candidate.Timing == RuntimeStoryQueueTiming.BeforeDocument)
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }

        private string GetBeforeDocumentCharacterId(RuntimeStoryQueueEntry entry)
        {
            if (entry == null || taskService == null)
            {
                return string.Empty;
            }

            foreach (var stage in taskService.GetStages(entry.TaskId))
            {
                if (stage.TaskStageId == entry.TaskStageId)
                {
                    return stage.BeforeDocumentCharacterId;
                }
            }

            return string.Empty;
        }

        private void ResolveDependencies()
        {
            if (runtimeDataService == null)
            {
                runtimeDataService = FindFirstObjectByType<RuntimeDataService>();
            }

            if (roundService == null)
            {
                roundService = FindFirstObjectByType<RoundService>();
            }

            if (taskService == null)
            {
                taskService = FindFirstObjectByType<TaskService>();
            }

            if (storyService == null)
            {
                storyService = FindFirstObjectByType<StoryService>();
            }

            if (documentService == null)
            {
                documentService = FindFirstObjectByType<DocumentService>();
            }

            if (documentPopupPanel == null)
            {
                documentPopupPanel = FindFirstObjectByType<DocumentPopupPanelView>(FindObjectsInactive.Include);
            }

            if (newspaperPanel == null)
            {
                newspaperPanel = FindFirstObjectByType<NewspaperPanelView>(FindObjectsInactive.Include);
            }

            if (sharedActorSlot == null)
            {
                sharedActorSlot = FindFirstObjectByType<SharedActorSlotView>(FindObjectsInactive.Include);
            }
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
            {
                statusText.text = value ?? string.Empty;
            }
        }

        private static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }
}
