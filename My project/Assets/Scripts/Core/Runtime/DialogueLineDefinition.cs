using System;
using System.Collections.Generic;
using TwelveMoons.Core.Config;

namespace TwelveMoons.Core.Runtime
{
    public sealed class DialogueLineDefinition
    {
        public DialogueLineDefinition(ConfigRow row)
        {
            LineId = row.GetString("LineId");
            StoryId = row.GetString("StoryId");
            NextLineIds = Split(row.GetString("NextLineId"));
            SpeakerCharacterId = row.GetString("SpeakerCharacterId");
            Contents = Split(row.GetString("Content"));
            Position = row.GetInt("Position");
            IsChoice = row.GetBool("IsChoice");
            RequiredItemIds = Split(row.GetString("RequiredItemIds"));
            RequiredItemCounts = SplitInts(row.GetString("RequiredItemCounts"));
            ConsumeItems = SplitBools(row.GetString("ConsumeItems"));
            AddItemIds = Split(row.GetString("AddItemIds"));
            AddItemCounts = SplitInts(row.GetString("AddItemCounts"));
            PresentationCue = row.GetString("演出", row.GetString("PresentationCue"));
        }

        public string LineId { get; }

        public string StoryId { get; }

        public IReadOnlyList<string> NextLineIds { get; }

        public string SpeakerCharacterId { get; }

        public IReadOnlyList<string> Contents { get; }

        public int Position { get; }

        public bool IsChoice { get; }

        public IReadOnlyList<string> RequiredItemIds { get; }

        public IReadOnlyList<int> RequiredItemCounts { get; }

        public IReadOnlyList<bool> ConsumeItems { get; }

        public IReadOnlyList<string> AddItemIds { get; }

        public IReadOnlyList<int> AddItemCounts { get; }

        public string BackgroundImageId { get; }

        public string PresentationCue { get; }

        public string Content => Contents.Count > 0 ? Contents[0] : string.Empty;

        public string GetChoiceText(int optionIndex)
        {
            return optionIndex >= 0 && optionIndex < Contents.Count ? Contents[optionIndex] : string.Empty;
        }

        public string GetNextLineId(int optionIndex)
        {
            return optionIndex >= 0 && optionIndex < NextLineIds.Count ? NextLineIds[optionIndex] : string.Empty;
        }

        public string GetRequiredItemId(int optionIndex)
        {
            return optionIndex >= 0 && optionIndex < RequiredItemIds.Count ? RequiredItemIds[optionIndex] : string.Empty;
        }

        public int GetRequiredItemCount(int optionIndex)
        {
            return optionIndex >= 0 && optionIndex < RequiredItemCounts.Count ? RequiredItemCounts[optionIndex] : 0;
        }

        public bool ShouldConsumeItem(int optionIndex)
        {
            return optionIndex >= 0 && optionIndex < ConsumeItems.Count && ConsumeItems[optionIndex];
        }

        public string GetAddItemId(int optionIndex)
        {
            return optionIndex >= 0 && optionIndex < AddItemIds.Count ? AddItemIds[optionIndex] : string.Empty;
        }

        public int GetAddItemCount(int optionIndex)
        {
            return optionIndex >= 0 && optionIndex < AddItemCounts.Count ? AddItemCounts[optionIndex] : 0;
        }

        public bool IsItemSubmissionLine()
        {
            return !IsChoice && HasRequiredItems();
        }

        public bool HasRequiredItems()
        {
            for (var index = 0; index < RequiredItemIds.Count; index++)
            {
                if (!string.IsNullOrEmpty(RequiredItemIds[index]) && GetRequiredItemCount(index) > 0)
                {
                    return true;
                }
            }

            return false;
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
                result.Add(part.Trim());
            }

            return result;
        }

        private static List<int> SplitInts(string value)
        {
            var result = new List<int>();
            foreach (var part in Split(value))
            {
                result.Add(int.TryParse(part, out var parsed) ? parsed : 0);
            }

            return result;
        }

        private static List<bool> SplitBools(string value)
        {
            var result = new List<bool>();
            foreach (var part in Split(value))
            {
                if (bool.TryParse(part, out var parsed))
                {
                    result.Add(parsed);
                }
                else if (int.TryParse(part, out var numericValue))
                {
                    result.Add(numericValue != 0);
                }
                else
                {
                    result.Add(false);
                }
            }

            return result;
        }
    }
}

