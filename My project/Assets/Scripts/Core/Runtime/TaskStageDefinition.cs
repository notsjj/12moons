using System;
using System.Collections.Generic;
using TwelveMoons.Core.Config;

namespace TwelveMoons.Core.Runtime
{
    public sealed class TaskStageDefinition
    {
        public TaskStageDefinition(ConfigRow row)
        {
            TaskStageId = row.GetString("TaskStageId");
            TaskId = row.GetString("TaskId");
            StageIndex = row.GetInt("StageIndex");
            StartOffsetRound = row.GetInt("StartOffsetRound");
            EndOffsetRound = row.GetInt("EndOffsetRound", StartOffsetRound);
            StartStoryId = row.GetString("StartStoryId");
            EndStoryId = row.GetString("EndStoryId");
            BeforeDocumentCharacterId = row.GetString("BeforeDocumentCharacterId");
            BeforeDocumentStoryId = row.GetString("BeforeDocumentStoryId");
            StartLetterId = row.GetString("StartLetterId");
            EndLetterId = row.GetString("EndLetterId");
            LinkedDocumentIds = SplitIds(row.GetString("LinkedDocumentIds"));
            StageDescription = row.GetString("StageDescription");
        }

        public string TaskStageId { get; }

        public string TaskId { get; }

        public int StageIndex { get; }

        public int StartOffsetRound { get; }

        public int EndOffsetRound { get; }

        public string StartStoryId { get; }

        public string EndStoryId { get; }

        public string BeforeDocumentCharacterId { get; }

        public string BeforeDocumentStoryId { get; }

        public string StartLetterId { get; }

        public string EndLetterId { get; }

        public IReadOnlyList<string> LinkedDocumentIds { get; }

        public string StageDescription { get; }

        private static IReadOnlyList<string> SplitIds(string rawValue)
        {
            var results = new List<string>();
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return results;
            }

            var ids = rawValue.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var id in ids)
            {
                var trimmed = id.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    results.Add(trimmed);
                }
            }

            return results;
        }
    }
}
