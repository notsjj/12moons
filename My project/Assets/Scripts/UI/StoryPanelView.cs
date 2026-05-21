using System.Text;
using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class StoryPanelView : MonoBehaviour
    {
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

        public void OnContinueClicked()
        {
            OnStoryAreaClicked();
        }

        public void OnStoryAreaClicked()
        {
            if (RevealTypewriterIfNeeded())
            {
                return;
            }

            storyService?.Continue();
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
            }

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
            SetButtonVisible(textContinueButton, textContinueButtonText, true, "Close");
        }

        private void RefreshDialogue(StoryPlaybackState playback)
        {
            ShowOnlyPanel(dialoguePanel);
            SetPanelActive(submissionPanel, false);
            SetButtonVisible(dialogueContinueButton, dialogueContinueButtonText, true, "Continue");
            SetButtonVisible(choiceButtonA, choiceButtonAText, false, "");
            SetButtonVisible(choiceButtonB, choiceButtonBText, false, "");

            var line = playback.CurrentLine;
            if (line == null)
            {
                SetText(speakerNameText, "");
                SetText(dialogueText, "");
                RefreshPortraits(null);
                return;
            }

            UpdateDialogueCharacter(line);
            var speakerCharacterId = GetCurrentSpeakerCharacterId(line);
            RefreshPortraits(speakerCharacterId);
            RefreshSpeakerExpression(speakerCharacterId);
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
            SetButtonVisible(textContinueButton, textContinueButtonText, false, "");
        }

        private void RefreshImageStory(StoryPlaybackState playback)
        {
            ShowOnlyPanel(imageStoryPanel);
            ResetTypewriter();
            var story = playback.Story;
            var imageCount = Mathf.Max(1, story.ImageIds.Count);
            var isLast = playback.PresentationIndex >= imageCount - 1;

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

            SetButtonVisible(imageContinueButton, imageContinueButtonText, isLast, "Continue");
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
            SetPortraitSprite(speakerExpressionImage, activeSpeakerCharacterId);
        }

        private void SetPortrait(Image portrait, string characterId, string activeSpeakerCharacterId)
        {
            if (portrait == null)
            {
                return;
            }

            SetPortraitSprite(portrait, characterId);
            portrait.transform.localScale = !string.IsNullOrEmpty(characterId) && characterId == activeSpeakerCharacterId
                ? Vector3.one * 1.08f
                : Vector3.one;
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
                portrait.sprite = string.IsNullOrEmpty(character.PortraitId)
                    ? null
                    : Resources.Load<Sprite>(character.PortraitId);
            }
            else
            {
                portrait.sprite = null;
            }

            portrait.enabled = portrait.sprite != null;
            portrait.color = Color.white;
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
            if (visible)
            {
                transform.SetAsLastSibling();
            }

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
