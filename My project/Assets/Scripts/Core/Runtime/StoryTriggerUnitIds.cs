using System;
using System.Linq;

namespace TwelveMoons.Core.Runtime
{
    public static class StoryTriggerUnitIds
    {
        public const string RoundStart = "\u516c\u6587\u524d";
        public const string ExploreBefore = "\u63a2\u7d22\u524d";
        public const string ExploreAfter = "\u63a2\u7d22\u540e";

        public static string GetDocumentSlot(int slotIndex)
        {
            return $"\u516c\u6587{Math.Max(1, slotIndex)}";
        }

        public static bool IsDocumentSlot(string triggerUnitId)
        {
            var value = (triggerUnitId ?? string.Empty).Trim();
            return value.StartsWith("\u516c\u6587", StringComparison.Ordinal) &&
                   !string.Equals(value, RoundStart, StringComparison.Ordinal);
        }

        public static bool IsCityPointTrigger(string triggerUnitId)
        {
            var value = (triggerUnitId ?? string.Empty).Trim();
            return value.StartsWith("P", StringComparison.OrdinalIgnoreCase) || value.Contains("|");
        }

        public static string ResolvePointId(string triggerUnitId, string storyId, int roundNumber)
        {
            var value = (triggerUnitId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var candidates = value.Split('|')
                .Select(candidate => candidate.Trim())
                .Where(candidate => !string.IsNullOrEmpty(candidate))
                .ToArray();
            if (candidates.Length == 0)
            {
                return string.Empty;
            }

            if (candidates.Length == 1)
            {
                return candidates[0];
            }

            var seed = Math.Abs(((storyId ?? string.Empty).GetHashCode() * 397) ^ roundNumber);
            return candidates[seed % candidates.Length];
        }
    }
}
