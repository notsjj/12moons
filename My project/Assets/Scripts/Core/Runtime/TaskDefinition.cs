using TwelveMoons.Core.Config;

namespace TwelveMoons.Core.Runtime
{
    public sealed class TaskDefinition
    {
        public TaskDefinition(ConfigRow row)
        {
            TaskId = row.GetString("TaskId");
            TaskName = row.GetString("TaskName");
            TaskType = row.GetString("TaskType");
            Description = row.GetString("Description");
            StartRound = row.GetInt("StartRound");
            EndRound = row.GetInt("EndRound");
            SuccessScore = row.GetInt("SuccessScore");
            FailScore = row.GetInt("FailScore");
            SuccessResultText = row.GetString("SuccessResultText");
            FailResultText = row.GetString("FailResultText");
            ShowInTaskPanel = row.GetBool("ShowInTaskPanel", true);
        }

        public string TaskId { get; }

        public string TaskName { get; }

        public string TaskType { get; }

        public string Description { get; }

        public int StartRound { get; }

        public int EndRound { get; }

        public int SuccessScore { get; }

        public int FailScore { get; }

        public string SuccessResultText { get; }

        public string FailResultText { get; }

        public bool ShowInTaskPanel { get; }
    }
}
