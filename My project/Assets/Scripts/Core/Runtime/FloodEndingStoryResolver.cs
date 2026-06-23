using System.Linq;

namespace TwelveMoons.Core.Runtime
{
    public static class FloodEndingStoryResolver
    {
        public const string DeanEndingStoryId = "S0028";
        public const string GuardCaptainEndingStoryId = "S0029";
        public const string BishopEndingStoryId = "S0030";
        public const string HighPriestEndingStoryId = "S0031";
        public const string ReservoirEndingStoryId = "S0032";

        public static string ResolveStoryId(GameRuntimeData data)
        {
            if (data == null)
            {
                return ReservoirEndingStoryId;
            }

            if (IsTaskCompleted(data, "T0007"))
            {
                return GuardCaptainEndingStoryId;
            }

            if (IsTaskCompleted(data, "T0006"))
            {
                return HighPriestEndingStoryId;
            }

            if (IsBuildingUnlocked(data, "B0004"))
            {
                return DeanEndingStoryId;
            }

            if (IsBuildingUnlocked(data, "B0005") || IsBuildingUnlocked(data, "B0007"))
            {
                return BishopEndingStoryId;
            }

            return ReservoirEndingStoryId;
        }

        public static bool IsFloodEndingStoryId(string storyId)
        {
            return storyId == DeanEndingStoryId ||
                   storyId == GuardCaptainEndingStoryId ||
                   storyId == BishopEndingStoryId ||
                   storyId == HighPriestEndingStoryId ||
                   storyId == ReservoirEndingStoryId;
        }

        private static bool IsTaskCompleted(GameRuntimeData data, string taskId)
        {
            return data.Tasks.Any(candidate =>
                candidate.TaskId == taskId && candidate.Status == TaskRuntimeStatus.Completed);
        }

        private static bool IsBuildingUnlocked(GameRuntimeData data, string buildingId)
        {
            return data.Buildings.Any(candidate =>
                candidate.BuildingId == buildingId && candidate.IsUnlocked);
        }
    }
}
