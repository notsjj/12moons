using System;

namespace TwelveMoons.Core.Runtime
{
    [Serializable]
    public sealed class RuntimeSideEventState
    {
        public RuntimeSideEventState(string sideEventId)
        {
            SideEventId = sideEventId ?? string.Empty;
            TriggeredRound = 0;
            TriggerCount = 0;
        }

        public string SideEventId { get; private set; }

        public int TriggeredRound { get; private set; }

        public int TriggerCount { get; private set; }

        public bool HasTriggered => TriggerCount > 0;

        public void RecordTriggered(int currentRound)
        {
            TriggeredRound = Math.Max(1, currentRound);
            TriggerCount++;
        }
    }
}
