using System;

namespace TwelveMoons.Core.Runtime
{
    [Serializable]
    public sealed class RuntimeFollowUpDocumentState
    {
        public RuntimeFollowUpDocumentState(
            string documentId,
            string sourceDocumentId,
            string taskId,
            string taskStageId,
            string beforeDocumentCharacterId,
            int activateRound)
        {
            DocumentId = documentId ?? string.Empty;
            SourceDocumentId = sourceDocumentId ?? string.Empty;
            TaskId = taskId ?? string.Empty;
            TaskStageId = taskStageId ?? string.Empty;
            BeforeDocumentCharacterId = beforeDocumentCharacterId ?? string.Empty;
            ActivateRound = Math.Max(1, activateRound);
        }

        public string DocumentId { get; }

        public string SourceDocumentId { get; }

        public string TaskId { get; }

        public string TaskStageId { get; }

        public string BeforeDocumentCharacterId { get; }

        public int ActivateRound { get; }
    }
}
