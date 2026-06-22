using System;

namespace TwelveMoons.Core.Runtime
{
    public static class CharacterDisplayNameUtility
    {
        private static readonly string[] ExpressionSuffixes =
        {
            "\u6b63\u5e38\u60ca\u8bb6",
            "\u62ac\u7535\u8bdd\u60ca\u8bb6",
            "\u7535\u8bdd\u60ca\u8bb6",
            "\u7535\u8bdd\u751f\u6c14",
            "\u8d54\u7f6a\u6253\u62db\u547c",
            "\u6487\u5634\u601d\u8003",
            "\u6c34\u6876\u65e0\u8bed",
            "\u751f\u6c14\u62ac\u624b",
            "\u7741\u773c\u6b63\u5e38",
            "\u4e22\u7535\u8bdd",
            "\u6b63\u5e38",
            "\u95ed\u773c",
            "\u5931\u843d",
            "\u4e25\u8083",
            "\u5c2c\u7b11",
            "\u5c34\u5c2c",
            "\u62ac\u624b",
            "\u4ecb\u7ecd",
            "\u60ca\u8bb6",
            "\u751f\u6c14",
            "\u5f00\u5fc3",
            "\u9ad8\u5174",
            "\u6293\u72c2",
            "\u4e3a\u96be",
            "\u6389\u80e1\u5b50",
            "\u8c04\u5a9a",
            "\u8fa9\u8bba",
            "\u5927\u60ca",
            "\u75af\u766b",
            "\u7591\u60d1",
            "\u601d\u8003",
            "\u6b7b\u673a",
            "\u96be\u7ef7",
            "\u5978\u7b11",
            "\u65e0\u8bed",
            "\u6c34\u6876",
            "\u8d54\u7f6a",
            "\u60ca",
            "\u75bc",
            "\u7f50"
        };

        public static string GetDisplayName(string speakerCharacterId)
        {
            var normalized = (speakerCharacterId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                return string.Empty;
            }

            foreach (var suffix in ExpressionSuffixes)
            {
                if (normalized.EndsWith(suffix, StringComparison.Ordinal) &&
                    normalized.Length > suffix.Length)
                {
                    return normalized.Substring(0, normalized.Length - suffix.Length);
                }
            }

            return normalized;
        }
    }
}
