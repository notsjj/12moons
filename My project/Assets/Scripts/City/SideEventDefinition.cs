using System;
using TwelveMoons.Core.Config;

namespace TwelveMoons.City
{
    public sealed class SideEventDefinition
    {
        public SideEventDefinition(ConfigRow row)
        {
            SideEventId = row.GetString("SideEventId");
            Round = Math.Max(1, row.GetInt("Round", 1));
            CityAreaId = row.GetString("CityAreaId");
            PointId = row.GetString("PointId");
            DisplayCharacterId = row.GetString("DisplayCharacterId");
            StoryId = row.GetString("StoryId");
            ExpireRound = Math.Max(0, row.GetInt("ExpireRound"));
            IsOneTime = row.GetBool("IsOneTime", true);
            RequiredTaskId = row.GetString("RequiredTaskId");
            RequiredTaskState = row.GetString("RequiredTaskState");
            RequiredItemId = row.GetString("RequiredItemId");
            RequiredItemCount = Math.Max(0, row.GetInt("RequiredItemCount"));
            Remark = row.GetString("Remark");
        }

        public string SideEventId { get; }

        public int Round { get; }

        public string CityAreaId { get; }

        public string PointId { get; }

        public string DisplayCharacterId { get; }

        public string StoryId { get; }

        public int ExpireRound { get; }

        public bool IsOneTime { get; }

        public string RequiredTaskId { get; }

        public string RequiredTaskState { get; }

        public string RequiredItemId { get; }

        public int RequiredItemCount { get; }

        public string Remark { get; }
    }
}
