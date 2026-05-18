using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    public sealed class InventoryItemRow : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Text countText;
        [SerializeField] private Text typeText;

        public void Configure(Text itemNameText, Text itemCountText, Text itemTypeText = null)
        {
            nameText = itemNameText;
            countText = itemCountText;
            typeText = itemTypeText;
        }

        public void Bind(ItemDefinition definition, RuntimeItemState state)
        {
            if (nameText != null)
            {
                nameText.text = string.IsNullOrEmpty(definition.ItemName)
                    ? definition.ItemId
                    : definition.ItemName;
            }

            if (countText != null)
            {
                countText.text = state.Count.ToString();
            }

            if (typeText != null)
            {
                typeText.text = definition.ItemType.ToString();
            }
        }
    }
}
