namespace TwelveMoons.Core.Runtime
{
    public sealed class DocumentResolutionResult
    {
        public DocumentResolutionResult(bool success, string message, string proposerFeedbackText, string factionFeedbackText)
        {
            Success = success;
            Message = message;
            ProposerFeedbackText = proposerFeedbackText;
            FactionFeedbackText = factionFeedbackText;
        }

        public bool Success { get; }

        public string Message { get; }

        public string ProposerFeedbackText { get; }

        public string FactionFeedbackText { get; }
    }
}
