namespace TwelveMoons.Core.Runtime
{
    public sealed class DocumentResolutionResult
    {
        public DocumentResolutionResult(bool success, string message, string proposerFeedbackText, string factionFeedbackText)
            : this(success, message, proposerFeedbackText, factionFeedbackText, string.Empty)
        {
        }

        public DocumentResolutionResult(
            bool success,
            string message,
            string proposerFeedbackText,
            string factionFeedbackText,
            string feedbackFactionId)
        {
            Success = success;
            Message = message;
            ProposerFeedbackText = proposerFeedbackText;
            FactionFeedbackText = factionFeedbackText;
            FeedbackFactionId = feedbackFactionId;
            MostAffectedFactionId = feedbackFactionId;
        }

        public bool Success { get; }

        public string Message { get; }

        public string ProposerFeedbackText { get; }

        public string FactionFeedbackText { get; }

        public string FeedbackFactionId { get; }

        public string MostAffectedFactionId { get; }
    }
}
