using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class StoryPanelView : MonoBehaviour
    {
        private const string PortraitOutlineMaterialResourcePath = "Materials/UI/PortraitAlphaOutlineRuntime";
        private const string SkeletonCharacterNamePrefix = "\u9ab7\u9ac5";
        private const string OpeningSkeletonStoryId = "S0001";
        private const string OpeningWorkStoryId = "S0002";
        private const string OpeningWorkNarrationStartLineId = "S0002_001";
        private const string OpeningWorkNarrationSecondLineId = "S0002_002";
        private const string OpeningSkeletonDefaultExpressionId = "\u9ab7\u9ac5\u601d\u8003";
        private const string SkeletonStartAtPresentationPointCue = "演出点位起始";
        private const string SkeletonActivateRiseCue = "上升300回初始位";
        private const string SkeletonFloatCue = "持续漂浮";
        private static readonly string[] SkeletonExpressionRoots =
        {
            "Art/Art/Character/\u9ab7\u9ac5"
        };
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int OutlinePixelWidthId = Shader.PropertyToID("_OutlinePixelWidth");
        private static readonly int GlowPixelWidthId = Shader.PropertyToID("_GlowPixelWidth");
        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
        private static readonly int GlowFalloffPowerId = Shader.PropertyToID("_GlowFalloffPower");

        [Header("依赖服务：读取并推进当前剧情播放状态")]
        [SerializeField] private StoryService storyService;

        [Header("根层显隐：无剧情时隐藏并放开桌面点击")]
        [SerializeField] private CanvasGroup rootCanvasGroup;
        [Tooltip("剧情面板根背景；显示剧情时保持不透明并拦截桌面点击。")]
        [SerializeField] private Image rootBackgroundImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button storyAreaButton;
        [SerializeField] private float typewriterCharactersPerSecond = 42f;

        [Header("S0002\u9ed1\u573a\u65c1\u767d\uff1a\u5267\u60c5\u9762\u677f\u505c\u987f\u540e\u64ad\u653e")]
        [Tooltip("S0002_001\u89e6\u53d1\u65f6\uff0c\u5148\u4fdd\u6301\u5267\u60c5\u9762\u677f\u6253\u5f00\u7684\u7b49\u5f85\u65f6\u95f4\uff1b\u7b49\u5f85\u7ed3\u675f\u540e\u518d\u6253\u5f00\u9ed1\u573a\u9762\u677f\u5e76\u64ad\u653e\u65c1\u767d\u3002")]
        [SerializeField, Min(0f)] private float openingWorkNarrationDelayBeforeBlackScreen = 1f;

        [Header("对话面板：显示角色、对白、继续按钮和选项")]
        [SerializeField] private GameObject dialoguePanel;
        [Tooltip("\u5bf9\u8bdd\u9762\u677f\u81ea\u8eab\u7684 Image\uff1b\u5267\u60c5\u884c\u7684\u80cc\u666fID \u4f1a\u4f18\u5148\u66ff\u6362\u8fd9\u5f20\u56fe\u7247\uff0c\u672a\u62d6\u5f15\u7528\u65f6\u81ea\u52a8\u8bfb\u53d6\u5bf9\u8bdd\u9762\u677f\u4e0a\u7684 Image\u3002")]
        [SerializeField] private Image dialogueBackgroundImage;
        [SerializeField] private Image leftPortrait;
        [SerializeField] private Image rightPortrait;
        [SerializeField] private Image speakerExpressionImage;
        [Header("\u5bf9\u8bdd\u4eba\u7269\u660e\u5ea6\uff1a\u8bf4\u8bdd\u8005\u53d8\u4eae\uff0c\u975e\u8bf4\u8bdd\u8005\u53d8\u6697")]
        [Tooltip("\u5f53\u524d\u8bf4\u8bdd\u4eba\u7269\u7684\u7acb\u7ed8\u989c\u8272\uff1b\u4fdd\u6301\u539f\u5c3a\u5bf8\uff0c\u4ec5\u7528\u660e\u5ea6\u8868\u793a\u8bf4\u8bdd\u72b6\u6001\u3002")]
        [SerializeField] private Color activeSpeakerPortraitColor = Color.white;
        [Tooltip("\u975e\u5f53\u524d\u8bf4\u8bdd\u4eba\u7269\u7684\u7acb\u7ed8\u989c\u8272\uff1b\u7528\u8f83\u6697\u660e\u5ea6\u8868\u793a\u6b64\u65f6\u6ca1\u6709\u8bf4\u8bdd\u3002")]
        [SerializeField] private Color inactiveSpeakerPortraitColor = new Color(0.52f, 0.52f, 0.52f, 1f);
        [Header("剧情人物白色描边晕圈：直接作用在角色 Image 上")]
        [Tooltip("当前说话人物外侧细描边颜色；透明度越高，人物边缘越清晰。")]
        [SerializeField] private Color activePortraitOutlineColor = new Color(1f, 1f, 1f, 0f);
        [Tooltip("非说话人物外侧细描边颜色；保持较淡，避免抢过当前说话人物。")]
        [SerializeField] private Color inactivePortraitOutlineColor = new Color(1f, 1f, 1f, 0f);
        [Tooltip("当前说话人物外侧柔和晕圈颜色；用于制造参考图中人物背后的淡白光。")]
        [SerializeField] private Color activePortraitGlowColor = new Color(1f, 1f, 1f, 0.46f);
        [Tooltip("非说话人物外侧柔和晕圈颜色；透明度较低，只保留轻微区分。")]
        [SerializeField] private Color inactivePortraitGlowColor = new Color(1f, 1f, 1f, 0.18f);
        [Tooltip("白色细描边采样宽度，单位约等于 Sprite 像素；建议 1 到 3。")]
        [SerializeField] private int portraitOutlinePixelWidth;
        [Tooltip("白色柔和晕圈采样宽度，单位约等于 Sprite 像素；建议大于描边宽度。")]
        [SerializeField] private int portraitGlowPixelWidth = 30;
        [Tooltip("晕圈强度；数值越高，外侧淡白光越明显。")]
        [Range(0f, 1f)]
        [SerializeField] private float portraitGlowIntensity = 0.72f;
        [Tooltip("光晕边缘衰减曲线；数值越高，外缘越柔和地淡出，越不容易形成硬白边。")]
        [Range(1f, 4f)]
        [SerializeField] private float portraitGlowFalloffPower = 2.4f;
        [Tooltip("人物描边晕圈专用 Shader；留空时自动查找 TwelveMoons/UI/PortraitAlphaOutline，直接作用在角色 Image 上。")]
        [SerializeField] private Shader portraitOutlineShader;
        [Tooltip("人物描边晕圈材质模板；留空时从 Resources/Materials/UI/PortraitAlphaOutlineRuntime 加载，避免打包后 Shader 被裁剪。")]
        [SerializeField] private Material portraitOutlineTemplateMaterial;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Button dialogueContinueButton;
        [SerializeField] private TMP_Text dialogueContinueButtonText;
        [SerializeField] private Button choiceButtonA;
        [SerializeField] private Button choiceButtonB;
        [SerializeField] private TMP_Text choiceButtonAText;
        [SerializeField] private TMP_Text choiceButtonBText;

        [Header("提交面板：剧情需要道具时显示提交与退出按钮")]
        [SerializeField] private GameObject submissionPanel;
        [SerializeField] private TMP_Text submissionTitleText;
        [SerializeField] private TMP_Text submissionRequirementText;
        [SerializeField] private Button submitButton;
        [SerializeField] private TMP_Text submitButtonText;
        [SerializeField] private Button exitSubmitButton;
        [SerializeField] private TMP_Text exitSubmitButtonText;

        [Header("图片剧情面板：显示单页图片或漫画格")]
        [SerializeField] private GameObject imageStoryPanel;
        [SerializeField] private Image storyImage;
        [SerializeField] private Image[] comicPanelImages;
        [SerializeField] private TMP_Text imageCaptionText;
        [SerializeField] private Button imageContinueButton;
        [SerializeField] private TMP_Text imageContinueButtonText;

        [Header("文本剧情面板：显示纯文本段落和继续按钮")]
        [SerializeField] private GameObject textStoryPanel;
        [SerializeField] private TMP_Text textContent;
        [SerializeField] private Button textContinueButton;
        [SerializeField] private TMP_Text textContinueButtonText;

                                [Header("开场骷髅演出：启用与演出点位")]
        [Tooltip("开启后，S0001 会按演出栏驱动骷髅的开场动效。")]
        [SerializeField] private bool enableOpeningSkeletonPresentation = true;
        [Header("骷髅演出点位：S0001 开场初始位置")]
        [Tooltip("优先使用 Prefab 内名为“骷髅演出点位”的 RectTransform；未手动拖引用时会自动按名字查找。")]
        [SerializeField] private RectTransform skeletonPresentationPoint;

        [Header("骷髅激活动效：S0001_005 上升与回位")]
        [Tooltip("S0001_005 播放时骷髅向上缓慢上升的距离。")]
        [SerializeField, Min(0f)] private float openingSkeletonActivateRiseDistance = 300f;
        [Tooltip("S0001_005 从黑色剪影缓慢上升到顶点的时长；播放期间对话框不可点击。")]
        [SerializeField, Min(0f)] private float openingSkeletonActivateDuration = 3f;
        [Tooltip("S0001_005 上升结束后平滑回到初始点位的时长。")]
        [SerializeField, Min(0f)] private float openingSkeletonReturnDuration = 0.9f;
        [Tooltip("S0001_005 ????????????")]
        [SerializeField, Min(0f)] private float openingSkeletonPeakHoldDuration = 1f;
        [Tooltip("S0001_005 上升期间骷髅左右抖动的幅度。")]
        [SerializeField, Min(0f)] private float openingSkeletonActivateShakeAmplitude = 6f;
        [Tooltip("S0001_005 上升期间骷髅每秒抖动次数。")]
        [SerializeField, Min(0f)] private float openingSkeletonActivateShakeFrequency = 22f;

        [Header("开场骷髅演出：只读调试快照")]
        [Tooltip("运行时只读；显示 S0001 骷髅当前所处的演出阶段，便于在 Inspector 中观察。")]
        [SerializeField] private string openingSkeletonSnapshot = "开场骷髅：等待 S0001";
        private string currentStoryId;
        private string leftCharacterId;
        private string rightCharacterId;
        private string activeTypewriterKey;
        private string targetTypewriterText;
        private int visibleTypewriterCharacters;
        private float typewriterTimer;
        private bool isTypewriting;
        private bool finalContinueVisible;
        private string finalContinueKey;
        private Material leftPortraitOutlineMaterial;
        private Material rightPortraitOutlineMaterial;
        private Material speakerExpressionOutlineMaterial;
        private Material originalLeftPortraitMaterial;
        private Material originalRightPortraitMaterial;
        private Material originalSpeakerExpressionMaterial;
        private Sprite originalSpeakerExpressionSprite;
        private RectTransform speakerExpressionRectTransform;
        private Vector2 speakerExpressionOriginalAnchoredPosition;
        private bool hasSpeakerExpressionOriginalPosition;
        private int currentDialogueLineNumber;
        private bool openingSkeletonMotionActive;
        private string activePresentationCueKey;
        private bool presentationCueInputLocked;
        private Tween presentationCueTween;
        private Coroutine openingWorkNarrationRoutine;
        private bool openingWorkNarrationActive;

        public Color ActiveSpeakerPortraitColor => activeSpeakerPortraitColor;

        public Color InactiveSpeakerPortraitColor => inactiveSpeakerPortraitColor;

        private void Awake()
        {
            if (storyService == null)
            {
                storyService = FindFirstObjectByType<StoryService>();
            }

            if (rootCanvasGroup == null)
            {
                rootCanvasGroup = GetComponent<CanvasGroup>();
            }

            if (rootCanvasGroup == null)
            {
                rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (rootBackgroundImage == null)
            {
                rootBackgroundImage = GetComponent<Image>();
                if (rootBackgroundImage == null)
                {
                    rootBackgroundImage = gameObject.AddComponent<Image>();
                    rootBackgroundImage.color = Color.black;
                }
            }

            ResolveDialogueBackgroundImage();
            CacheOriginalPortraitMaterials();
            EnsureStoryAreaButtonBinding();
        }

        private void Update()
        {
            UpdateOpeningSkeletonMotion();

            if (!isTypewriting || string.IsNullOrEmpty(targetTypewriterText))
            {
                return;
            }

            var step = Mathf.Max(1f, typewriterCharactersPerSecond);
            typewriterTimer += Time.unscaledDeltaTime * step;
            var nextVisible = Mathf.Min(targetTypewriterText.Length, Mathf.FloorToInt(typewriterTimer));
            if (nextVisible == visibleTypewriterCharacters)
            {
                return;
            }

            visibleTypewriterCharacters = nextVisible;
            ApplyTypewriterText();
            if (visibleTypewriterCharacters >= targetTypewriterText.Length)
            {
                isTypewriting = false;
            }
        }

        private void OnEnable()
        {
            if (storyService != null)
            {
                storyService.StoryChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (storyService != null)
            {
                storyService.StoryChanged -= Refresh;
            }
        }

        private void OnDestroy()
        {
            KillPresentationCueTween(false);
            DestroyRuntimeMaterial(ref leftPortraitOutlineMaterial);
            DestroyRuntimeMaterial(ref rightPortraitOutlineMaterial);
            DestroyRuntimeMaterial(ref speakerExpressionOutlineMaterial);
        }

        private void ResolveDialogueBackgroundImage()
        {
            if (dialogueBackgroundImage == null && dialoguePanel != null)
            {
                dialogueBackgroundImage = dialoguePanel.GetComponent<Image>();
            }
        }

        private void CacheOriginalPortraitMaterials()
        {
            originalLeftPortraitMaterial = leftPortrait != null ? leftPortrait.material : null;
            originalRightPortraitMaterial = rightPortrait != null ? rightPortrait.material : null;
            originalSpeakerExpressionMaterial = speakerExpressionImage != null ? speakerExpressionImage.material : null;
            originalSpeakerExpressionSprite = speakerExpressionImage != null ? speakerExpressionImage.sprite : null;
        }

        private static void DestroyRuntimeMaterial(ref Material runtimeMaterial)
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeMaterial);
            }
            else
            {
                DestroyImmediate(runtimeMaterial);
            }

            runtimeMaterial = null;
        }

        public void OnContinueClicked()
        {
            if (presentationCueInputLocked || openingWorkNarrationActive)
            {
                return;
            }

            if (!finalContinueVisible)
            {
                return;
            }

            var playback = storyService != null ? storyService.CurrentPlayback : null;
            if (playback != null && string.Equals(playback.Story.StoryId, OpeningSkeletonStoryId, System.StringComparison.Ordinal) && IsCurrentStoryStepLast(playback))
            {
                DeskLoopController.BeginStoryPanelVisibleHold();
            }

            finalContinueVisible = false;
            finalContinueKey = string.Empty;
            storyService?.Continue();
        }

        public void OnStoryAreaClicked()
        {
            if (presentationCueInputLocked || openingWorkNarrationActive)
            {
                return;
            }

            if (RevealTypewriterIfNeeded())
            {
                return;
            }

            var playback = storyService != null ? storyService.CurrentPlayback : null;
            if (playback != null && IsCurrentStoryStepLast(playback))
            {
                ShowFinalContinueButton(playback);
                return;
            }

            storyService?.Continue();
        }

        private void EnsureStoryAreaButtonBinding()
        {
            if (storyAreaButton == null || HasPersistentStoryAreaClickBinding())
            {
                return;
            }

            storyAreaButton.onClick.RemoveListener(OnStoryAreaClicked);
            storyAreaButton.onClick.AddListener(OnStoryAreaClicked);
        }

        private bool HasPersistentStoryAreaClickBinding()
        {
            if (storyAreaButton == null)
            {
                return false;
            }

            var eventCount = storyAreaButton.onClick.GetPersistentEventCount();
            for (var index = 0; index < eventCount; index++)
            {
                var target = storyAreaButton.onClick.GetPersistentTarget(index);
                var methodName = storyAreaButton.onClick.GetPersistentMethodName(index);
                if (target == this && methodName == nameof(OnStoryAreaClicked))
                {
                    return true;
                }
            }

            return false;
        }

        public void OnOptionAClicked()
        {
            if (presentationCueInputLocked || openingWorkNarrationActive)
            {
                return;
            }

            storyService?.ChooseOption(0);
        }

        public void OnOptionBClicked()
        {
            if (presentationCueInputLocked || openingWorkNarrationActive)
            {
                return;
            }

            storyService?.ChooseOption(1);
        }

        public void OnSubmitClicked()
        {
            if (presentationCueInputLocked || openingWorkNarrationActive)
            {
                return;
            }

            storyService?.SubmitCurrentItems();
        }

        public void OnExitSubmitClicked()
        {
            if (presentationCueInputLocked)
            {
                return;
            }

            storyService?.ExitItemSubmission();
        }

        public void Refresh()
        {
            if (storyService == null || storyService.CurrentPlayback == null)
            {
                if (DeskLoopController.HoldStoryPanelVisibleDuringTransition)
                {
                    KeepVisibleForTransition();
                    return;
                }

                currentStoryId = string.Empty;
                ResetDialogueCharacters();
                ClearStoryVisualState();
                ResetTypewriter();
                ResetFinalContinue();
                KillPresentationCueTween(false);
                SetText(titleText, "Story");
                SetText(feedbackText, "");
                ApplyStoryBackground(null);
                ShowOnlyPanel(null);
                SetRootVisible(false);
                return;
            }

            SetRootVisible(true);
            var playback = storyService.CurrentPlayback;
            var story = playback.Story;
            ApplyStoryBackground(story);
            if (story.StoryId != currentStoryId)
            {
                currentStoryId = story.StoryId;
                ResetDialogueCharacters();
                ResetTypewriter();
                ResetFinalContinue();
                KillPresentationCueTween(false);
            }

            RefreshFinalContinueKey(playback);

            SetText(titleText, string.IsNullOrEmpty(story.StoryName) ? story.StoryId : story.StoryName);
            SetText(feedbackText, playback.Feedback);

            if (playback.IsCompleted)
            {
                RefreshCompletedStory(playback.Feedback);
                return;
            }

            switch (story.StoryType)
            {
                case StoryType.Dialogue:
                    RefreshDialogue(playback);
                    break;
                case StoryType.Image:
                    RefreshImageStory(playback);
                    break;
                default:
                    RefreshTextStory(playback);
                    break;
            }
        }

        private void KeepVisibleForTransition()
        {
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 1f;
                rootCanvasGroup.blocksRaycasts = false;
                rootCanvasGroup.interactable = false;
            }

            if (rootBackgroundImage != null)
            {
                rootBackgroundImage.raycastTarget = false;
            }

            SetButtonInteractable(dialogueContinueButton, false);
            SetButtonInteractable(choiceButtonA, false);
            SetButtonInteractable(choiceButtonB, false);
            SetButtonInteractable(submitButton, false);
            SetButtonInteractable(exitSubmitButton, false);
            SetButtonInteractable(textContinueButton, false);
            SetButtonInteractable(imageContinueButton, false);
            SetButtonInteractable(storyAreaButton, false);
        }

        private void ApplyStoryBackground(StoryDefinition story)
        {
            ApplyBackgroundImage(story == null ? string.Empty : story.BackgroundImageId);
        }

        private void ApplyDialogueBackground(StoryDefinition story, DialogueLineDefinition line)
        {
            var backgroundId = line != null && !string.IsNullOrEmpty(line.BackgroundImageId)
                ? line.BackgroundImageId
                : story == null ? string.Empty : story.BackgroundImageId;
            ApplyBackgroundImage(backgroundId);
        }

        private void ApplyBackgroundImage(string backgroundId)
        {
            ResolveDialogueBackgroundImage();
            var targetBackgroundImage = dialogueBackgroundImage != null ? dialogueBackgroundImage : rootBackgroundImage;
            if (targetBackgroundImage == null)
            {
                return;
            }

            var sprite = string.IsNullOrEmpty(backgroundId) ? null : StoryImageResourceProvider.LoadSprite(backgroundId);
            targetBackgroundImage.sprite = sprite;
            targetBackgroundImage.enabled = true;
            targetBackgroundImage.color = sprite == null ? Color.black : Color.white;
        }

        private bool TryResolveSkeletonPresentationPoint(out Vector2 anchoredPosition)
        {
            anchoredPosition = Vector2.zero;
            if (skeletonPresentationPoint == null && speakerExpressionImage != null)
            {
                var points = speakerExpressionImage.transform.root.GetComponentsInChildren<RectTransform>(true);
                foreach (var point in points)
                {
                    if (point != null && string.Equals(point.name, "骷髅演出点位", System.StringComparison.Ordinal))
                    {
                        skeletonPresentationPoint = point;
                        break;
                    }
                }
            }

            if (skeletonPresentationPoint == null)
            {
                return false;
            }

            anchoredPosition = skeletonPresentationPoint.anchoredPosition;
            return true;
        }

        private void RefreshCompletedStory(string feedback)
        {
            ShowOnlyPanel(textStoryPanel);
            SetText(textContent, feedback);
            SetButtonVisible(textContinueButton, textContinueButtonText, true, "继续");
        }

        private void RefreshDialogue(StoryPlaybackState playback)
        {
            ShowOnlyPanel(dialoguePanel);
            SetPanelActive(submissionPanel, false);
            SetButtonVisible(dialogueContinueButton, dialogueContinueButtonText, finalContinueVisible, "继续");
            SetButtonVisible(choiceButtonA, choiceButtonAText, false, "");
            SetButtonVisible(choiceButtonB, choiceButtonBText, false, "");

            var line = playback.CurrentLine;
            if (line == null)
            {
                ApplyDialogueBackground(playback.Story, null);
                currentDialogueLineNumber = 0;
                SetText(speakerNameText, "");
                SetText(dialogueText, "");
                RefreshPortraits(null);
                RefreshSpeakerExpression(null);
                ApplyPortraitOutlineEffects(null);
                return;
            }

            ApplyDialogueBackground(playback.Story, line);
            currentDialogueLineNumber = GetDialogueLineNumber(line.LineId);
            UpdateDialogueCharacter(line);
            var speakerCharacterId = GetCurrentSpeakerCharacterId(line);
            RefreshPortraits(speakerCharacterId);
            RefreshSpeakerExpression(speakerCharacterId);
            ApplyPortraitOutlineEffects(speakerCharacterId);
            SetText(speakerNameText, GetSpeakerName(speakerCharacterId));

            if (playback.IsWaitingForSubmission || line.IsItemSubmissionLine())
            {
                RefreshSubmission(line);
                return;
            }

            if (TryPlayOpeningWorkNarration(playback, line))
            {
                return;
            }

            if (line.IsChoice)
            {
                ResetTypewriter();
                SetText(dialogueText, "Choose a response.");
                SetButtonVisible(dialogueContinueButton, dialogueContinueButtonText, false, "");
                SetButtonVisible(choiceButtonA, choiceButtonAText, true, line.GetChoiceText(0));
                SetButtonVisible(choiceButtonB, choiceButtonBText, true, line.GetChoiceText(1));
                return;
            }

            SetTypewriterText($"{playback.Story.StoryId}:{line.LineId}", line.Content, dialogueText);
            ApplyDialoguePresentationCue(playback, line);
            SetButtonVisible(dialogueContinueButton, dialogueContinueButtonText, finalContinueVisible, "继续");
            ApplyPresentationInputLockToButtons();
        }

        private bool TryPlayOpeningWorkNarration(StoryPlaybackState playback, DialogueLineDefinition line)
        {
            if (playback == null || line == null || storyService == null)
            {
                return false;
            }

            if (!string.Equals(playback.Story.StoryId, OpeningWorkStoryId, System.StringComparison.Ordinal) ||
                !string.Equals(line.LineId, OpeningWorkNarrationStartLineId, System.StringComparison.Ordinal))
            {
                return false;
            }

            ShowOnlyPanel(dialoguePanel);
            SetRootVisible(true);
            SetText(speakerNameText, string.Empty);
            SetText(dialogueText, string.Empty);
            SetButtonVisible(dialogueContinueButton, dialogueContinueButtonText, false, string.Empty);
            SetButtonVisible(choiceButtonA, choiceButtonAText, false, string.Empty);
            SetButtonVisible(choiceButtonB, choiceButtonBText, false, string.Empty);
            if (openingWorkNarrationRoutine == null)
            {
                openingWorkNarrationRoutine = StartCoroutine(PlayOpeningWorkNarrationRoutine(line));
            }

            return true;
        }

        private IEnumerator PlayOpeningWorkNarrationRoutine(DialogueLineDefinition firstLine)
        {
            openingWorkNarrationActive = true;
            var narrationLines = new List<string> { firstLine.Content };
            var secondLine = storyService != null &&
                storyService.TryGetDialogueLine(OpeningWorkNarrationSecondLineId, out var resolvedSecondLine)
                    ? resolvedSecondLine
                    : null;
            if (secondLine != null)
            {
                narrationLines.Add(secondLine.Content);
            }

            if (openingWorkNarrationDelayBeforeBlackScreen > 0f)
            {
                yield return new WaitForSecondsRealtime(openingWorkNarrationDelayBeforeBlackScreen);
            }

            var uiBootstrap = FindFirstObjectByType<BaseSceneUIBootstrap>(FindObjectsInactive.Include);
            var blackPanel = uiBootstrap != null ? uiBootstrap.ShowBlackScreenPanel() : null;
            if (blackPanel != null)
            {
                yield return blackPanel.FadeIn(0.25f);
                yield return blackPanel.PlayNarrationLines(narrationLines, typewriterCharactersPerSecond);
                yield return blackPanel.FadeOut(0.25f);
                blackPanel.ClearNarration();
                uiBootstrap.HideBlackScreenPanel();
            }

            if (storyService != null && storyService.CurrentPlayback != null)
            {
                storyService.Continue();
                if (storyService.CurrentPlayback != null &&
                    storyService.CurrentPlayback.CurrentLine != null &&
                    string.Equals(storyService.CurrentPlayback.CurrentLine.LineId, OpeningWorkNarrationSecondLineId, System.StringComparison.Ordinal))
                {
                    storyService.Continue();
                }
            }

            openingWorkNarrationActive = false;
            openingWorkNarrationRoutine = null;
        }

        private void RefreshSubmission(DialogueLineDefinition line)
        {
            ResetTypewriter();
            SetText(dialogueText, "");
            SetButtonVisible(dialogueContinueButton, dialogueContinueButtonText, false, "");
            SetButtonVisible(choiceButtonA, choiceButtonAText, false, "");
            SetButtonVisible(choiceButtonB, choiceButtonBText, false, "");
            SetPanelActive(submissionPanel, true);
            SetText(submissionTitleText, "Submit Items");
            SetText(submissionRequirementText, BuildSubmissionRequirements(line));
            SetButtonVisible(submitButton, submitButtonText, true, "Submit");
            SetButtonVisible(exitSubmitButton, exitSubmitButtonText, true, "Exit");
        }

        private string BuildSubmissionRequirements(DialogueLineDefinition line)
        {
            var builder = new StringBuilder();
            for (var index = 0; index < line.RequiredItemIds.Count; index++)
            {
                var itemId = line.GetRequiredItemId(index);
                var requiredCount = line.GetRequiredItemCount(index);
                if (string.IsNullOrEmpty(itemId) || requiredCount <= 0)
                {
                    continue;
                }

                var itemName = itemId;
                if (storyService != null &&
                    storyService.TryGetItemDefinition(itemId, out var definition) &&
                    !string.IsNullOrEmpty(definition.ItemName))
                {
                    itemName = definition.ItemName;
                }

                var currentCount = storyService == null ? 0 : storyService.GetItemCount(itemId);
                builder.Append(itemName)
                    .Append("  ")
                    .Append(currentCount)
                    .Append("/")
                    .Append(requiredCount)
                    .AppendLine();
            }

            return builder.Length > 0 ? builder.ToString() : "No required item configured.";
        }

        private void ApplyDialoguePresentationCue(StoryPlaybackState playback, DialogueLineDefinition line)
        {
            if (playback == null || line == null)
            {
                return;
            }

            var cue = line.PresentationCue;
            var key = $"{playback.Story.StoryId}:{line.LineId}:{cue}";
            if (activePresentationCueKey == key)
            {
                ApplyPresentationInputLockToButtons();
                return;
            }

            activePresentationCueKey = key;
            KillPresentationCueTween(false);
            if (string.IsNullOrWhiteSpace(cue))
            {
                ApplyPresentationInputLockToButtons();
                return;
            }

            if (cue.Contains(SkeletonStartAtPresentationPointCue))
            {
                MoveOpeningSkeletonToCenterLower();
            }

            if (cue.Contains(SkeletonActivateRiseCue))
            {
                PlayOpeningSkeletonActivatePresentation();
            }

            ApplyPresentationInputLockToButtons();
        }

        private void MoveOpeningSkeletonToCenterLower()
        {
            CacheSpeakerExpressionOriginalPosition();
            if (speakerExpressionRectTransform == null)
            {
                return;
            }

            openingSkeletonMotionActive = false;
            if (TryResolveSkeletonPresentationPoint(out var anchoredPosition))
            {
                speakerExpressionRectTransform.anchoredPosition = anchoredPosition;
                openingSkeletonSnapshot = "???????????????";
            }
            else
            {
                openingSkeletonSnapshot = "?????????????????????";
            }

            if (speakerExpressionImage != null)
            {
                speakerExpressionImage.color = Color.black;
            }
        }

        private void PlayOpeningSkeletonActivatePresentation()
        {
            CacheSpeakerExpressionOriginalPosition();
            if (speakerExpressionRectTransform == null || !hasSpeakerExpressionOriginalPosition)
            {
                return;
            }

            var restorePosition = speakerExpressionOriginalAnchoredPosition;
            var startPosition = speakerExpressionRectTransform.anchoredPosition;
            var endPosition = startPosition + Vector2.up * Mathf.Max(0f, openingSkeletonActivateRiseDistance);
            var riseDuration = Mathf.Max(0f, openingSkeletonActivateDuration);
            var returnDuration = Mathf.Max(0f, openingSkeletonReturnDuration);
            var peakHoldDuration = Mathf.Max(0f, openingSkeletonPeakHoldDuration);
            var shakeAmplitude = Mathf.Max(0f, openingSkeletonActivateShakeAmplitude);
            var shakeFrequency = Mathf.Max(0f, openingSkeletonActivateShakeFrequency);
            presentationCueInputLocked = true;
            openingSkeletonMotionActive = false;
            openingSkeletonSnapshot = "??????? S0001_005 ?????";

            var riseProgress = 0f;
            var sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(DOTween.To(() => riseProgress, value =>
                {
                    riseProgress = value;
                    var risePosition = Vector2.Lerp(startPosition, endPosition, riseProgress);
                    var damping = 1f - Mathf.Clamp01(riseProgress * 0.35f);
                    var shakeOffset = Mathf.Sin(riseProgress * riseDuration * shakeFrequency * Mathf.PI * 2f) * shakeAmplitude * damping;
                    speakerExpressionRectTransform.anchoredPosition = risePosition + new Vector2(shakeOffset, 0f);
                    if (speakerExpressionImage != null)
                    {
                        speakerExpressionImage.color = Color.Lerp(Color.black, Color.white, riseProgress);
                    }
                }, 1f, riseDuration).SetEase(Ease.Linear));
            sequence.AppendInterval(peakHoldDuration);
            sequence.Append(speakerExpressionRectTransform.DOAnchorPos(restorePosition, returnDuration).SetEase(Ease.InOutSine));
            sequence.OnComplete(() =>
                {
                    speakerExpressionRectTransform.anchoredPosition = restorePosition;
                    if (speakerExpressionImage != null)
                    {
                        speakerExpressionImage.color = Color.white;
                    }

                    presentationCueInputLocked = false;
                    presentationCueTween = null;
                    ApplyPresentationInputLockToButtons();
                    openingSkeletonSnapshot = "???????? Prefab ??????????";
                    storyService?.Continue();
                })
                .OnKill(() =>
                {
                    presentationCueInputLocked = false;
                    presentationCueTween = null;
                    ApplyPresentationInputLockToButtons();
                });
            presentationCueTween = sequence;
        }

        private void KillPresentationCueTween(bool complete)
        {
            if (presentationCueTween != null && presentationCueTween.IsActive())
            {
                presentationCueTween.Kill(complete);
            }

            presentationCueTween = null;
            presentationCueInputLocked = false;
            ApplyPresentationInputLockToButtons();
        }

        private void ApplyPresentationInputLockToButtons()
        {
            SetButtonInteractable(dialogueContinueButton, !presentationCueInputLocked);
            SetButtonInteractable(choiceButtonA, !presentationCueInputLocked);
            SetButtonInteractable(choiceButtonB, !presentationCueInputLocked);
            SetButtonInteractable(submitButton, !presentationCueInputLocked);
            SetButtonInteractable(exitSubmitButton, !presentationCueInputLocked);
            SetButtonInteractable(textContinueButton, !presentationCueInputLocked);
            SetButtonInteractable(imageContinueButton, !presentationCueInputLocked);
            SetButtonInteractable(storyAreaButton, !presentationCueInputLocked);
        }

        private static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
        private void RefreshTextStory(StoryPlaybackState playback)
        {
            ShowOnlyPanel(textStoryPanel);
            var story = playback.Story;
            var builder = new StringBuilder();
            for (var index = 0; index < playback.PresentationIndex; index++)
            {
                builder.AppendLine(GetTextSegment(story, index));
                builder.AppendLine();
            }

            var currentSegment = GetTextSegment(story, playback.PresentationIndex);
            var key = $"{story.StoryId}:text:{playback.PresentationIndex}";
            SetTypewriterText(key, currentSegment, textContent, builder.ToString());
            SetButtonVisible(textContinueButton, textContinueButtonText, finalContinueVisible, "继续");
        }

        private void RefreshImageStory(StoryPlaybackState playback)
        {
            ShowOnlyPanel(imageStoryPanel);
            ResetTypewriter();
            var story = playback.Story;
            var imageCount = Mathf.Max(1, story.ImageIds.Count);
            if (story.ImageDisplayMode == StoryImageDisplayMode.ComicPanels)
            {
                RefreshComicPanels(story, playback.PresentationIndex);
                SetImageVisible(storyImage, false);
                SetText(imageCaptionText, story.GetImageCaption(playback.PresentationIndex));
            }
            else
            {
                SetImageVisible(storyImage, true);
                SetSprite(storyImage, GetImageId(story, playback.PresentationIndex));
                HideComicPanels();
                SetText(imageCaptionText, story.GetImageCaption(playback.PresentationIndex));
            }

            SetButtonVisible(imageContinueButton, imageContinueButtonText, finalContinueVisible, "继续");
        }

        private void RefreshComicPanels(StoryDefinition story, int presentationIndex)
        {
            if (comicPanelImages == null || comicPanelImages.Length == 0)
            {
                SetImageVisible(storyImage, true);
                SetSprite(storyImage, GetImageId(story, presentationIndex));
                return;
            }

            var visibleCount = presentationIndex + 1;
            for (var index = 0; index < comicPanelImages.Length; index++)
            {
                var image = comicPanelImages[index];
                var visible = index < story.ImageIds.Count && index < visibleCount;
                SetImageVisible(image, visible);
                if (visible)
                {
                    SetSprite(image, GetImageId(story, index));
                }
            }
        }

        private void HideComicPanels()
        {
            if (comicPanelImages == null)
            {
                return;
            }

            foreach (var image in comicPanelImages)
            {
                SetImageVisible(image, false);
            }
        }

        private static string GetTextSegment(StoryDefinition story, int index)
        {
            return index >= 0 && index < story.TextSegments.Count ? story.TextSegments[index] : story.TextContent;
        }

        private static string GetImageId(StoryDefinition story, int index)
        {
            return index >= 0 && index < story.ImageIds.Count ? story.ImageIds[index] : story.ImageId;
        }

        private void SetTypewriterText(string key, string value, TMP_Text target, string prefix = "")
        {
            if (value == null)
            {
                value = string.Empty;
            }
            if (activeTypewriterKey != key || targetTypewriterText != value)
            {
                activeTypewriterKey = key;
                targetTypewriterText = value;
                visibleTypewriterCharacters = 0;
                typewriterTimer = 0f;
                isTypewriting = value.Length > 0;
            }

            if (!isTypewriting && visibleTypewriterCharacters <= 0)
            {
                visibleTypewriterCharacters = value.Length;
            }

            SetText(target, prefix + value.Substring(0, Mathf.Min(visibleTypewriterCharacters, value.Length)));
        }

        private void ApplyTypewriterText()
        {
            var playback = storyService == null ? null : storyService.CurrentPlayback;
            if (playback == null || playback.IsCompleted)
            {
                return;
            }

            if (playback.Story.StoryType == StoryType.Text)
            {
                RefreshTextStory(playback);
            }
            else if (playback.Story.StoryType == StoryType.Dialogue)
            {
                SetText(dialogueText, targetTypewriterText.Substring(0, Mathf.Min(visibleTypewriterCharacters, targetTypewriterText.Length)));
            }
        }

        private bool RevealTypewriterIfNeeded()
        {
            if (!isTypewriting)
            {
                return false;
            }

            visibleTypewriterCharacters = targetTypewriterText.Length;
            typewriterTimer = targetTypewriterText.Length;
            isTypewriting = false;
            ApplyTypewriterText();
            return true;
        }

        private void ResetTypewriter()
        {
            activeTypewriterKey = string.Empty;
            targetTypewriterText = string.Empty;
            visibleTypewriterCharacters = 0;
            typewriterTimer = 0f;
            isTypewriting = false;
        }

        private void ResetFinalContinue()
        {
            finalContinueVisible = false;
            finalContinueKey = string.Empty;
        }

        private void RefreshFinalContinueKey(StoryPlaybackState playback)
        {
            var key = GetCurrentStoryStepKey(playback);
            if (finalContinueKey != key)
            {
                finalContinueKey = key;
                finalContinueVisible = false;
            }
        }

        private void ShowFinalContinueButton(StoryPlaybackState playback)
        {
            finalContinueKey = GetCurrentStoryStepKey(playback);
            finalContinueVisible = true;
            Refresh();
        }

        private static string GetCurrentStoryStepKey(StoryPlaybackState playback)
        {
            if (playback == null || playback.Story == null)
            {
                return string.Empty;
            }

            if (playback.Story.StoryType == StoryType.Dialogue)
            {
                return playback.CurrentLine != null
                    ? $"{playback.Story.StoryId}:dialogue:{playback.CurrentLine.LineId}"
                    : $"{playback.Story.StoryId}:dialogue:none";
            }

            return $"{playback.Story.StoryId}:{playback.Story.StoryType}:{playback.PresentationIndex}";
        }

        private static bool IsCurrentStoryStepLast(StoryPlaybackState playback)
        {
            if (playback == null || playback.Story == null)
            {
                return false;
            }

            var story = playback.Story;
            if (story.StoryType == StoryType.Image)
            {
                var imageCount = Mathf.Max(1, story.ImageIds.Count);
                return playback.PresentationIndex >= imageCount - 1;
            }

            if (story.StoryType == StoryType.Text)
            {
                var textCount = Mathf.Max(1, story.TextSegments.Count);
                return playback.PresentationIndex >= textCount - 1;
            }

            var line = playback.CurrentLine;
            if (line == null || line.IsChoice || line.IsItemSubmissionLine())
            {
                return false;
            }

            var nextLineId = line.GetNextLineId(0);
            return string.IsNullOrWhiteSpace(nextLineId) ||
                string.Equals(nextLineId.Trim(), "END", System.StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateDialogueCharacter(DialogueLineDefinition line)
        {
            if (string.IsNullOrEmpty(line.SpeakerCharacterId))
            {
                return;
            }

            if (line.Position == 1)
            {
                if (IsSameDialogueCharacter(rightCharacterId, line.SpeakerCharacterId))
                {
                    rightCharacterId = string.Empty;
                }

                leftCharacterId = line.SpeakerCharacterId;
            }
            else
            {
                if (IsSameDialogueCharacter(leftCharacterId, line.SpeakerCharacterId))
                {
                    leftCharacterId = string.Empty;
                }

                rightCharacterId = line.SpeakerCharacterId;
            }
        }

        private static bool IsSameDialogueCharacter(string firstCharacterId, string secondCharacterId)
        {
            if (string.IsNullOrEmpty(firstCharacterId) || string.IsNullOrEmpty(secondCharacterId))
            {
                return false;
            }

            var firstDisplayName = CharacterDisplayNameUtility.GetDisplayName(firstCharacterId);
            var secondDisplayName = CharacterDisplayNameUtility.GetDisplayName(secondCharacterId);
            return !string.IsNullOrEmpty(firstDisplayName) &&
                   string.Equals(firstDisplayName, secondDisplayName, System.StringComparison.Ordinal);
        }

        private string GetCurrentSpeakerCharacterId(DialogueLineDefinition line)
        {
            if (!string.IsNullOrEmpty(line.SpeakerCharacterId))
            {
                return line.SpeakerCharacterId;
            }

            return line.Position == 1 ? leftCharacterId : rightCharacterId;
        }

        private void RefreshPortraits(string activeSpeakerCharacterId)
        {
            SetPortrait(leftPortrait, leftCharacterId, activeSpeakerCharacterId);
            SetPortrait(rightPortrait, rightCharacterId, activeSpeakerCharacterId);
        }

        private void RefreshSpeakerExpression(string activeSpeakerCharacterId)
        {
            if (speakerExpressionImage == null)
            {
                return;
            }

            if (TryRefreshOpeningSkeletonExpression())
            {
                return;
            }

            ResetOpeningSkeletonMotion();
            speakerExpressionImage.color = Color.white;

            if (!IsSkeletonExpressionId(activeSpeakerCharacterId) ||
                !StoryImageResourceProvider.TryLoadSprite(activeSpeakerCharacterId, SkeletonExpressionRoots, out var expressionSprite))
            {
                speakerExpressionImage.sprite = originalSpeakerExpressionSprite;
                speakerExpressionImage.enabled = originalSpeakerExpressionSprite != null;
                return;
            }

            speakerExpressionImage.sprite = expressionSprite;
            speakerExpressionImage.enabled = true;
        }

        private bool TryRefreshOpeningSkeletonExpression()
        {
            if (!enableOpeningSkeletonPresentation ||
                !string.Equals(currentStoryId, OpeningSkeletonStoryId, System.StringComparison.Ordinal) ||
                currentDialogueLineNumber <= 0)
            {
                return false;
            }

            CacheSpeakerExpressionOriginalPosition();
            if (!StoryImageResourceProvider.TryLoadSprite(OpeningSkeletonDefaultExpressionId, SkeletonExpressionRoots, out var expressionSprite))
            {
                openingSkeletonSnapshot = "\u5f00\u573a\u9ab7\u9ac5\uff1a\u672a\u627e\u5230\u9ab7\u9ac5\u601d\u8003\u8d44\u6e90";
                return false;
            }

            speakerExpressionImage.sprite = expressionSprite;
            speakerExpressionImage.enabled = true;
            speakerExpressionImage.color = currentDialogueLineNumber <= 5 ? Color.black : Color.white;
            if (!openingSkeletonMotionActive && presentationCueTween == null)
            {
                openingSkeletonSnapshot = currentDialogueLineNumber <= 5
                    ? $"开场骷髅：第 {currentDialogueLineNumber} 行，黑色剪影"
                    : $"开场骷髅：第 {currentDialogueLineNumber} 行，等待演出栏动效";
            }

            return true;
        }

        private void UpdateOpeningSkeletonMotion()
        {
            if (!openingSkeletonMotionActive || speakerExpressionRectTransform == null || !hasSpeakerExpressionOriginalPosition)
            {
                return;
            }

            speakerExpressionRectTransform.anchoredPosition = speakerExpressionOriginalAnchoredPosition;
        }

        private void ResetOpeningSkeletonMotion()
        {
            openingSkeletonMotionActive = false;
            RestoreSpeakerExpressionOriginalPosition();
            if (!string.Equals(currentStoryId, OpeningSkeletonStoryId, System.StringComparison.Ordinal))
            {
                openingSkeletonSnapshot = "\u5f00\u573a\u9ab7\u9ac5\uff1a\u7b49\u5f85 S0001";
            }
        }

        private void CacheSpeakerExpressionOriginalPosition()
        {
            if (speakerExpressionImage == null)
            {
                return;
            }

            if (speakerExpressionRectTransform == null)
            {
                speakerExpressionRectTransform = speakerExpressionImage.rectTransform;
            }

            if (speakerExpressionRectTransform != null && !hasSpeakerExpressionOriginalPosition)
            {
                speakerExpressionOriginalAnchoredPosition = speakerExpressionRectTransform.anchoredPosition;
                hasSpeakerExpressionOriginalPosition = true;
            }
        }

        private void RestoreSpeakerExpressionOriginalPosition()
        {
            if (speakerExpressionRectTransform != null && hasSpeakerExpressionOriginalPosition)
            {
                speakerExpressionRectTransform.anchoredPosition = speakerExpressionOriginalAnchoredPosition;
            }
        }

        private static int GetDialogueLineNumber(string lineId)
        {
            if (string.IsNullOrEmpty(lineId))
            {
                return 0;
            }

            var separatorIndex = lineId.LastIndexOf('_');
            var numberText = separatorIndex >= 0 && separatorIndex < lineId.Length - 1
                ? lineId.Substring(separatorIndex + 1)
                : lineId;
            return int.TryParse(numberText, out var number) ? number : 0;
        }

        private static bool IsSkeletonExpressionId(string characterId)
        {
            return !string.IsNullOrEmpty(characterId) &&
                   characterId.StartsWith(SkeletonCharacterNamePrefix, System.StringComparison.Ordinal);
        }

        private void ApplyPortraitOutlineEffects(string activeSpeakerCharacterId)
        {
            ApplyPortraitOutlineEffect(
                leftPortrait,
                ref leftPortraitOutlineMaterial,
                originalLeftPortraitMaterial,
                leftCharacterId,
                activeSpeakerCharacterId);
            ApplyPortraitOutlineEffect(
                rightPortrait,
                ref rightPortraitOutlineMaterial,
                originalRightPortraitMaterial,
                rightCharacterId,
                activeSpeakerCharacterId);
            ApplyPortraitOutlineEffect(
                speakerExpressionImage,
                ref speakerExpressionOutlineMaterial,
                originalSpeakerExpressionMaterial,
                activeSpeakerCharacterId,
                activeSpeakerCharacterId);
        }

        private void ApplyPortraitOutlineEffect(
            Image portrait,
            ref Material runtimeMaterial,
            Material originalMaterial,
            string characterId,
            string activeSpeakerCharacterId)
        {
            if (portrait == null)
            {
                return;
            }

            if (!portrait.enabled || portrait.sprite == null || string.IsNullOrEmpty(characterId))
            {
                portrait.material = originalMaterial;
                return;
            }

            var material = EnsurePortraitOutlineMaterial(ref runtimeMaterial, portrait.name);
            if (material == null)
            {
                portrait.material = originalMaterial;
                return;
            }

            var isActiveSpeaker = !string.IsNullOrEmpty(characterId) && characterId == activeSpeakerCharacterId;
            material.SetColor(OutlineColorId, isActiveSpeaker ? activePortraitOutlineColor : inactivePortraitOutlineColor);
            material.SetColor(GlowColorId, isActiveSpeaker ? activePortraitGlowColor : inactivePortraitGlowColor);
            material.SetFloat(OutlinePixelWidthId, Mathf.Clamp(portraitOutlinePixelWidth, 0, 12));
            material.SetFloat(GlowPixelWidthId, Mathf.Clamp(portraitGlowPixelWidth, 1, 32));
            material.SetFloat(GlowIntensityId, Mathf.Clamp01(portraitGlowIntensity));
            material.SetFloat(GlowFalloffPowerId, Mathf.Clamp(portraitGlowFalloffPower, 1f, 4f));
            portrait.material = material;
        }

        private Material EnsurePortraitOutlineMaterial(ref Material runtimeMaterial, string materialName)
        {
            if (runtimeMaterial != null)
            {
                return runtimeMaterial;
            }

            var templateMaterial = portraitOutlineTemplateMaterial;
            if (templateMaterial == null)
            {
                templateMaterial = Resources.Load<Material>(PortraitOutlineMaterialResourcePath);
            }

            if (templateMaterial != null)
            {
                runtimeMaterial = new Material(templateMaterial)
                {
                    name = $"{name}_{materialName}_RuntimePortraitOutlineMaterial",
                    hideFlags = HideFlags.HideAndDontSave
                };
                return runtimeMaterial;
            }

            if (portraitOutlineShader == null)
            {
                portraitOutlineShader = Shader.Find("TwelveMoons/UI/PortraitAlphaOutline");
            }

            if (portraitOutlineShader == null)
            {
                Debug.LogWarning("缺少 TwelveMoons/UI/PortraitAlphaOutline Shader，剧情人物白色描边晕圈会隐藏，避免显示成整个人物半透明白图。", this);
                return null;
            }

            runtimeMaterial = new Material(portraitOutlineShader)
            {
                name = $"{name}_{materialName}_RuntimePortraitOutlineMaterial",
                hideFlags = HideFlags.HideAndDontSave
            };
            return runtimeMaterial;
        }

        private void SetPortrait(Image portrait, string characterId, string activeSpeakerCharacterId)
        {
            if (portrait == null)
            {
                return;
            }

            SetPortraitSprite(portrait, characterId);
            portrait.transform.localScale = Vector3.one;
            portrait.color = !string.IsNullOrEmpty(characterId) && characterId == activeSpeakerCharacterId
                ? activeSpeakerPortraitColor
                : inactiveSpeakerPortraitColor;
        }

        private void SetPortraitSprite(Image portrait, string characterId)
        {
            if (portrait == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(characterId) &&
                storyService != null &&
                storyService.TryGetCharacter(characterId, out var character))
            {
                portrait.sprite = CharacterPlaceholderPortraitProvider.LoadPortrait(character.PortraitId);
            }
            else
            {
                portrait.sprite = CharacterPlaceholderPortraitProvider.LoadPortrait(characterId);
            }

            portrait.enabled = portrait.sprite != null;
            if (portrait.sprite != null)
            {
                portrait.SetNativeSize();
            }
        }

        private string GetSpeakerName(string characterId)
        {
            if (!string.IsNullOrEmpty(characterId) &&
                storyService != null &&
                storyService.TryGetCharacter(characterId, out var character) &&
                !string.IsNullOrEmpty(character.CharacterName))
            {
                return character.CharacterName;
            }

            return CharacterDisplayNameUtility.GetDisplayName(characterId);
        }

        private void ResetDialogueCharacters()
        {
            leftCharacterId = string.Empty;
            rightCharacterId = string.Empty;
            currentDialogueLineNumber = 0;
            ResetOpeningSkeletonMotion();
            RefreshPortraits(null);
            RefreshSpeakerExpression(null);
            ApplyPortraitOutlineEffects(null);
        }

        private void ClearStoryVisualState()
        {
            ClearPortraitImage(leftPortrait, originalLeftPortraitMaterial);
            ClearPortraitImage(rightPortrait, originalRightPortraitMaterial);
            ClearPortraitImage(speakerExpressionImage, originalSpeakerExpressionMaterial);
            RestoreSpeakerExpressionOriginalPosition();
            openingSkeletonMotionActive = false;
            activePresentationCueKey = string.Empty;
            openingSkeletonSnapshot = "\u5f00\u573a\u9ab7\u9ac5\uff1a\u7b49\u5f85 S0001";
        }

        private static void ClearPortraitImage(Image portrait, Material originalMaterial)
        {
            if (portrait == null)
            {
                return;
            }

            portrait.sprite = null;
            portrait.enabled = false;
            portrait.color = Color.white;
            portrait.material = originalMaterial;
            portrait.transform.localScale = Vector3.one;
        }

        private void ShowOnlyPanel(GameObject activePanel)
        {
            SetPanelActive(dialoguePanel, activePanel == dialoguePanel);
            SetPanelActive(imageStoryPanel, activePanel == imageStoryPanel);
            SetPanelActive(textStoryPanel, activePanel == textStoryPanel);
            if (activePanel != dialoguePanel)
            {
                SetPanelActive(submissionPanel, false);
            }
        }

        private void SetRootVisible(bool visible)
        {
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = visible ? 1f : 0f;
                rootCanvasGroup.blocksRaycasts = visible;
                rootCanvasGroup.interactable = visible;
            }

            if (rootBackgroundImage != null)
            {
                var color = rootBackgroundImage.color;
                color.a = 1f;
                rootBackgroundImage.color = color;
                rootBackgroundImage.raycastTarget = visible;
            }

            if (storyAreaButton != null)
            {
                storyAreaButton.gameObject.SetActive(visible);
            }
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private static void SetButtonVisible(Button button, TMP_Text label, bool visible, string text)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }

            SetText(label, text);
        }

        private static void SetImageVisible(Image image, bool visible)
        {
            if (image != null)
            {
                image.gameObject.SetActive(visible);
            }
        }

        private static void SetSprite(Image image, string imageId)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = string.IsNullOrEmpty(imageId) ? null : StoryImageResourceProvider.LoadSprite(imageId);
            image.enabled = image.sprite != null;
            image.color = image.sprite == null
                ? new Color(0.19f, 0.2f, 0.22f, 1f)
                : Color.white;
        }
    }
}
