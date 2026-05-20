using TwelveMoons.Core.Config;

namespace TwelveMoons.Core.Runtime
{
    public sealed class CharacterDefinition
    {
        public CharacterDefinition(ConfigRow row)
        {
            CharacterId = row.GetString("CharacterId");
            CharacterName = row.GetString("CharacterName");
            FactionId = row.GetString("FactionId");
            PortraitId = row.GetString("PortraitId");
            Description = row.GetString("Description");
        }

        public string CharacterId { get; }

        public string CharacterName { get; }

        public string FactionId { get; }

        public string PortraitId { get; }

        public string Description { get; }
    }
}
