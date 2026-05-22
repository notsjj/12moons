using TwelveMoons.Core.Config;

namespace TwelveMoons.City
{
    public sealed class CityBuildingDefinition
    {
        public CityBuildingDefinition(ConfigRow row)
        {
            BuildingId = row.GetString("BuildingId");
            BuildingName = row.GetString("BuildingName");
            CityAreaId = row.GetString("CityAreaId");
            PointId = row.GetString("PointId");
            DefaultVisible = row.GetBool("DefaultVisible");
            BuildingEffectType = row.GetString("BuildingEffectType");
            ProduceItemId = row.GetString("ProduceItemId");
            ProduceCount = row.GetInt("ProduceCount");
            ReduceFactionId = row.GetString("ReduceFactionId");
            ReduceSuspicionValue = row.GetInt("ReduceSuspicionValue");
            CooldownRound = row.GetInt("CooldownRound", 1);
            Remark = row.GetString("Remark");
        }

        public string BuildingId { get; }

        public string BuildingName { get; }

        public string CityAreaId { get; }

        public string PointId { get; }

        public bool DefaultVisible { get; }

        public string BuildingEffectType { get; }

        public string ProduceItemId { get; }

        public int ProduceCount { get; }

        public string ReduceFactionId { get; }

        public int ReduceSuspicionValue { get; }

        public int CooldownRound { get; }

        public string Remark { get; }
    }
}
