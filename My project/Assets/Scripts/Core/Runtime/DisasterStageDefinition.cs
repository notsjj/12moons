using TwelveMoons.Core.Config;

namespace TwelveMoons.Core.Runtime
{
    public sealed class DisasterStageDefinition
    {
        public DisasterStageDefinition(ConfigRow row)
        {
            DisasterId = row.GetString("DisasterId");
            StageId = row.GetString("StageId");
            StageName = row.GetString("StageName", StageId);
            StartRound = row.GetInt("StartRound", 1);
            EndRound = row.GetInt("EndRound", StartRound);
            Remark = row.GetString("Remark");
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
    }
}
