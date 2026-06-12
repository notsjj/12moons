using UnityEngine;
using System.Collections.Generic;

namespace TwelveMoons.UI
{
    public static class CharacterPlaceholderPortraitProvider
    {
        private const string PlaceholderResource = "Art/Art/Character/IMG_0940";
        private static readonly Dictionary<string, Sprite> CachedSprites = new Dictionary<string, Sprite>();
        private static Sprite fallbackSprite;

        public static Sprite LoadPortrait(string portraitIdOrCharacterId)
        {
            if (!string.IsNullOrWhiteSpace(portraitIdOrCharacterId) &&
                TryLoadSprite(portraitIdOrCharacterId, out var portrait))
            {
                return portrait;
            }

            if (fallbackSprite != null)
            {
                return fallbackSprite;
            }

            if (TryLoadSprite(PlaceholderResource, out fallbackSprite))
            {
                return fallbackSprite;
            }

            return null;
        }

        private static bool TryLoadSprite(string resourceKey, out Sprite portrait)
        {
            if (CachedSprites.TryGetValue(resourceKey, out portrait))
            {
                return portrait != null;
            }

            var candidatePaths = BuildCandidatePaths(resourceKey);
            foreach (var candidatePath in candidatePaths)
            {
                portrait = Resources.Load<Sprite>(candidatePath);
                if (portrait == null)
                {
                    var sprites = Resources.LoadAll<Sprite>(candidatePath);
                    if (sprites != null && sprites.Length > 0)
                    {
                        portrait = sprites[0];
                    }
                }

                if (portrait == null)
                {
                    var texture = Resources.Load<Texture2D>(candidatePath);
                    if (texture != null)
                    {
                        portrait = Sprite.Create(
                            texture,
                            new Rect(0f, 0f, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f),
                            100f);
                        portrait.name = texture.name;
                    }
                }

                if (portrait != null)
                {
                    CachedSprites[resourceKey] = portrait;
                    return true;
                }
            }

            CachedSprites[resourceKey] = null;
            portrait = null;
            return false;
        }

        private static string[] BuildCandidatePaths(string resourceKey)
        {
            if (resourceKey.Contains("/"))
            {
                return new[] { resourceKey };
            }

            return new[]
            {
                resourceKey,
                $"Art/Art/Character/{resourceKey}"
            };
        }
    }
}
