using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class InventoryItemRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text typeText;

        public void Configure(TMP_Text itemNameText, TMP_Text itemCountText, TMP_Text itemTypeText = null)
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
