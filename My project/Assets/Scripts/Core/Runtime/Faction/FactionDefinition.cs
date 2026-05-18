using TwelveMoons.Core.Config;

namespace TwelveMoons.Core.Runtime
{
    public sealed class FactionDefinition
    {
        public FactionDefinition(ConfigRow row)
        {
            FactionId = row.GetString("FactionId");
            FactionName = row.GetString("FactionName");
            InitSuspicion = row.GetInt("InitSuspicion");
            MaxSuspicion = row.GetInt("MaxSuspicion", 100);
            LowSuspicionThreshold = row.GetInt("LowSuspicionThreshold");
            LowSuspicionLetterId = row.GetString("LowSuspicionLetterId");
            HighSuspicionThreshold = row.GetInt("HighSuspicionThreshold", MaxSuspicion);
            PunishTaskId = row.GetString("PunishTaskId");
        }

        public string FactionId { get; }

        public string FactionName { get; }

        public int InitSuspicion { get; }

        public int MaxSuspicion { get; }

        public int LowSuspicionThreshold { get; }

        public string LowSuspicionLetterId { get; }

        public int HighSuspicionThreshold { get; }

        public string PunishTaskId { get; }
    }
}
