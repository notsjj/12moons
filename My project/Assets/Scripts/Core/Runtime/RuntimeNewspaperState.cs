using System;
using System.Collections.Generic;

namespace TwelveMoons.Core.Runtime
{
    [Serializable]
    public sealed class RuntimeNewspaperState
    {
        private readonly List<string> entries = new List<string>();

        public RuntimeNewspaperState(int round)
        {
            Round = Math.Max(1, round);
        }

        public int Round { get; }

        public IReadOnlyList<string> Entries => entries;

        public string Title => $"第 {Round} 回合报纸";

        public void AddEntry(string entry)
        {
            if (!string.IsNullOrWhiteSpace(entry))
            {
                entries.Add(entry.Trim());
            }
        }

        public string BuildBodyText()
        {
            if (entries.Count == 0)
            {
                return "本回合暂无可刊登事项。";
            }

            return string.Join("\n", entries);
        }
    }
}
