using TwelveMoons.Core.Config;
using System.Collections.Generic;

namespace TwelveMoons.Core.Runtime
{
    public sealed class StoryDefinition
    {
        public StoryDefinition(ConfigRow row)
        {
            StoryId = row.GetString("StoryId");
            StoryName = row.GetString("StoryName");
            StoryType = StoryTypeUtility.Parse(row.GetString("StoryType"));
            StoryContentAssetId = row.GetString("StoryContentAssetId");
            ImageId = row.GetString("ImageId");
            ImageDisplayMode = StoryImageDisplayModeUtility.Parse(row.GetString("ImageDisplayMode"));
            ImageIds = Split(row.GetString("ImageIds"));
            if (ImageIds.Count == 0 && !string.IsNullOrEmpty(ImageId))
            {
                ImageIds.Add(ImageId);
            }

            ImageCaptions = Split(row.GetString("ImageCaptions"));
            TextContent = row.GetString("TextContent");
            BackgroundImageId = row.GetString("\u80cc\u666f\u56fe\u7247", row.GetString("BackgroundImageId"));
            TriggerUnitId = row.GetString("\u89e6\u53d1\u5355\u4f4did", row.GetString("TriggerUnitId"));
            RoundNumber = row.GetInt("\u56de\u5408\u6570", row.GetInt("RoundNumber"));
            TextSegments = Split(TextContent);
            if (TextSegments.Count == 0 && !string.IsNullOrEmpty(TextContent))
            {
                TextSegments.Add(TextContent);
            }

            TriggerTaskOnEnd = row.GetBool("TriggerTaskOnEnd");
            TriggerTaskId = row.GetString("TriggerTaskId");
            AddItemId = row.GetString("AddItemId");
            AddItemCount = row.GetInt("AddItemCount");
        }

        public string StoryId { get; }

        public string StoryName { get; }

        public StoryType StoryType { get; }

        public string StoryContentAssetId { get; }

        public string ImageId { get; }

        public StoryImageDisplayMode ImageDisplayMode { get; }

        public List<string> ImageIds { get; }

        public List<string> ImageCaptions { get; }

        public string TextContent { get; }

        public string BackgroundImageId { get; }

        public string TriggerUnitId { get; }

        public int RoundNumber { get; }

        public List<string> TextSegments { get; }

        public bool TriggerTaskOnEnd { get; }

        public string TriggerTaskId { get; }

        public string AddItemId { get; }

        public int AddItemCount { get; }

        public string GetImageCaption(int index)
        {
            if (index >= 0 && index < ImageCaptions.Count)
            {
                return ImageCaptions[index];
            }

            return TextContent;
        }

        private static List<string> Split(string value)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(value))
            {
                return result;
            }

            var parts = value.Split('|');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    result.Add(trimmed);
                }
            }

            return result;
        }
    }
}
