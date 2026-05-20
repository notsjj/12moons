using System;

namespace TwelveMoons.Core.Runtime
{
    [Serializable]
    public sealed class RuntimeDocumentQueueEntry
    {
        public RuntimeDocumentQueueEntry(
            string documentId,
            string taskId,
            string taskStageId,
            string beforeDocumentCharacterId,
            int queuedRound)
        {
            DocumentId = documentId;
            TaskId = taskId;
            TaskStageId = taskStageId;
            BeforeDocumentCharacterId = beforeDocumentCharacterId;
            QueuedRound = Math.Max(1, queuedRound);
        }

        public string DocumentId { get; }

        public string TaskId { get; }

        public string TaskStageId { get; }

        public string BeforeDocumentCharacterId { get; }

        public int QueuedRound { get; }
    }
}
