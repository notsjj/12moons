using System;
using System.Collections.Generic;

namespace TwelveMoons.Core.Runtime
{
    public enum TaskRuntimeStatus
    {
        NotStarted,
        Active,
        Completed,
        Failed
    }

    [Serializable]
    public sealed class RuntimeTaskState
    {
        public RuntimeTaskState(string taskId)
        {
            TaskId = taskId;
            Status = TaskRuntimeStatus.NotStarted;
            ActivatedRound = 0;
            CompletedRound = 0;
            Score = 0;
        }

        private readonly List<string> processedStageStarts = new List<string>();
        private readonly List<string> processedStageEnds = new List<string>();

        public string TaskId { get; private set; }

        public TaskRuntimeStatus Status { get; private set; }

        public int ActivatedRound { get; private set; }

        public int CompletedRound { get; private set; }

        public int Score { get; private set; }

        public IReadOnlyList<string> ProcessedStageStarts => processedStageStarts;

        public IReadOnlyList<string> ProcessedStageEnds => processedStageEnds;

        public void Activate(int currentRound)
        {
            if (Status == TaskRuntimeStatus.NotStarted)
            {
                Status = TaskRuntimeStatus.Active;
                ActivatedRound = Math.Max(1, currentRound);
            }
        }

        public void AddScore(int delta)
        {
            Score += delta;
        }

        public bool HasProcessedStageStart(string taskStageId)
        {
            return processedStageStarts.Contains(taskStageId);
        }

        public bool HasProcessedStageEnd(string taskStageId)
        {
            return processedStageEnds.Contains(taskStageId);
        }

        public void MarkStageStartProcessed(string taskStageId)
        {
            if (!string.IsNullOrEmpty(taskStageId) && !processedStageStarts.Contains(taskStageId))
            {
                processedStageStarts.Add(taskStageId);
            }
        }

        public void MarkStageEndProcessed(string taskStageId)
        {
            if (!string.IsNullOrEmpty(taskStageId) && !processedStageEnds.Contains(taskStageId))
            {
                processedStageEnds.Add(taskStageId);
            }
        }

        public void Complete(int currentRound)
        {
            Status = TaskRuntimeStatus.Completed;
            CompletedRound = Math.Max(1, currentRound);
        }

        public void Fail(int currentRound)
        {
            Status = TaskRuntimeStatus.Failed;
            CompletedRound = Math.Max(1, currentRound);
        }
    }
}
