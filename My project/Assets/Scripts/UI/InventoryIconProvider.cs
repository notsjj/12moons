using System.Collections.Generic;
using UnityEngine;

namespace TwelveMoons.UI
{
    public static class InventoryIconProvider
    {
        private const string IconResourceRoot = "Icons/";
        private static readonly Dictionary<string, Sprite> fallbackSprites = new Dictionary<string, Sprite>();

        public static Sprite LoadIcon(string iconId)
        {
            if (!string.IsNullOrEmpty(iconId))
            {
                var configuredSprite = Resources.Load<Sprite>(IconResourceRoot + iconId);
                if (configuredSprite != null)
                {
                    return configuredSprite;
                }
            }

            return GetFallbackSprite(string.IsNullOrEmpty(iconId) ? "missing_icon" : iconId);
        }

        private static Sprite GetFallbackSprite(string key)
        {
            if (fallbackSprites.TryGetValue(key, out var sprite))
            {
                return sprite;
            }

            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            {
                name = key + "_placeholder"
            };

            var baseColor = ColorFromKey(key);
            var borderColor = new Color(0.95f, 0.95f, 0.85f, 1f);
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var isBorder = x < 2 || y < 2 || x >= texture.width - 2 || y >= texture.height - 2;
                    texture.SetPixel(x, y, isBorder ? borderColor : baseColor);
                }
            }

            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            sprite.name = key + "_placeholder";
            fallbackSprites[key] = sprite;
            return sprite;
        }

        private static Color ColorFromKey(string key)
        {
            var hash = key.GetHashCode();
            var r = 0.25f + ((hash & 0xFF) / 255f) * 0.45f;
            var g = 0.25f + (((hash >> 8) & 0xFF) / 255f) * 0.45f;
            var b = 0.25f + (((hash >> 16) & 0xFF) / 255f) * 0.45f;
            return new Color(r, g, b, 1f);
        }
    }
}
