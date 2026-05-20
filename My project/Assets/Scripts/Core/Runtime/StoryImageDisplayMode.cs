using System;

namespace TwelveMoons.Core.Runtime
{
    public enum StoryImageDisplayMode
    {
        ComicPanels,
        PageSequence
    }

    public static class StoryImageDisplayModeUtility
    {
        public static StoryImageDisplayMode Parse(string value)
        {
            if (Enum.TryParse(value, true, out StoryImageDisplayMode parsed))
            {
                return parsed;
            }

            return StoryImageDisplayMode.PageSequence;
        }
    }
}
