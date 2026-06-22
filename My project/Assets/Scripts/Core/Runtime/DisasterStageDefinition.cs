using TwelveMoons.Core.Config;

namespace TwelveMoons.Core.Runtime
{
    public sealed class DisasterStageDefinition
    {
        public DisasterStageDefinition(ConfigRow row)
        {
            DisasterId = GetFirstString(row, "DisasterId", "灾难ID");
            StageId = GetFirstString(row, "DisasterStageId", "StageId", "灾难阶段ID");
            StageName = GetFirstString(row, "StageName", "阶段名称", StageId);
            StartRound = GetFirstInt(row, 1, "StartRound", "开始回合");
            EndRound = GetFirstInt(row, StartRound, "EndRound", "结束回合");
            Remark = GetFirstString(row, "Remark", "备注");
        }

        public string DisasterId { get; }

        public string StageId { get; }

        public string StageName { get; }

        public int StartRound { get; }

        public int EndRound { get; }

        public string Remark { get; }

        public bool Contains(string disasterId, int round)
        {
            var matchesDisaster = string.IsNullOrEmpty(DisasterId) || DisasterId == disasterId;
            return matchesDisaster && round >= StartRound && round <= EndRound;
        }

        private static string GetFirstString(ConfigRow row, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                var value = row.GetString(fieldName);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static int GetFirstInt(ConfigRow row, int defaultValue, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                if (row.TryGetString(fieldName, out var value) && int.TryParse(value, out var parsed))
                {
                    return parsed;
                }
            }

            return defaultValue;
        }
    }
}
