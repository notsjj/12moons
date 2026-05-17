using System;

namespace TwelveMoons.Core.Runtime
{
    [Serializable]
    public sealed class RuntimeBuildingState
    {
        public RuntimeBuildingState(string buildingId, bool isUnlocked = false)
        {
            BuildingId = buildingId;
            IsUnlocked = isUnlocked;
            LastCollectedRound = 0;
        }

        public string BuildingId { get; private set; }

        public bool IsUnlocked { get; private set; }

        public int LastCollectedRound { get; private set; }

        public void Unlock()
        {
            IsUnlocked = true;
        }

        public void RecordCollected(int currentRound)
        {
            LastCollectedRound = Math.Max(1, currentRound);
        }
    }
}
