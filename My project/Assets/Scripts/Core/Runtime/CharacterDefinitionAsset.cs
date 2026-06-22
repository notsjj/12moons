using UnityEngine;

namespace TwelveMoons.Core.Runtime
{
    [CreateAssetMenu(fileName = "CharacterDefinitionAsset", menuName = "Twelve Moons/GameData/Character Definition")]
    public sealed class CharacterDefinitionAsset : ScriptableObject
    {
        [Header("??????")]
        [Tooltip("???? ID???????? CharacterId?Plot ???????? plot_character_????")]
        [SerializeField] private string characterId;
        [Tooltip("??????????????????????????")]
        [SerializeField] private string characterName;
        [Tooltip("?????? ID???????????")]
        [SerializeField] private string factionId;
        [Tooltip("??????????? ID?Resources.Load ???????????")]
        [SerializeField] private string portraitId;
        [Tooltip("??????? Inspector ??????????")]
        [TextArea(2, 5)]
        [SerializeField] private string description;

        [Header("Plot ??????")]
        [Tooltip("Plot ????????????? SpeakerCharacterId????? Character ???????????")]
        [SerializeField] private string[] expressionPortraitIds = System.Array.Empty<string>();

        public string CharacterId => characterId;
        public string CharacterName => characterName;
        public string FactionId => factionId;
        public string PortraitId => portraitId;
        public string Description => description;
        public System.Collections.Generic.IReadOnlyList<string> ExpressionPortraitIds => expressionPortraitIds;

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
            expressionPortraitIds = System.Array.Empty<string>();
        }

        public void ApplyPlotCharacter(
            string id,
            string displayName,
            string defaultPortraitId,
            string[] expressionIds,
            string note)
        {
            characterId = id ?? string.Empty;
            characterName = displayName ?? string.Empty;
            factionId = string.Empty;
            portraitId = defaultPortraitId ?? string.Empty;
            description = note ?? string.Empty;
            expressionPortraitIds = expressionIds ?? System.Array.Empty<string>();
        }
    }
}
