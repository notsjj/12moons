using System.Text;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class StoryPanelView : MonoBehaviour
    {
        private const string PortraitOutlineMaterialResourcePath = "Materials/UI/PortraitAlphaOutlineRuntime";
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

        [Header("对话面板：显示角色、对白、继续按钮和选项")]
        [SerializeField] private GameObject dialoguePanel;
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

            CacheOriginalPortraitMaterials();
            EnsureStoryAreaButtonBinding();
        }

        private void Update()
        {
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
            DestroyRuntimeMaterial(ref leftPortraitOutlineMaterial);
            DestroyRuntimeMaterial(ref rightPortraitOutlineMaterial);
            DestroyRuntimeMaterial(ref speakerExpressionOutlineMaterial);
        }

        private void CacheOriginalPortraitMaterials()
        {
            originalLeftPortraitMaterial = leftPortrait != null ? leftPortrait.material : null;
            originalRightPortraitMaterial = rightPortrait != null ? rightPortrait.material : null;
            originalSpeakerExpressionMaterial = speakerExpressionImage != null ? speakerExpressionImage.material : null;
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
            if (!finalContinueVisible)
            {
                return;
            }

            finalContinueVisible = false;
            finalContinueKey = string.Empty;
            storyService?.Continue();
        }

        public void OnStoryAreaClicked()
        {
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
            storyService?.ChooseOption(0);
        }

        public void OnOptionBClicked()
        {
            storyService?.ChooseOption(1);
        }

        public void OnSubmitClicked()
        {
            storyService?.SubmitCurrentItems();
        }

        public void OnExitSubmitClicked()
        {
            storyService?.ExitItemSubmission();
        }

        public void Refresh()
        {
            if (storyService == null || storyService.CurrentPlayback == null)
            {
                currentStoryId = string.Empty;
                ResetDialogueCharacters();
                ResetTypewriter();
                ResetFinalContinue();
                SetText(titleText, "Story");
                SetText(feedbackText, "");
                ShowOnlyPanel(null);
                SetRootVisible(false);
                return;
            }

            SetRootVisible(true);
            var playback = storyService.CurrentPlayback;
            var story = playback.Story;
            if (story.StoryId != currentStoryId)
            {
                currentStoryId = story.StoryId;
                ResetDialogueCharacters();
                ResetTypewriter();
                ResetFinalContinue();
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
                SetText(speakerNameText, "");
                SetText(dialogueText, "");
                RefreshPortraits(null);
                RefreshSpeakerExpression(null);
                ApplyPortraitOutlineEffects(null);
                return;
            }

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
            SetButtonVisible(dialogueContinueButton, dialogueContinueButtonText, finalContinueVisible, "继续");
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
                rightCharacterId = line.SpeakerCharacterId;
            }
            else
            {
                leftCharacterId = line.SpeakerCharacterId;
            }
        }

        private string GetCurrentSpeakerCharacterId(DialogueLineDefinition line)
        {
            if (!string.IsNullOrEmpty(line.SpeakerCharacterId))
            {
                return line.SpeakerCharacterId;
            }

            return line.Position == 1 ? rightCharacterId : leftCharacterId;
        }

        private void RefreshPortraits(string activeSpeakerCharacterId)
        {
            SetPortrait(leftPortrait, leftCharacterId, activeSpeakerCharacterId);
            SetPortrait(rightPortrait, rightCharacterId, activeSpeakerCharacterId);
        }

        private void RefreshSpeakerExpression(string activeSpeakerCharacterId)
        {
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

            return characterId;
        }

        private void ResetDialogueCharacters()
        {
            leftCharacterId = string.Empty;
            rightCharacterId = string.Empty;
            RefreshPortraits(null);
            RefreshSpeakerExpression(null);
            ApplyPortraitOutlineEffects(null);
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

            image.sprite = string.IsNullOrEmpty(imageId) ? null : Resources.Load<Sprite>(imageId);
            image.enabled = image.sprite != null;
            image.color = image.sprite == null
                ? new Color(0.19f, 0.2f, 0.22f, 1f)
                : Color.white;
        }
    }
}
