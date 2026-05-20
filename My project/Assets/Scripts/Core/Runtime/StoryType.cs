using System;

namespace TwelveMoons.Core.Runtime
{
    public enum StoryType
    {
        Dialogue,
        Image,
        Text
    }

    public static class StoryTypeUtility
    {
        public static StoryType Parse(string value)
        {
            if (Enum.TryParse(value, true, out StoryType parsed))
            {
                return parsed;
            }

            return StoryType.Text;
        }
    }
}
