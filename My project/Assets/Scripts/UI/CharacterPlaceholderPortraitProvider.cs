using UnityEngine;
using TwelveMoons.Core.Runtime;

namespace TwelveMoons.UI
{
    public static class CharacterPlaceholderPortraitProvider
    {
        private const string PlaceholderResource = "Art/Art/Character/后勤正常";
        private static readonly string[] CharacterRoots =
        {
            string.Empty,
            "Art/Art/Character"
        };

        private static Sprite fallbackSprite;

        public static Sprite LoadPortrait(string portraitIdOrCharacterId)
        {
            if (!string.IsNullOrWhiteSpace(portraitIdOrCharacterId))
            {
                var definition = Resources.Load<CharacterDefinitionAsset>($"GameData/Characters/{portraitIdOrCharacterId}");
                if (definition != null &&
                    !string.IsNullOrWhiteSpace(definition.PortraitId) &&
                    StoryImageResourceProvider.TryLoadSprite(definition.PortraitId, CharacterRoots, out var portraitFromDefinition))
                {
                    return portraitFromDefinition;
                }
            }

            if (!string.IsNullOrWhiteSpace(portraitIdOrCharacterId) &&
                StoryImageResourceProvider.TryLoadSprite(portraitIdOrCharacterId, CharacterRoots, out var portrait))
            {
                return portrait;
            }

            if (fallbackSprite != null)
            {
                return fallbackSprite;
            }

            if (StoryImageResourceProvider.TryLoadSprite(PlaceholderResource, CharacterRoots, out fallbackSprite))
            {
                return fallbackSprite;
            }

            return null;
        }
    }
}
