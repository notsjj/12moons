using System;

namespace TwelveMoons.Core.Runtime
{
    public enum InventoryItemType
    {
        Unknown,
        Money,
        Material,
        Food,
        TaskItem,
        Character
    }

    public static class InventoryItemTypeUtility
    {
        public static InventoryItemType Parse(string value)
        {
            return Enum.TryParse(value, true, out InventoryItemType parsed)
                ? parsed
                : InventoryItemType.Unknown;
        }
    }
}
