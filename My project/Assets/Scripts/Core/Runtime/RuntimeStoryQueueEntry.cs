using System;

namespace TwelveMoons.Core.Runtime
{
    public enum RuntimeStoryQueueTiming
    {
        StageStart,
        StageEnd,
        BeforeDocument,
        ExploreBefore,
        ExploreAfter
    }

    [Serializable]
    public sealed class RuntimeStoryQueueEntry
    {
        public RuntimeStoryQueueEntry(
            string storyId,
            string taskId,
            string taskStageId,
            int queuedRound,
            RuntimeStoryQueueTiming timing)
        {
            StoryId = storyId ?? string.Empty;
            TaskId = taskId ?? string.Empty;
            TaskStageId = taskStageId ?? string.Empty;
            QueuedRound = Math.Max(1, queuedRound);
            Timing = timing;
        }

        public string StoryId { get; }

        public string TaskId { get; }

        public string TaskStageId { get; }

        public int QueuedRound { get; }

        public RuntimeStoryQueueTiming Timing { get; }
    }
}
