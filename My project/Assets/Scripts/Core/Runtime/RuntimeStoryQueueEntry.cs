using System;

namespace TwelveMoons.Core.Runtime
{
    public enum RuntimeStoryQueueTiming
    {
        StageStart,
        StageEnd,
        BeforeDocument
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
            StoryId = storyId;
            TaskId = taskId;
            TaskStageId = taskStageId;
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
