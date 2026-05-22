using TwelveMoons.Core.Config;

namespace TwelveMoons.City
{
    public sealed class CityPointDefinition
    {
        public CityPointDefinition(ConfigRow row)
        {
            PointId = row.GetString("PointId");
            PointName = row.GetString("PointName");
            AreaId = row.GetString("AreaId");
            PointType = row.GetString("PointType");
            Description = row.GetString("Description");
            SortOrder = row.GetInt("SortOrder");
        }

        public string PointId { get; }

        public string PointName { get; }

        public string AreaId { get; }

        public string PointType { get; }

        public string Description { get; }

        public int SortOrder { get; }
    }
}
