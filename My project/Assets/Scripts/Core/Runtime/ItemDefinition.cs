using TwelveMoons.Core.Config;

namespace TwelveMoons.Core.Runtime
{
    public sealed class ItemDefinition
    {
        public ItemDefinition(ConfigRow row)
        {
            ItemId = row.GetString("ItemId");
            ItemName = row.GetString("ItemName");
            ItemType = InventoryItemTypeUtility.Parse(row.GetString("ItemType"));
            Description = row.GetString("Description");
            IconId = row.GetString("IconId");
            CanDrag = row.GetBool("CanDrag");
            CanConsume = row.GetBool("CanConsume");
        }

        public string ItemId { get; }

        public string ItemName { get; }

        public InventoryItemType ItemType { get; }

        public string Description { get; }

        public string IconId { get; }

        public bool CanDrag { get; }

        public bool CanConsume { get; }
    }
}
