using System;

namespace TwelveMoons.Core.Runtime
{
    [Serializable]
    public sealed class RuntimeStoryProgressState
    {
        public RuntimeStoryProgressState(string storyId, string lineId, bool waitingForSubmission)
        {
            StoryId = storyId;
            LineId = lineId;
            WaitingForSubmission = waitingForSubmission;
        }

        public string StoryId { get; private set; }

        public string LineId { get; private set; }

        public bool WaitingForSubmission { get; private set; }

        public void SetProgress(string lineId, bool waitingForSubmission)
        {
            LineId = lineId;
            WaitingForSubmission = waitingForSubmission;
        }
    }
}
