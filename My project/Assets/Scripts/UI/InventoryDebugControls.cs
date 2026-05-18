using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class InventoryDebugControls : MonoBehaviour
    {
        [SerializeField] private InventoryService inventoryService;
        [SerializeField] private int moneyDelta = 10;
        [SerializeField] private int materialDelta = 5;
        [SerializeField] private int foodDelta = 3;

        private void Awake()
        {
            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<InventoryService>();
            }
        }

        public void AddMoney()
        {
            inventoryService?.AddMoney(moneyDelta);
        }

        public void RemoveMoney()
        {
            inventoryService?.RemoveMoney(moneyDelta);
        }

        public void AddMaterial()
        {
            inventoryService?.AddMaterial(materialDelta);
        }

        public void RemoveMaterial()
        {
            inventoryService?.RemoveMaterial(materialDelta);
        }

        public void AddFood()
        {
            inventoryService?.AddFood(foodDelta);
        }

        public void RemoveFood()
        {
            inventoryService?.RemoveFood(foodDelta);
        }
    }
}
