using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class StoryDebugControls : MonoBehaviour
    {
        [SerializeField] private StoryService storyService;
        [SerializeField] private GameRuntimeDataSource runtimeDataSource = GameRuntimeDataSource.Queue;
        [SerializeField] private string demoDialogueStoryId = "story_relief_start";
        [SerializeField] private string demoTextStoryId = "story_demo_text";
        [SerializeField] private string demoImageStoryId = "story_demo_image";
        [SerializeField] private string demoComicImageStoryId = "story_demo_comic_image";
        [SerializeField] private string demoSubmissionStoryId = "story_demo_submission";
        [SerializeField] private TMP_Text feedbackText;

        private enum GameRuntimeDataSource
        {
            Queue,
            Direct
        }

        private void Awake()
        {
            if (storyService == null)
            {
                storyService = FindFirstObjectByType<StoryService>();
            }
        }

        public void StartNextQueuedStory()
        {
            if (storyService != null && storyService.StartNextQueuedStory())
            {
                SetFeedback("Started next queued story.");
                return;
            }

            SetFeedback("No queued story to start.");
        }

        public void StartDemoDialogue()
        {
            StartStory(demoDialogueStoryId);
        }

        public void StartDemoText()
        {
            StartStory(demoTextStoryId);
        }

        public void StartDemoImage()
        {
            StartStory(demoImageStoryId);
        }

        public void StartDemoComicImage()
        {
            StartStory(demoComicImageStoryId);
        }

        public void StartDemoSubmission()
        {
            StartStory(demoSubmissionStoryId);
        }

        public void RefreshStories()
        {
            storyService?.Refresh();
            SetFeedback(runtimeDataSource == GameRuntimeDataSource.Queue
                ? "Story config refreshed. Queue mode is active."
                : "Story config refreshed. Direct mode is active.");
        }

        private void StartStory(string storyId)
        {
            if (storyService != null && storyService.StartStory(storyId))
            {
                SetFeedback($"Started story {storyId}.");
                return;
            }

            SetFeedback($"Cannot start story {storyId}.");
        }

        private void SetFeedback(string value)
        {
            if (feedbackText != null)
            {
                feedbackText.text = value;
            }
        }
    }
}
