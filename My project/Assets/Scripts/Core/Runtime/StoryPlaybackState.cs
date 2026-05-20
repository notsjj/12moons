namespace TwelveMoons.Core.Runtime
{
    public sealed class StoryPlaybackState
    {
        public StoryPlaybackState(StoryDefinition story, DialogueLineDefinition line)
        {
            Story = story;
            CurrentLine = line;
            IsCompleted = false;
            Feedback = string.Empty;
            PresentationIndex = 0;
            IsWaitingForSubmission = line != null && line.IsItemSubmissionLine();
        }

        public StoryDefinition Story { get; private set; }

        public DialogueLineDefinition CurrentLine { get; private set; }

        public bool IsCompleted { get; private set; }

        public string Feedback { get; private set; }

        public int PresentationIndex { get; private set; }

        public bool IsWaitingForSubmission { get; private set; }

        public void SetLine(DialogueLineDefinition line)
        {
            CurrentLine = line;
            Feedback = string.Empty;
            IsWaitingForSubmission = line != null && line.IsItemSubmissionLine();
        }

        public void SetFeedback(string feedback)
        {
            Feedback = feedback ?? string.Empty;
        }

        public void SetPresentationIndex(int index)
        {
            PresentationIndex = index < 0 ? 0 : index;
            Feedback = string.Empty;
        }

        public void SetWaitingForSubmission(bool waiting)
        {
            IsWaitingForSubmission = waiting;
        }

        public void Complete(string feedback)
        {
            CurrentLine = null;
            IsCompleted = true;
            Feedback = feedback ?? string.Empty;
            IsWaitingForSubmission = false;
        }
    }
}
