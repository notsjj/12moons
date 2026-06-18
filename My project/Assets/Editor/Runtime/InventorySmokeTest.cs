using System;
using System.Reflection;
using TMPro;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
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
                ValidateInitialBackpackItems(runtimeDataService);

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
                ValidateInventoryPanelLayout(inventoryService, runtimeDataService, testRoot.transform);

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

        private static void ValidateInitialBackpackItems(RuntimeDataService runtimeDataService)
        {
            AssertInitialItem(runtimeDataService, "item_money");
            AssertInitialItem(runtimeDataService, "item_material");
            AssertInitialItem(runtimeDataService, "item_food");
            AssertInitialItem(runtimeDataService, "item_drainage_map");
            AssertInitialItem(runtimeDataService, "item_archivist_badge");

            foreach (var item in runtimeDataService.Data.Items)
            {
                item.SetCount(0);
            }
        }

        private static void AssertInitialItem(RuntimeDataService runtimeDataService, string itemId)
        {
            var item = runtimeDataService.Data.GetOrCreateItem(itemId);
            if (item.Count <= 0)
            {
                throw new InvalidOperationException($"New game should start with a visible backpack card for {itemId}.");
            }
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

        private static TMP_Text CreateText(string name, Transform parent)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            return textObject.GetComponent<TextMeshProUGUI>();
        }

        private static void ValidateInventoryPanelLayout(
            InventoryService inventoryService,
            RuntimeDataService runtimeDataService,
            Transform parent)
        {
            var panelObject = new GameObject("InventoryPanelTest", typeof(RectTransform));
            panelObject.transform.SetParent(parent, false);
            var contentObject = new GameObject("InventoryContentTest", typeof(RectTransform));
            contentObject.transform.SetParent(panelObject.transform, false);

            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(600f, 260f);
            var contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(600f, 260f);

            var panel = panelObject.AddComponent<InventoryPanelView>();
            SetPrivateField(panel, "inventoryService", inventoryService);
            SetPrivateField(panel, "runtimeDataService", runtimeDataService);
            SetPrivateField(panel, "contentRoot", contentRect);
            SetPrivateField(panel, "showZeroCountItems", false);
            SetPrivateField(panel, "cardSize", new Vector2(180f, 220f));
            SetPrivateField(panel, "minimumVisibleStep", 42f);

            foreach (var item in runtimeDataService.Data.Items)
            {
                item.SetCount(0);
            }

            panel.Refresh();
            if (contentObject.transform.childCount != 0)
            {
                throw new InvalidOperationException("InventoryPanelView should hide zero-count items.");
            }

            runtimeDataService.AddItem("item_money", 1);
            panel.Refresh();
            if (contentObject.transform.childCount != 1)
            {
                throw new InvalidOperationException("InventoryPanelView should show one card after receiving one item.");
            }

            var firstCard = contentObject.transform.GetChild(0) as RectTransform;
            var expectedCenterX = (contentRect.sizeDelta.x - 180f) * 0.5f;
            if (Mathf.Abs(firstCard.anchoredPosition.x - expectedCenterX) > 0.5f)
            {
                throw new InvalidOperationException("Single inventory card should be centered in content area.");
            }

            runtimeDataService.AddItem("item_material", 1);
            runtimeDataService.AddItem("item_food", 1);
            runtimeDataService.AddItem("item_drainage_map", 1);
            runtimeDataService.AddItem("item_archivist_badge", 1);
            panel.Refresh();
            if (contentObject.transform.childCount != 5)
            {
                throw new InvalidOperationException("InventoryPanelView should show five received item cards.");
            }

            var left = ((RectTransform)contentObject.transform.GetChild(0)).anchoredPosition.x;
            var rightCard = (RectTransform)contentObject.transform.GetChild(contentObject.transform.childCount - 1);
            var right = rightCard.anchoredPosition.x + rightCard.sizeDelta.x;
            if (left < -0.5f || right > contentRect.sizeDelta.x + 0.5f)
            {
                throw new InvalidOperationException("InventoryPanelView cards should stay inside content area.");
            }

            ValidateInventoryPanelKeepsCardPrefabSize(inventoryService, runtimeDataService, parent);
            ValidateInventoryPanelRestoresRaycastOnShow(parent);
            ValidateInventoryPanelKeepsFrameAboveCards(inventoryService, runtimeDataService, parent);
            ValidateInventoryCardDragKeepsAlpha(inventoryService, parent);
        }

        private static void ValidateInventoryPanelKeepsFrameAboveCards(
            InventoryService inventoryService,
            RuntimeDataService runtimeDataService,
            Transform parent)
        {
            var panelObject = new GameObject("InventoryPanelFrameLayerTest", typeof(RectTransform));
            panelObject.transform.SetParent(parent, false);
            var contentObject = new GameObject("InventoryContentFrameLayerTest", typeof(RectTransform));
            contentObject.transform.SetParent(panelObject.transform, false);
            var frameObject = new GameObject("物品栏", typeof(RectTransform), typeof(Image));
            frameObject.transform.SetParent(panelObject.transform, false);
            frameObject.GetComponent<Image>().raycastTarget = false;

            var contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(600f, 260f);

            var panel = panelObject.AddComponent<InventoryPanelView>();
            SetPrivateField(panel, "inventoryService", inventoryService);
            SetPrivateField(panel, "runtimeDataService", runtimeDataService);
            SetPrivateField(panel, "contentRoot", contentRect);
            SetPrivateField(panel, "showZeroCountItems", false);

            foreach (var item in runtimeDataService.Data.Items)
            {
                item.SetCount(0);
            }

            runtimeDataService.AddItem("item_money", 1);
            InvokePrivate(panel, "Awake");
            panel.Refresh();

            if (frameObject.transform.GetSiblingIndex() != panelObject.transform.childCount - 1)
            {
                throw new InvalidOperationException("InventoryPanelView must keep the item bar frame above card content while leaving the frame raycast-disabled.");
            }

            if (frameObject.GetComponent<Image>().raycastTarget)
            {
                throw new InvalidOperationException("InventoryPanelView foreground item bar frame must not block card clicks.");
            }
        }

        private static void ValidateInventoryCardDragKeepsAlpha(InventoryService inventoryService, Transform parent)
        {
            if (!inventoryService.TryGetDefinition("item_drainage_map", out var draggableDefinition))
            {
                throw new InvalidOperationException("InventoryItemCard drag test cannot find a draggable demo item.");
            }

            var canvasObject = new GameObject("InventoryCardDragCanvasTest", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(parent, false);
            var card = CreateTestCard(canvasObject.transform);
            var canvasGroup = card.GetComponent<CanvasGroup>();

            if (!canvasGroup)
            {
                throw new InvalidOperationException("InventoryItemCard drag test requires a CanvasGroup.");
            }

            card.Bind(draggableDefinition, new RuntimeItemState(draggableDefinition.ItemId, 1));
            card.OnBeginDrag(new PointerEventData(EventSystem.current));

            if (canvasGroup.alpha < 0.99f)
            {
                throw new InvalidOperationException("InventoryItemCard must not lower card transparency while dragging.");
            }

            card.OnEndDrag(new PointerEventData(EventSystem.current));
        }

        private static void ValidateInventoryPanelKeepsCardPrefabSize(
            InventoryService inventoryService,
            RuntimeDataService runtimeDataService,
            Transform parent)
        {
            var panelObject = new GameObject("InventoryPanelSizeTest", typeof(RectTransform));
            panelObject.transform.SetParent(parent, false);
            var contentObject = new GameObject("InventoryContentSizeTest", typeof(RectTransform));
            contentObject.transform.SetParent(panelObject.transform, false);

            var contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(600f, 260f);
            var prefab = CreateTestCard(parent);
            var prefabRect = prefab.transform as RectTransform;
            prefabRect.sizeDelta = new Vector2(132f, 196f);

            var panel = panelObject.AddComponent<InventoryPanelView>();
            SetPrivateField(panel, "inventoryService", inventoryService);
            SetPrivateField(panel, "runtimeDataService", runtimeDataService);
            SetPrivateField(panel, "contentRoot", contentRect);
            SetPrivateField(panel, "cardPrefab", prefab);
            SetPrivateField(panel, "showZeroCountItems", false);
            SetPrivateField(panel, "cardSize", new Vector2(180f, 220f));
            SetPrivateField(panel, "minimumVisibleStep", 42f);

            foreach (var item in runtimeDataService.Data.Items)
            {
                item.SetCount(0);
            }

            runtimeDataService.AddItem("item_money", 1);
            InvokePrivate(panel, "Awake");
            panel.Refresh();

            var cardRect = contentObject.transform.GetChild(0) as RectTransform;
            if (cardRect == null ||
                !Mathf.Approximately(cardRect.sizeDelta.x, 132f) ||
                !Mathf.Approximately(cardRect.sizeDelta.y, 196f))
            {
                throw new InvalidOperationException("InventoryPanelView must keep the item card prefab's original size.");
            }
        }

        private static void ValidateInventoryPanelRestoresRaycastOnShow(Transform parent)
        {
            var panelObject = new GameObject("InventoryPanelRaycastTest", typeof(RectTransform), typeof(CanvasGroup));
            panelObject.transform.SetParent(parent, false);
            var canvasGroup = panelObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0.25f;

            var panel = panelObject.AddComponent<InventoryPanelView>();
            InvokePrivate(panel, "Awake");
            panel.ShowForDocumentSubmission();

            if (!canvasGroup.blocksRaycasts || !canvasGroup.interactable || canvasGroup.alpha < 0.99f)
            {
                throw new InvalidOperationException("InventoryPanelView must restore CanvasGroup interaction when shown for document submission.");
            }
        }
    }
}
