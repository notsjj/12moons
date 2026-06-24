using System;
using DG.Tweening;
using TMPro;
using TwelveMoons.City;
using TwelveMoons.Core;
using TwelveMoons.Core.Runtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [Tooltip("城区建筑服务；用于结束城区回合时自动结算未手动领取的建筑产出。为空时运行时自动查找。")]
        [SerializeField] private CityBuildingService cityBuildingService;
        [Header("场景切换：处理完公文后进入城区")]
        [Tooltip("游戏入口对象；用于在当前回合公文全部处理完后，从桌面切换到城区界面。")]
        [SerializeField] private GameEntry gameEntry;

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
        [Tooltip("进入城区按钮；只有在当前没有剧情、公文前角色、公文队列和公文弹窗时才允许点击。")]
        [SerializeField] private Button cityButton;
        [Tooltip("桌面流程状态文本；用于显示当前可执行动作。")]
        [SerializeField] private TMP_Text statusText;
        [Tooltip("公文前剧情人物立绘框；只允许点击该人物进入公文前剧情。")]
        [SerializeField] private SharedActorSlotView sharedActorSlot;
        [Header("进入城区：关闭物品栏")]
        [Tooltip("物品面板；进入城区时会主动隐藏，避免桌面物品栏残留在城区界面上。为空时运行时自动查找。")]
        [SerializeField] private InventoryPanelView inventoryPanel;
        [Header("进入城区同步过场：LoadingPanel 与摄像机共用时长")]
        [Tooltip("从 LoadingPanel 打开到关闭的总时长，同时也是城区摄像机入场移动时长；数值越大，整个过场越慢。")]
        [SerializeField, Min(0.01f)] private float synchronizedEntryTransitionDuration = 2.5f;
        [SerializeField] private BaseSceneUIBootstrap uiBootstrap;

        [Header("剧情进出场：黑场面板淡入淡出")]
        [Tooltip("启用后，普通剧情开始和结束时会复用黑场面板进行淡入淡出过渡。")]
        [SerializeField] private bool storyBlackFadeEnabled = true;
        [Tooltip("普通剧情进入和退出时，黑场淡入或淡出的单段时长。数值越大，过渡越慢。")]
        [SerializeField, Min(0f)] private float storyBlackFadeDuration = 0.35f;
        
        [Header("只读快照：桌面流程状态")]
        [Tooltip("运行时只读快照；显示当前桌面流程进度。")]
        [SerializeField] private string deskFlowSnapshot;
        [Tooltip("启用后，运行时按子物体名称自动寻找“公文按钮”“报纸按钮”“城区按钮”，并绑定到现有桌面流程方法；不会修改按钮布局。")]
        [SerializeField] private bool autoBindWorkflowButtons = true;

        [Header("只读快照：桌面按钮后端关联")]
        [Tooltip("运行时只读快照；显示公文按钮、报纸按钮、城区按钮是否已经关联到 DeskLoopController。")]
        [SerializeField] private string buttonBindingSnapshot;

        [Header("\u57ce\u533a\u6309\u94ae\u906e\u7f69\u52a8\u753b\uff1a\u70b9\u51fb\u540e\u5148\u5de6\u53f3\u62c9\u5f00")]
        [Tooltip("\u542f\u7528\u540e\uff0c\u70b9\u51fb\u57ce\u533a\u6309\u94ae\u4f1a\u5148\u5c06\u6309\u94ae\u4e0b\u540d\u79f0\u5305\u542b\u201c\u906e\u7f69\u201d\u6216 Mask \u7684\u4e24\u4e2a\u5b50\u7269\u4f53\u5206\u522b\u5411\u5de6\u53f3\u62c9\u5f00\uff0c\u518d\u8fdb\u5165 LoadingPanel \u8fc7\u573a\u3002")]
        [SerializeField] private bool playCityButtonMaskReveal = true;
        [Tooltip("\u57ce\u533a\u6309\u94ae\u4e24\u4e2a\u906e\u7f69\u5404\u81ea\u5411\u5916\u62c9\u5f00\u7684\u8ddd\u79bb\uff1b\u53ea\u6539\u906e\u7f69\u5b50\u7269\u4f53\u7684 anchoredPosition\uff0c\u4e0d\u6539\u6309\u94ae\u5e03\u5c40\u3002")]
        [SerializeField, Min(0f)] private float cityButtonMaskRevealDistance = 360f;
        [Tooltip("\u57ce\u533a\u6309\u94ae\u906e\u7f69\u62c9\u5f00\u52a8\u753b\u65f6\u957f\uff1b\u52a8\u753b\u7ed3\u675f\u540e\u624d\u663e\u793a LoadingPanel\u3002")]
        [SerializeField, Min(0f)] private float cityButtonMaskRevealDuration = 0.5f;
        [Tooltip("\u57ce\u533a\u6309\u94ae\u906e\u7f69\u62c9\u5f00\u52a8\u753b\u7684\u7f13\u52a8\u66f2\u7ebf\u3002")]
        [SerializeField] private Ease cityButtonMaskRevealEase = Ease.OutCubic;
        [Header("\u53ea\u8bfb\u5feb\u7167\uff1a\u57ce\u533a\u6309\u94ae\u906e\u7f69")]
        [Tooltip("\u8fd0\u884c\u65f6\u53ea\u8bfb\u5feb\u7167\uff1b\u663e\u793a\u662f\u5426\u627e\u5230\u57ce\u533a\u6309\u94ae\u4e0b\u7684\u4e24\u4e2a\u906e\u7f69\u5b50\u7269\u4f53\u3002")]
        [SerializeField] private string cityButtonMaskSnapshot;

        private int roundWaitingForAdvance;
        private bool waitingToAdvanceAfterEndStories;
        private int roundPendingAdvanceAfterEndStories;
        private RuntimeStoryQueueEntry pendingBeforeDocumentStory;
        private bool waitingForBeforeDocumentActorClick;
        private bool isEnteringCityWithTransition;
        private CityCameraController cityCameraController;
        private Sequence cityButtonMaskRevealSequence;
        private readonly Dictionary<RectTransform, Vector2> cityButtonMaskClosedPositions = new Dictionary<RectTransform, Vector2>();
        private bool wasDocumentFlowActive;
        private bool cityButtonMasksAreOpenAfterDocuments;
        private bool hasDocumentFlowBeenStarted;
        private bool hasStartedInitialRoundFlow;
        private bool observedStoryActiveForBlackFade;
        private bool storyBlackFadeRunning;
        private static bool holdStoryPanelVisibleDuringTransition;

        public static bool HoldStoryPanelVisibleDuringTransition => holdStoryPanelVisibleDuringTransition;

        public static void BeginStoryPanelVisibleHold()
        {
            holdStoryPanelVisibleDuringTransition = true;
        }

        public static void EndStoryPanelVisibleHold()
        {
            holdStoryPanelVisibleDuringTransition = false;
        }

        public bool OpeningTutorialEnabled => true;

        public string OpeningTutorialSnapshot => deskFlowSnapshot;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void Start()
        {
            BeginCurrentRoundFromEntry();
            SetCityButtonMasksClosedInstantly();
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

            RegisterDocumentPopupStateListener(documentPopupPanel);

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

            UnregisterDocumentPopupStateListener(documentPopupPanel);

            if (sharedActorSlot != null)
            {
                sharedActorSlot.Clicked -= HandleSharedActorSlotClicked;
            }

            KillCityButtonMaskRevealSequence();
            EndStoryPanelVisibleHold();
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
            SetCityButtonMasksClosedInstantly();
            documentService?.GenerateCurrentRoundDocumentQueue();
            EnsureDocumentPopupVisible();
            if (documentPopupPanel == null)
            {
                SetStatus("未找到公文界面，无法打开当前回合公文。");
                RefreshButtons();
                return;
            }

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

        /// <summary>
        /// 城区 HUD 专用入口：带黑场过渡的回合推进。
        /// 播放黑场淡入 → 切换回桌面 → 推进回合 → 初始化新回合 → 黑场淡出。
        /// </summary>
        public void EndRoundFromCityView()
        {
            ResolveDependencies();
            if (runtimeDataService == null || roundService == null)
            {
                SetStatus("缺少回合服务，无法推进回合。");
                return;
            }

            var runner = GetActiveCoroutineRunner();
            if (runner == null)
            {
                SetStatus("\u7f3a\u5c11\u6fc0\u6d3b\u7684\u534f\u7a0b\u627f\u8f7d\u5bf9\u8c61\uff0c\u65e0\u6cd5\u4ece\u57ce\u533a\u63a8\u8fdb\u4e0b\u4e00\u56de\u5408\u3002");
                return;
            }

            runner.StartCoroutine(PlayEndRoundTransition());
        }

        private MonoBehaviour GetActiveCoroutineRunner()
        {
            if (uiBootstrap != null && uiBootstrap.isActiveAndEnabled)
            {
                return uiBootstrap;
            }

            if (gameEntry != null && gameEntry.isActiveAndEnabled)
            {
                return gameEntry;
            }

            var context = FindFirstObjectByType<BaseSceneUIContext>(FindObjectsInactive.Include);
            if (context != null && context.isActiveAndEnabled)
            {
                return context;
            }

            return isActiveAndEnabled ? this : null;
        }

        private IEnumerator PlayEndRoundTransition()
        {
            if (QueueAndStartScheduledStories(StoryTriggerUnitIds.ExploreAfter, RuntimeStoryQueueTiming.ExploreAfter))
            {
                RefreshButtons();
                while (HasActiveStory() || HasQueuedStories(RuntimeStoryQueueTiming.ExploreAfter))
                {
                    yield return null;
                }
            }

            var blackPanel = uiBootstrap != null ? uiBootstrap.ShowBlackScreenPanel() : null;
            if (blackPanel != null)
            {
                yield return blackPanel.FadeIn(0.3f);
            }

            ResetCityCameraToGlobalView();
            uiBootstrap?.ShowDesk();
            gameEntry?.ShowDesk();

            var endingRound = runtimeDataService.Data.CurrentRound;
            var buildingSettlementText = CollectCityBuildingSettlementText(endingRound);
            var documentRewardText = BuildDocumentRewardSettlementText(endingRound);
            runtimeDataService.Data.EnsureNewspaperEntry(endingRound, "本回合事务已结算。");

            var advanced = roundService.NextRound();
            if (advanced)
            {
                BeginCurrentRound();
                SetStatus($"进入第 {runtimeDataService.Data.CurrentRound} 回合。");
            }
            else
            {
                SetStatus("已到灾难最后一回合，无法继续推进。");
            }

            RefreshButtons();

            if (blackPanel != null)
            {
                yield return blackPanel.FadeOut(0.3f);
                uiBootstrap?.HideBlackScreenPanel();
            }

            if (advanced)
            {
                ShowSettlementPanel(buildingSettlementText, documentRewardText);
            }
        }


        private string CollectCityBuildingSettlementText(int endingRound)
        {
            if (cityBuildingService == null)
            {
                cityBuildingService = FindFirstObjectByType<CityBuildingService>(FindObjectsInactive.Include);
            }

            var results = cityBuildingService?.CollectAvailableOutputsForSettlement();
            if (results == null || results.Count == 0)
            {
                return "本回合没有可领取的建筑产出";
            }

            foreach (var result in results)
            {
                runtimeDataService?.Data.EnsureNewspaperEntry(endingRound, $"建筑产出：{result}");
            }

            return string.Join("\n", results);
        }

        private string BuildDocumentRewardSettlementText(int endingRound)
        {
            if (runtimeDataService == null || !runtimeDataService.Data.TryGetNewspaper(endingRound, out var newspaper))
            {
                return "本回合公文无直接奖励";
            }

            var rewards = newspaper.Entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry) && entry.StartsWith("公文奖励：", StringComparison.Ordinal))
                .ToList();
            return rewards.Count > 0 ? string.Join("\n", rewards) : "本回合公文无直接奖励";
        }

        private void ShowSettlementPanel(string buildingSettlementText, string documentRewardText)
        {
            if (uiBootstrap != null)
            {
                uiBootstrap.ShowSettlementPanel(buildingSettlementText, documentRewardText);
                return;
            }

            var panel = FindFirstObjectByType<SettlementPanelView>(FindObjectsInactive.Include);
            panel?.Show(buildingSettlementText, documentRewardText);
        }

        private void ResetCityCameraToGlobalView()
        {
            if (cityCameraController == null)
            {
                var context = FindFirstObjectByType<BaseSceneUIContext>(FindObjectsInactive.Include);
                cityCameraController = context != null && context.CityCameraController != null
                    ? context.CityCameraController
                    : FindFirstObjectByType<CityCameraController>(FindObjectsInactive.Include);
            }

            cityCameraController?.JumpToDefaultView();
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

        public void EnterCity()
        {
            ResolveDependencies();
            if (isEnteringCityWithTransition)
            {
                SetStatus("正在进入城区过场，请稍候。");
                RefreshButtons();
                return;
            }

            if (IsDocumentFlowActive() || HasActiveStory() || HasQueuedGameplayStories() ||
                HasPendingBeforeDocumentStory() || HasPendingDocuments())
            {
                SetStatus("请先完成当前剧情和全部公文，再进入城区。");
                RefreshButtons();
                return;
            }

            newspaperPanel?.Hide();
            HideInventoryPanelForCity();
            var runner = GetActiveCoroutineRunner();
            if (runner == null)
            {
                SetStatus("\u7f3a\u5c11\u6fc0\u6d3b\u7684\u534f\u7a0b\u627f\u8f7d\u5bf9\u8c61\uff0c\u65e0\u6cd5\u8fdb\u5165\u57ce\u533a\u3002");
                RefreshButtons();
                return;
            }

            runner.StartCoroutine(PlayCityButtonMaskRevealThenEnterCityTransition());
        }

        private IEnumerator PlayCityButtonMaskRevealThenEnterCityTransition()
        {
            isEnteringCityWithTransition = true;
            RefreshButtons();
            yield return PlayCityButtonMaskReveal();

            var loadingPanel = uiBootstrap?.ShowLoadingPanel();
            if (loadingPanel == null)
            {
                yield return PlayEnterCityCameraOnlyTransition();
                yield break;
            }

            yield return PlayEnterCityTransition(loadingPanel);
        }

        public void HideNewspaper()
        {
            newspaperPanel?.Hide();
        }

        public void BeginCurrentRoundFromEntry()
        {
            if (hasStartedInitialRoundFlow)
            {
                return;
            }

            hasStartedInitialRoundFlow = true;
            BeginCurrentRound();
        }

        private void BeginCurrentRound()
        {
            ResolveDependencies();
            isEnteringCityWithTransition = false;
            taskService?.ProcessCurrentRoundStart();
            QueueScheduledStoriesForCurrentRound(StoryTriggerUnitIds.RoundStart, RuntimeStoryQueueTiming.StageStart);
            QueueScheduledDocumentStoriesForCurrentRound();
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

        private IEnumerator PlayScheduledExploreBeforeStoryAfterCityEntry()
        {
            yield return new WaitForSecondsRealtime(1f);
            QueueAndStartScheduledStories(StoryTriggerUnitIds.ExploreBefore, RuntimeStoryQueueTiming.ExploreBefore);
        }

        private void QueueScheduledDocumentStoriesForCurrentRound()
        {
            for (var slotIndex = 1; slotIndex <= 6; slotIndex++)
            {
                QueueScheduledStoriesForCurrentRound(
                    StoryTriggerUnitIds.GetDocumentSlot(slotIndex),
                    RuntimeStoryQueueTiming.BeforeDocument);
            }
        }

        private bool QueueAndStartScheduledStories(string triggerUnitId, RuntimeStoryQueueTiming timing)
        {
            QueueScheduledStoriesForCurrentRound(triggerUnitId, timing);
            if (!HasQueuedStories(timing))
            {
                return false;
            }

            var started = storyService != null && storyService.StartNextQueuedStory(timing);
            if (started)
            {
                uiBootstrap?.ShowStory();
            }

            return started;
        }

        private void QueueScheduledStoriesForCurrentRound(string triggerUnitId, RuntimeStoryQueueTiming timing)
        {
            if (runtimeDataService == null || storyService == null || string.IsNullOrEmpty(triggerUnitId))
            {
                return;
            }

            var currentRound = runtimeDataService.Data.CurrentRound;
            foreach (var story in storyService.Stories)
            {
                if (story.RoundNumber != currentRound || story.TriggerUnitId != triggerUnitId)
                {
                    continue;
                }

                var storyId = ResolveScheduledStoryId(story, timing);
                if (string.IsNullOrEmpty(storyId))
                {
                    continue;
                }

                runtimeDataService.Data.QueueStory(
                    storyId,
                    string.Empty,
                    string.Empty,
                    currentRound,
                    timing);
            }
        }

        private string ResolveScheduledStoryId(StoryDefinition story, RuntimeStoryQueueTiming timing)
        {
            if (story == null)
            {
                return string.Empty;
            }

            if (timing == RuntimeStoryQueueTiming.ExploreAfter &&
                FloodEndingStoryResolver.IsFloodEndingStoryId(story.StoryId))
            {
                return story.StoryId == FloodEndingStoryResolver.ResolveStoryId(runtimeDataService?.Data)
                    ? story.StoryId
                    : string.Empty;
            }

            return story.StoryId;
        }

        private void RefreshButtons()
        {
            var isDocumentFlowActive = IsDocumentFlowActive();
            var hasActiveStory = HasActiveStory();
            var hasQueuedGameplayStories = HasQueuedGameplayStories();
            var hasPendingBeforeDocumentStory = HasPendingBeforeDocumentStory();
            var hasPendingDocuments = HasPendingDocuments();
            var canContinueStory = !isEnteringCityWithTransition && !isDocumentFlowActive &&
                (hasActiveStory || (!hasPendingBeforeDocumentStory && hasQueuedGameplayStories));
            var canOpenDocuments = !isEnteringCityWithTransition &&
                !isDocumentFlowActive &&
                !hasActiveStory &&
                !hasQueuedGameplayStories &&
                !hasPendingBeforeDocumentStory &&
                hasPendingDocuments;
            var canEndRound = !isEnteringCityWithTransition &&
                !isDocumentFlowActive &&
                !hasActiveStory &&
                !hasQueuedGameplayStories &&
                !hasPendingBeforeDocumentStory &&
                !hasPendingDocuments;
            var canEnterCity = canEndRound;

            SetButtonInteractable(storyButton, canContinueStory);
            SetWorkflowButtonState(documentButton, !isDocumentFlowActive, canOpenDocuments);
            SetButtonInteractable(endRoundButton, canEndRound);
            SetWorkflowButtonState(newspaperButton, true, !isEnteringCityWithTransition && !isDocumentFlowActive && HasPreviousNewspaper());
            SetWorkflowButtonState(cityButton, true, canEnterCity);
            deskFlowSnapshot = $"\u56de\u5408={GetCurrentRoundForSnapshot()}\uff0c\u8fdb\u57ce\u8fc7\u573a={isEnteringCityWithTransition}\uff0c\u516c\u6587\u6d41={isDocumentFlowActive}\uff0c\u5267\u60c5={hasActiveStory}\uff0c\u961f\u5217\u5267\u60c5={hasQueuedGameplayStories}\uff0c\u516c\u6587\u524d={hasPendingBeforeDocumentStory}\uff0c\u5f85\u516c\u6587={hasPendingDocuments}\uff0c\u516c\u6587\u6309\u94ae={canOpenDocuments}\uff0c\u57ce\u533a\u6309\u94ae={canEnterCity}";
        }

        private IEnumerator PlayCityButtonMaskReveal()
        {
            if (!playCityButtonMaskReveal ||
                !TryResolveCityButtonRevealMasks(out var leftMask, out var rightMask))
            {
                yield break;
            }

            if (cityButtonMasksAreOpenAfterDocuments)
            {
                cityButtonMaskSnapshot = "\u516c\u6587\u9000\u51fa\u540e\u906e\u7f69\u5df2\u6253\u5f00\uff0c\u8fdb\u5165\u57ce\u533a\u65f6\u4e0d\u91cd\u590d\u62c9\u5f00";
                yield break;
            }

            KillCityButtonMaskRevealSequence();
            EndStoryPanelVisibleHold();
            var leftClosedPosition = GetCityButtonMaskClosedPosition(leftMask);
            var rightClosedPosition = GetCityButtonMaskClosedPosition(rightMask);
            leftMask.anchoredPosition = leftClosedPosition;
            rightMask.anchoredPosition = rightClosedPosition;

            var distance = Mathf.Max(360f, cityButtonMaskRevealDistance);
            var duration = Mathf.Max(0.5f, cityButtonMaskRevealDuration);
            var leftOpenPosition = leftClosedPosition + (Vector2.left * distance);
            var rightOpenPosition = rightClosedPosition + (Vector2.right * distance);
            if (duration <= 0f)
            {
                leftMask.anchoredPosition = leftOpenPosition;
                rightMask.anchoredPosition = rightOpenPosition;
                cityButtonMasksAreOpenAfterDocuments = true;
                yield break;
            }

            var isFinished = false;
            cityButtonMaskRevealSequence = DOTween.Sequence().SetUpdate(true);
            cityButtonMaskRevealSequence.Join(leftMask.DOAnchorPos(leftOpenPosition, duration).SetEase(cityButtonMaskRevealEase));
            cityButtonMaskRevealSequence.Join(rightMask.DOAnchorPos(rightOpenPosition, duration).SetEase(cityButtonMaskRevealEase));
            cityButtonMaskRevealSequence.OnComplete(() =>
            {
                cityButtonMaskRevealSequence = null;
                cityButtonMasksAreOpenAfterDocuments = true;
                isFinished = true;
            });

            while (!isFinished && cityButtonMaskRevealSequence != null && cityButtonMaskRevealSequence.IsActive())
            {
                yield return null;
            }
        }

        private bool TryResolveCityButtonRevealMasks(out RectTransform leftMask, out RectTransform rightMask)
        {
            leftMask = null;
            rightMask = null;
            if (cityButton == null)
            {
                cityButtonMaskSnapshot = "\u672a\u627e\u5230\u57ce\u533a\u6309\u94ae\uff0c\u65e0\u6cd5\u8bc6\u522b\u906e\u7f69";
                return false;
            }

            var buttonRoot = cityButton.transform as RectTransform;
            var masks = cityButton
                .GetComponentsInChildren<RectTransform>(true)
                .Where(rect => rect != null && rect != buttonRoot && IsCityButtonMaskCandidate(rect))
                .OrderBy(rect => rect.anchoredPosition.x)
                .Take(2)
                .ToArray();

            if (masks.Length < 2)
            {
                cityButtonMaskSnapshot = "\u672a\u627e\u5230\u4e24\u4e2a\u540d\u79f0\u5305\u542b\u201c\u906e\u7f69\u201d\u6216 Mask\uff0c\u6216\u6302\u6709 Mask/RectMask2D \u7684\u57ce\u533a\u6309\u94ae\u5b50\u7269\u4f53";
                return false;
            }

            leftMask = masks[0];
            rightMask = masks[1];
            cityButtonMaskSnapshot = $"\u5df2\u627e\u5230\u906e\u7f69\uff1a{leftMask.gameObject.name} / {rightMask.gameObject.name}";
            return true;
        }

        private static bool IsCityButtonMaskCandidate(RectTransform rectTransform)
        {
            return rectTransform != null &&
                (IsCityButtonMaskName(rectTransform.gameObject.name) ||
                rectTransform.GetComponent<Mask>() != null ||
                rectTransform.GetComponent<RectMask2D>() != null);
        }

        private static bool IsCityButtonMaskName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            var lowerName = objectName.ToLowerInvariant();
            return objectName.Contains("\u906e\u7f69") || lowerName.Contains("mask");
        }

        private Vector2 GetCityButtonMaskClosedPosition(RectTransform mask)
        {
            if (!cityButtonMaskClosedPositions.TryGetValue(mask, out var closedPosition))
            {
                closedPosition = mask.anchoredPosition;
                cityButtonMaskClosedPositions[mask] = closedPosition;
            }

            return closedPosition;
        }

        private void SetCityButtonMasksClosedInstantly()
        {
            cityButtonMasksAreOpenAfterDocuments = false;
            if (!TryResolveCityButtonRevealMasks(out var leftMask, out var rightMask))
            {
                return;
            }

            KillCityButtonMaskRevealSequence();
            EndStoryPanelVisibleHold();
            leftMask.anchoredPosition = GetCityButtonMaskClosedPosition(leftMask);
            rightMask.anchoredPosition = GetCityButtonMaskClosedPosition(rightMask);
            cityButtonMaskSnapshot = "\u516c\u6587\u6d41\u7a0b\u5f00\u59cb\uff0c\u57ce\u533a\u6309\u94ae\u906e\u7f69\u5df2\u590d\u4f4d\u4e3a\u95ed\u5408";
        }

        private void OpenCityButtonMasksAfterDocumentExit()
        {
            if (!playCityButtonMaskReveal ||
                !TryResolveCityButtonRevealMasks(out var leftMask, out var rightMask))
            {
                return;
            }

            KillCityButtonMaskRevealSequence();
            EndStoryPanelVisibleHold();
            var distance = Mathf.Max(360f, cityButtonMaskRevealDistance);
            var duration = Mathf.Max(0.5f, cityButtonMaskRevealDuration);
            var leftClosedPosition = GetCityButtonMaskClosedPosition(leftMask);
            var rightClosedPosition = GetCityButtonMaskClosedPosition(rightMask);
            var leftOpenPosition = leftClosedPosition + (Vector2.left * distance);
            var rightOpenPosition = rightClosedPosition + (Vector2.right * distance);

            leftMask.anchoredPosition = leftClosedPosition;
            rightMask.anchoredPosition = rightClosedPosition;
            hasDocumentFlowBeenStarted = false;
            cityButtonMaskSnapshot = "\u516c\u6587\u9000\u51fa\u540e\uff0c\u57ce\u533a\u6309\u94ae\u906e\u7f69\u6b63\u5728\u5de6\u53f3\u62c9\u5f00";

            cityButtonMaskRevealSequence = DOTween.Sequence().SetUpdate(true);
            cityButtonMaskRevealSequence.Join(leftMask.DOAnchorPos(leftOpenPosition, duration).SetEase(cityButtonMaskRevealEase));
            cityButtonMaskRevealSequence.Join(rightMask.DOAnchorPos(rightOpenPosition, duration).SetEase(cityButtonMaskRevealEase));
            cityButtonMaskRevealSequence.OnComplete(() =>
            {
                cityButtonMaskRevealSequence = null;
                cityButtonMasksAreOpenAfterDocuments = true;
                cityButtonMaskSnapshot = "\u516c\u6587\u9000\u51fa\u540e\uff0c\u57ce\u533a\u6309\u94ae\u4e24\u4e2a\u906e\u7f69\u5df2\u5b8c\u6210\u62c9\u5f00";
            });
        }

        private void KillCityButtonMaskRevealSequence()
        {
            if (cityButtonMaskRevealSequence == null)
            {
                return;
            }

            cityButtonMaskRevealSequence.Kill();
            cityButtonMaskRevealSequence = null;
        }

        private IEnumerator PlayEnterCityTransition(LoadingPanelTransitionView loadingPanel)
        {
            isEnteringCityWithTransition = true;
            SetStatus("正在进入城区过场。");
            RefreshButtons();

            var isFinished = false;
            var isCameraFinished = cityCameraController == null;
            loadingPanel.PlayEnterCityTransitionSynchronized(
                () =>
                {
                    EnterCityImmediately();
                    if (cityCameraController != null)
                    {
                        cityCameraController.PlayEntryCinematic(GetSynchronizedEntryTransitionDuration(loadingPanel), () => isCameraFinished = true);
                    }
                },
                GetSynchronizedEntryTransitionDuration(loadingPanel),
                () =>
                {
                    uiBootstrap?.HideLoadingPanel();
                    isFinished = true;
                });

            while (!isFinished || !isCameraFinished)
            {
                yield return null;
            }

            yield return PlayScheduledExploreBeforeStoryAfterCityEntry();
            isEnteringCityWithTransition = false;
            RefreshButtons();
        }

        private float GetSynchronizedEntryTransitionDuration(LoadingPanelTransitionView loadingPanel)
        {
            if (synchronizedEntryTransitionDuration > 0f)
            {
                return synchronizedEntryTransitionDuration;
            }

            return loadingPanel != null ? loadingPanel.CloseDuration + loadingPanel.OpenDuration : 0f;
        }

        private IEnumerator PlayEnterCityCameraOnlyTransition()
        {
            isEnteringCityWithTransition = true;
            SetStatus("正在进入城区过场。");
            RefreshButtons();

            EnterCityImmediately();
            var isFinished = false;
            if (cityCameraController != null)
            {
                cityCameraController.PlayEntryCinematic(GetSynchronizedEntryTransitionDuration(null), () => isFinished = true);
            }
            else
            {
                isFinished = true;
            }

            while (!isFinished)
            {
                yield return null;
            }

            yield return PlayScheduledExploreBeforeStoryAfterCityEntry();
            isEnteringCityWithTransition = false;
            RefreshButtons();
        }

        private void EnterCityImmediately()
        {
            HideInventoryPanelForCity();
            uiBootstrap?.ShowCity();
            gameEntry?.ShowCity();
            EnsureCitySideEventBinding();
            SetStatus("已进入城区。");
            RefreshButtons();
        }


        private void EnsureCitySideEventBinding()
        {
            var host = gameEntry != null ? gameEntry.gameObject : gameObject;
            var pointRegistry = FindFirstObjectByType<CityPointRegistry>(FindObjectsInactive.Include) ?? host.GetComponent<CityPointRegistry>();
            if (pointRegistry == null)
            {
                pointRegistry = host.AddComponent<CityPointRegistry>();
            }

            var sideEventService = FindFirstObjectByType<CitySideEventService>(FindObjectsInactive.Include) ?? host.GetComponent<CitySideEventService>();
            if (sideEventService == null)
            {
                sideEventService = host.AddComponent<CitySideEventService>();
            }

            var sideEventRegistry = FindFirstObjectByType<CitySideEventRegistry>(FindObjectsInactive.Include) ?? host.GetComponent<CitySideEventRegistry>();
            if (sideEventRegistry == null)
            {
                sideEventRegistry = host.AddComponent<CitySideEventRegistry>();
            }

            pointRegistry.RefreshAndBind();
            sideEventService.Refresh();
            sideEventRegistry.RefreshAndBind();
        }
        private void HideInventoryPanelForCity()
        {
            if (inventoryPanel == null)
            {
                inventoryPanel = FindFirstObjectByType<InventoryPanelView>(FindObjectsInactive.Include);
            }

            inventoryPanel?.HideForDocumentSubmission(true);
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

        private int GetCurrentRoundForSnapshot()
        {
            return runtimeDataService != null ? runtimeDataService.Data.CurrentRound : 0;
        }

        private bool HasQueuedGameplayStories()
        {
            return HasQueuedStories(RuntimeStoryQueueTiming.StageStart);
        }

        private bool HasPendingBeforeDocumentStory()
        {
            return waitingForBeforeDocumentActorClick ||
                HasQueuedStories(RuntimeStoryQueueTiming.BeforeDocument);
        }

        private bool HasPendingDocuments()
        {
            documentService?.EnsureCurrentRoundDocumentQueue();

            if (runtimeDataService == null)
            {
                return false;
            }

            var currentRound = runtimeDataService.Data.CurrentRound;
            foreach (var entry in runtimeDataService.Data.DocumentQueue)
            {
                if (entry.QueuedRound <= currentRound)
                {
                    return true;
                }
            }

            return false;
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
            TryPlayStoryBlackFadeTransition();

            if (TryStartNextRequiredStory())
            {
                RefreshButtons();
                return;
            }

            TryAdvanceAfterEndStories();
            TryShowBeforeDocumentActor();
            RefreshButtons();
        }

        private void TryPlayStoryBlackFadeTransition()
        {
            if (!storyBlackFadeEnabled || uiBootstrap == null)
            {
                observedStoryActiveForBlackFade = HasActiveStory();
                return;
            }

            var hasActiveStory = HasActiveStory();
            if (hasActiveStory == observedStoryActiveForBlackFade)
            {
                return;
            }

            observedStoryActiveForBlackFade = hasActiveStory;
            if (!storyBlackFadeRunning)
            {
                StartCoroutine(PlayStoryBlackFadeTransition(hasActiveStory));
            }
        }

        private IEnumerator PlayStoryBlackFadeTransition(bool enteringStory)
        {
            storyBlackFadeRunning = true;
            var blackPanel = uiBootstrap != null ? uiBootstrap.ShowBlackScreenPanel() : null;
            if (blackPanel != null)
            {
                yield return blackPanel.FadeIn(storyBlackFadeDuration);
                if (enteringStory)
                {
                    uiBootstrap?.ShowStory();
                }
                else
                {
                    EndStoryPanelVisibleHold();
                    uiBootstrap?.HideStory();
                }
                yield return blackPanel.FadeOut(storyBlackFadeDuration);
                uiBootstrap?.HideBlackScreenPanel();
            }
            else
            {
                if (enteringStory)
                {
                    uiBootstrap?.ShowStory();
                }
                else
                {
                    EndStoryPanelVisibleHold();
                    uiBootstrap?.HideStory();
                }
            }
            deskFlowSnapshot = enteringStory
                ? "剧情进场：黑场淡入后切到剧情面板，再淡出返回"
                : "剧情退场：黑场淡入后切回桌面，再淡出返回";
            storyBlackFadeRunning = false;
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

            if (HasQueuedStories(RuntimeStoryQueueTiming.ExploreBefore))
            {
                storyService?.StartNextQueuedStory(RuntimeStoryQueueTiming.ExploreBefore);
                return true;
            }

            if (HasQueuedStories(RuntimeStoryQueueTiming.ExploreAfter))
            {
                storyService?.StartNextQueuedStory(RuntimeStoryQueueTiming.ExploreAfter);
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
                    sharedActorSlot.ShowActor(
                        character.CharacterName,
                        "公文前剧情",
                        CharacterPlaceholderPortraitProvider.LoadPortrait(character.PortraitId));
                }
                else
                {
                    sharedActorSlot.ShowActor(
                        string.IsNullOrEmpty(characterId) ? "公文前人物" : characterId,
                        "公文前剧情",
                        CharacterPlaceholderPortraitProvider.LoadPortrait(characterId));
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
            // 公文前角色不是公文人物，点击后直接消失，不播放离场滑出动画。
            // 只有公文中的人物（由 DocumentPopupPanelView 控制）才播放离场动画。
            sharedActorSlot?.Hide();
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

            if (gameEntry == null)
            {
                gameEntry = FindFirstObjectByType<GameEntry>(FindObjectsInactive.Include);
            }

            if (uiBootstrap == null)
            {
                uiBootstrap = FindFirstObjectByType<BaseSceneUIBootstrap>(FindObjectsInactive.Include);
            }

            if (documentPopupPanel == null)
            {
                documentPopupPanel = FindPreferredDocumentPopup();
            }

            if (newspaperPanel == null)
            {
                newspaperPanel = FindFirstObjectByType<NewspaperPanelView>(FindObjectsInactive.Include);
            }

            if (sharedActorSlot == null)
            {
                sharedActorSlot = FindFirstObjectByType<SharedActorSlotView>(FindObjectsInactive.Include);
            }

            if (inventoryPanel == null)
            {
                inventoryPanel = FindFirstObjectByType<InventoryPanelView>(FindObjectsInactive.Include);
            }

            if (cityCameraController == null)
            {
                cityCameraController = FindFirstObjectByType<CityCameraController>(FindObjectsInactive.Include);
            }

            BindWorkflowButtons();
        }

        private void BindWorkflowButtons()
        {
            if (!autoBindWorkflowButtons)
            {
                RefreshButtonBindingSnapshot();
                return;
            }

            if (documentButton == null)
            {
                documentButton = FindWorkflowButton("公文按钮", "公文", "DocumentButton", "OpenDocumentButton");
            }

            if (documentButton == null)
            {
                documentButton = FindWorkflowButton("\u516c\u6587\u6309\u94ae", "\u516c\u6587", "DocumentButton", "OpenDocumentButton");
            }

            if (newspaperButton == null)
            {
                newspaperButton = FindWorkflowButton("报纸按钮", "报纸", "NewspaperButton", "OpenNewspaperButton");
            }

            if (newspaperButton == null)
            {
                newspaperButton = FindWorkflowButton("\u62a5\u7eb8\u6309\u94ae", "\u62a5\u7eb8", "NewspaperButton", "OpenNewspaperButton");
            }

            if (cityButton == null)
            {
                cityButton = FindWorkflowButton("城区按钮", "城区", "CityButton", "OpenCityButton");
            }

            if (cityButton == null)
            {
                cityButton = FindWorkflowButton("\u57ce\u533a\u6309\u94ae", "\u57ce\u533a", "CityButton", "OpenCityButton");
            }

            BindButtonClick(documentButton, BeginDocumentFlow);
            BindButtonClick(newspaperButton, ShowPreviousRoundNewspaper);
            BindButtonClick(cityButton, EnterCity);
            EnsureWorkflowButtonHoverScaleEffect(documentButton);
            EnsureWorkflowButtonHoverScaleEffect(newspaperButton);
            RefreshButtonBindingSnapshot();
        }

        private static void EnsureWorkflowButtonHoverScaleEffect(Button button)
        {
            if (button == null || button.GetComponent<ButtonAnim>() != null)
            {
                return;
            }

            button.gameObject.AddComponent<ButtonAnim>();
        }

        private Button FindWorkflowButton(string exactChineseName, string chineseKeyword, params string[] fallbackNames)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button != null && button.gameObject.name == exactChineseName)
                {
                    return button;
                }
            }

            foreach (var button in buttons)
            {
                if (button == null)
                {
                    continue;
                }

                var objectName = button.gameObject.name;
                if (objectName.Contains(chineseKeyword) && objectName.Contains("\u6309\u94ae"))
                {
                    return button;
                }
                if (objectName.Contains(chineseKeyword) && objectName.Contains("按钮"))
                {
                    return button;
                }

                foreach (var fallbackName in fallbackNames)
                {
                    if (objectName == fallbackName)
                    {
                        return button;
                    }
                }
            }

            return null;
        }

        private static void BindButtonClick(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void RefreshButtonBindingSnapshot()
        {
            buttonBindingSnapshot =
                $"\u516c\u6587\u6309\u94ae\uff1a{DescribeWorkflowButtonBinding(documentButton)}\uff1b" +
                $"\u62a5\u7eb8\u6309\u94ae\uff1a{DescribeWorkflowButtonBinding(newspaperButton)}\uff1b" +
                $"\u57ce\u533a\u6309\u94ae\uff1a{DescribeWorkflowButtonBinding(cityButton)}";
        }

        private static string DescribeWorkflowButtonBinding(Button button)
        {
            return button != null ? $"\u5df2\u5173\u8054 {button.gameObject.name}" : "\u672a\u5173\u8054";
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

        private static void SetWorkflowButtonState(Button button, bool visible, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            if (button.gameObject.activeSelf != visible)
            {
                button.gameObject.SetActive(visible);
            }

            button.interactable = interactable;
        }

        private void EnsureDocumentPopupVisible()
        {
            var popup = uiBootstrap != null ? uiBootstrap.ShowDocumentPopup() : null;
            popup ??= FindPreferredDocumentPopup();
            if (popup == documentPopupPanel)
            {
                return;
            }

            UnregisterDocumentPopupStateListener(documentPopupPanel);
            documentPopupPanel = popup;
            RegisterDocumentPopupStateListener(documentPopupPanel);
        }

        private DocumentPopupPanelView FindPreferredDocumentPopup()
        {
            var popups = FindObjectsByType<DocumentPopupPanelView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return popups.FirstOrDefault(IsRuntimePopupInstance) ??
                popups.FirstOrDefault(candidate => candidate != null && candidate.gameObject.activeInHierarchy) ??
                popups.FirstOrDefault();
        }

        private static bool IsRuntimePopupInstance(DocumentPopupPanelView popup)
        {
            if (popup == null)
            {
                return false;
            }

            var current = popup.transform;
            while (current != null)
            {
                if (current.name == "PopupRoot")
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private void RegisterDocumentPopupStateListener(DocumentPopupPanelView popup)
        {
            if (popup != null)
            {
                popup.DocumentFlowStateChanged -= HandleDocumentPopupStateChanged;
                popup.DocumentFlowStateChanged += HandleDocumentPopupStateChanged;
                wasDocumentFlowActive = popup.IsDocumentFlowActive;
            }
        }

        private void UnregisterDocumentPopupStateListener(DocumentPopupPanelView popup)
        {
            if (popup != null)
            {
                popup.DocumentFlowStateChanged -= HandleDocumentPopupStateChanged;
            }
        }

        private void HandleDocumentPopupStateChanged()
        {
            var isDocumentFlowActive = IsDocumentFlowActive();
            if (!wasDocumentFlowActive && isDocumentFlowActive)
            {
                hasDocumentFlowBeenStarted = true;
                SetCityButtonMasksClosedInstantly();
            }
            else if (wasDocumentFlowActive && !isDocumentFlowActive && !HasPendingDocuments())
            {
                OpenCityButtonMasksAfterDocumentExit();
            }

            // 安全检查：只在正式公文流程（BeginDocumentFlow）曾经启动过的情况下，
            // 当公文面板变为非激活但遮罩尚未打开时，强制打开遮罩。
            if (hasDocumentFlowBeenStarted && !isDocumentFlowActive && !cityButtonMasksAreOpenAfterDocuments && !HasPendingDocuments())
            {
                OpenCityButtonMasksAfterDocumentExit();
            }

            wasDocumentFlowActive = isDocumentFlowActive;
            RefreshButtons();
        }
    }
}
