using System;

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

        public string TaskId { get; private set; }

        public TaskRuntimeStatus Status { get; private set; }

        public int ActivatedRound { get; private set; }

        public int CompletedRound { get; private set; }

        public int Score { get; private set; }

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
