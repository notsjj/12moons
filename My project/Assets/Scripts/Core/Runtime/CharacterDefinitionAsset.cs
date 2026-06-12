using UnityEngine;

namespace TwelveMoons.Core.Runtime
{
    [CreateAssetMenu(fileName = "CharacterDefinitionAsset", menuName = "Twelve Moons/GameData/Character Definition")]
    public sealed class CharacterDefinitionAsset : ScriptableObject
    {
        [Header("角色基础信息")]
        [SerializeField] private string characterId;
        [SerializeField] private string characterName;
        [SerializeField] private string factionId;
        [SerializeField] private string portraitId;
        [TextArea(2, 5)]
        [SerializeField] private string description;

        public string CharacterId => characterId;
        public string CharacterName => characterName;
        public string FactionId => factionId;
        public string PortraitId => portraitId;
        public string Description => description;

        public void Apply(CharacterDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            characterId = definition.CharacterId;
            characterName = definition.CharacterName;
            factionId = definition.FactionId;
            portraitId = definition.PortraitId;
            description = definition.Description;
        }
    }
}
