using System;
using System.Reflection;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class InventorySmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run Inventory Smoke Test")]
        public static void Run()
        {
            var testRoot = new GameObject("InventorySmokeTestRoot");

            try
            {
                var configManager = testRoot.AddComponent<ConfigManager>();
                SetPrivateField(configManager, "relativeConfigDirectory", "Configs/Demo");
                SetPrivateField(configManager, "loadOnAwake", false);
                configManager.BuildDefaultProviders();

                var runtimeDataService = testRoot.AddComponent<RuntimeDataService>();
                SetPrivateField(runtimeDataService, "configManager", configManager);
                runtimeDataService.CreateNewGame("disaster_flood_01");

                var inventoryService = testRoot.AddComponent<InventoryService>();
                SetPrivateField(inventoryService, "configManager", configManager);
                SetPrivateField(inventoryService, "runtimeDataService", runtimeDataService);
                InvokePrivate(inventoryService, "Awake");
                InvokePrivate(inventoryService, "Start");

                if (inventoryService.Definitions.Count != 5)
                {
                    throw new InvalidOperationException("InventoryService did not load all demo ItemConfig rows.");
                }

                if (!inventoryService.AddByType(InventoryItemType.Money, 20) ||
                    !inventoryService.AddByType(InventoryItemType.Material, 8) ||
                    !inventoryService.AddByType(InventoryItemType.Food, 6) ||
                    !inventoryService.AddItem("item_drainage_map", 1) ||
                    !inventoryService.AddItem("item_archivist_badge", 1))
                {
                    throw new InvalidOperationException("InventoryService failed to add configured items.");
                }

                if (!inventoryService.TryRemoveByType(InventoryItemType.Money, 5) ||
                    inventoryService.TryRemoveByType(InventoryItemType.Material, 99))
                {
                    throw new InvalidOperationException("InventoryService remove validation failed.");
                }

                if (inventoryService.GetCount("item_money") != 15 ||
                    inventoryService.GetCount("item_material") != 8 ||
                    inventoryService.GetCount("item_food") != 6 ||
                    inventoryService.GetCount("item_drainage_map") != 1 ||
                    inventoryService.GetCount("item_archivist_badge") != 1)
                {
                    throw new InvalidOperationException("InventoryService count check failed.");
                }

                ValidateInventoryCard(inventoryService, runtimeDataService, testRoot.transform);

                Debug.Log("Inventory smoke test passed. Money=15, Material=8, Food=6, TaskItem=1, CharacterItem=1, Cards=ok.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testRoot);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(target, null);
        }

        private static void ValidateInventoryCard(
            InventoryService inventoryService,
            RuntimeDataService runtimeDataService,
            Transform parent)
        {
            if (!inventoryService.TryGetDefinition("item_drainage_map", out var draggableDefinition) ||
                !inventoryService.TryGetDefinition("item_money", out var lockedDefinition))
            {
                throw new InvalidOperationException("InventoryService cannot find card test definitions.");
            }

            var draggableCard = CreateTestCard(parent);
            draggableCard.Bind(draggableDefinition, runtimeDataService.Data.GetOrCreateItem(draggableDefinition.ItemId));
            if (!draggableCard.CanDrag)
            {
                throw new InvalidOperationException("InventoryItemCard did not enable dragging for CanDrag=true item.");
            }

            var lockedCard = CreateTestCard(parent);
            lockedCard.Bind(lockedDefinition, runtimeDataService.Data.GetOrCreateItem(lockedDefinition.ItemId));
            if (!lockedCard.CanDrag)
            {
                throw new InvalidOperationException("InventoryItemCard should allow inventory card dragging for inspection.");
            }
        }

        private static InventoryItemCard CreateTestCard(Transform parent)
        {
            var cardObject = new GameObject("InventoryItemCardTest", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            cardObject.transform.SetParent(parent, false);

            var iconImage = CreateImage("IconImage", cardObject.transform);
            var nameText = CreateText("NameText", cardObject.transform);
            var countText = CreateText("CountText", cardObject.transform);
            var typeText = CreateText("TypeText", cardObject.transform);
            var descriptionText = CreateText("DescriptionText", cardObject.transform);

            var card = cardObject.AddComponent<InventoryItemCard>();
            card.Configure(iconImage, nameText, countText, typeText, descriptionText, cardObject.GetComponent<Image>());
            return card;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            return imageObject.GetComponent<Image>();
        }

        private static Text CreateText(string name, Transform parent)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }
    }
}
