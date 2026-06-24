using System;
using System.Collections.Generic;
using UnityEngine;

namespace TwelveMoons.UI
{
    public static class StoryImageResourceProvider
    {
        private static readonly string[] DefaultRoots =
        {
            string.Empty,
            "Art/Art/Character",
            "Art/Art/Map"
        };

        private static readonly Dictionary<string, string[]> Aliases =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                { "公会长开心", new[] { "总会长开心" } },
                { "公会长惊讶", new[] { "总会长惊讶" } },
                { "公会长正常", new[] { "总会长正常" } },
                { "公会长正常惊讶", new[] { "总会长惊讶", "总会长电话惊讶" } },
                { "公会长生气", new[] { "总会长生气" } },
                { "工坊主正常", new[] { "某工坊主正常" } },
                { "公会长抓狂", new[] { "总会长抓狂" } },
                { "前台正常", new[] { "后勤正常" } },
                { "近侍", new[] { "近侍正常" } },
                { "王女", new[] { "王女正常" } },
                { "贫民正常", new[] { "后勤正常" } },
                { "贫民生气", new[] { "后勤正常" } },
                { "护教正常", new[] { "护教领军正常" } },
                { "护教军正常", new[] { "护教领军正常" } },
                { "店主正常", new[] { "商会长正常" } },
                { "大贵族", new[] { "大贵族正常" } },
                { "邪教徒正常", new[] { "落魄贵族正常" } },
                { "王室护卫正常", new[] { "卫队长正常" } },
                { "酿酒师正常", new[] { "商会长正常" } },
                { "大祭司战锤", new[] { "大祭司生气抬手" } },
                { "侍从正常", new[] { "学院人员正常" } },
                { "侍从搬东西", new[] { "学院人员正常" } },
                { "总会长丢电话", new[] { "总会长生气" } },
                { "总会长高兴", new[] { "总会长开心" } },
                { "王", new[] { "王正常" } },
                { "骷髅", new[] { "骷髅/骷髅思考" } },
                { "骷髅正常", new[] { "骷髅/骷髅思考" } },
                { "上城区", new[] { "王城" } },
                { "教区", new[] { "教区背景" } },
                { "学院", new[] { "学院背景", "学院街道" } }
            };

        private static readonly Dictionary<string, Sprite> CachedSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);

        public static Sprite LoadSprite(string resourceKey)
        {
            return TryLoadSprite(resourceKey, DefaultRoots, out var sprite) ? sprite : null;
        }

        public static bool TryLoadSprite(string resourceKey, string[] roots, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrWhiteSpace(resourceKey))
            {
                return false;
            }

            var cacheKey = BuildCacheKey(resourceKey, roots);
            if (CachedSprites.TryGetValue(cacheKey, out sprite))
            {
                return sprite != null;
            }

            foreach (var key in ExpandKeys(resourceKey))
            {
                foreach (var path in BuildCandidatePaths(key, roots))
                {
                    if (TryLoadSpriteAtPath(path, out sprite))
                    {
                        CachedSprites[cacheKey] = sprite;
                        return true;
                    }
                }
            }

            foreach (var key in ExpandKeys(resourceKey))
            {
                foreach (var root in roots)
                {
                    if (string.IsNullOrEmpty(root))
                    {
                        continue;
                    }

                    if (TryFindLoadedResourceByName(root, key, out sprite))
                    {
                        CachedSprites[cacheKey] = sprite;
                        return true;
                    }
                }
            }

            CachedSprites[cacheKey] = null;
            return false;
        }

        private static IEnumerable<string> ExpandKeys(string resourceKey)
        {
            var normalized = NormalizeKey(resourceKey);
            if (string.IsNullOrEmpty(normalized))
            {
                yield break;
            }

            var emitted = new HashSet<string>(StringComparer.Ordinal);
            foreach (var key in ExpandKeyVariants(normalized, emitted))
            {
                yield return key;
            }
        }

        private static IEnumerable<string> ExpandKeyVariants(string key, HashSet<string> emitted)
        {
            var normalized = NormalizeKey(key);
            if (string.IsNullOrEmpty(normalized) || !emitted.Add(normalized))
            {
                yield break;
            }

            yield return normalized;

            if (normalized.IndexOf("and", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var parts = normalized.Split(new[] { "and", "AND", "And" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    foreach (var nested in ExpandKeyVariants(part.Trim(), emitted))
                    {
                        yield return nested;
                    }
                }
            }

            if (Aliases.TryGetValue(normalized, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    foreach (var nested in ExpandKeyVariants(alias, emitted))
                    {
                        yield return nested;
                    }
                }
            }
        }

        private static IEnumerable<string> BuildCandidatePaths(string key, string[] roots)
        {
            if (key.Contains("/"))
            {
                yield return key;
            }

            foreach (var root in roots)
            {
                if (string.IsNullOrEmpty(root))
                {
                    yield return key;
                    continue;
                }

                if (key.StartsWith(root + "/", StringComparison.Ordinal))
                {
                    yield return key;
                }
                else
                {
                    yield return $"{root}/{key}";
                }
            }
        }

        private static bool TryLoadSpriteAtPath(string path, out Sprite sprite)
        {
            sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return true;
            }

            var sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0)
            {
                sprite = sprites[0];
                return true;
            }

            var texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                return false;
            }

            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = texture.name;
            return true;
        }

        private static bool TryFindLoadedResourceByName(string root, string key, out Sprite sprite)
        {
            var expectedName = GetResourceName(key);
            var sprites = Resources.LoadAll<Sprite>(root);
            foreach (var candidate in sprites)
            {
                if (candidate != null && string.Equals(candidate.name, expectedName, StringComparison.Ordinal))
                {
                    sprite = candidate;
                    return true;
                }
            }

            var textures = Resources.LoadAll<Texture2D>(root);
            foreach (var texture in textures)
            {
                if (texture == null || !string.Equals(texture.name, expectedName, StringComparison.Ordinal))
                {
                    continue;
                }

                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                sprite.name = texture.name;
                return true;
            }

            sprite = null;
            return false;
        }

        private static string NormalizeKey(string key)
        {
            var normalized = key.Trim().Replace("\\", "/");
            var extensionIndex = normalized.LastIndexOf('.');
            if (extensionIndex > normalized.LastIndexOf('/'))
            {
                normalized = normalized.Substring(0, extensionIndex);
            }

            return normalized;
        }

        private static string GetResourceName(string key)
        {
            var slashIndex = key.LastIndexOf('/');
            return slashIndex >= 0 ? key.Substring(slashIndex + 1) : key;
        }

        private static string BuildCacheKey(string resourceKey, string[] roots)
        {
            return $"{NormalizeKey(resourceKey)}::{string.Join("|", roots)}";
        }
    }
}
