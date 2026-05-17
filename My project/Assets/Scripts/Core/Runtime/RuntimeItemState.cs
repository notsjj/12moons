using System;

namespace TwelveMoons.Core.Runtime
{
    [Serializable]
    public sealed class RuntimeItemState
    {
        public RuntimeItemState(string itemId, int count = 0)
        {
            ItemId = itemId;
            Count = Math.Max(0, count);
        }

        public string ItemId { get; private set; }

        public int Count { get; private set; }

        public void SetCount(int count)
        {
            Count = Math.Max(0, count);
        }

        public void AddCount(int delta)
        {
            SetCount(Count + delta);
        }
    }
}
