using System.IO;
using TMPro;
using TwelveMoons.Core;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools
{
    public static class InventoryUiBuilder
    {
        private const string InventoryCardPrefabPath = "Assets/Prefabs/UI/物品卡片.prefab";

        [MenuItem("Twelve Moons/Setup/Create Inventory UI")]
        public static void CreateInventoryUi()
        {
            var canvas = FindOrCreateCanvas();
            FindOrCreateEventSystem();

            var gameEntry = FindOrCreateGameEntry();
            var configManager = EnsureComponent<ConfigManager>(gameEntry.gameObject);
            var runtimeDataService = EnsureComponent<RuntimeDataService>(gameEntry.gameObject);
            var inventoryService = EnsureComponent<InventoryService>(gameEntry.gameObject);

            ConfigureConfigManager(configManager);
            ConfigureRuntimeDataService(runtimeDataService, configManager);
            ConfigureInventoryService(inventoryService, configManager, runtimeDataService);

            var inventoryPanel = FindOrCreateUiChild(canvas.transform, "InventoryPanel");
            ConfigurePanelRect(inventoryPanel.GetComponent<RectTransform>());

            var inventoryContent = FindOrCreateUiChild(inventoryPanel.transform, "InventoryContent");
            ConfigureContentStrip(inventoryContent);

            var inventoryPanelView = EnsureComponent<InventoryPanelView>(inventoryPanel);
            var cardPrefab = LoadOrCreateCardPrefab();
            ConfigureInventoryPanelView(inventoryPanelView, inventoryService, runtimeDataService, inventoryContent, cardPrefab);

            var debugControls = EnsureComponent<InventoryDebugControls>(inventoryPanel);
            ConfigureInventoryDebugControls(debugControls, inventoryService);
            CreateDebugButtons(inventoryPanel.transform, debugControls);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = inventoryPanel;
            Debug.Log("Inventory UI setup completed. Created or updated InventoryPanel, InventoryContent, InventoryItemCard prefab, services, and debug buttons.");
        }

        private static Canvas FindOrCreateCanvas()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                return canvas;
            }

            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return canvas;
        }

        private static void FindOrCreateEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static GameEntry FindOrCreateGameEntry()
        {
            var gameEntry = Object.FindFirstObjectByType<GameEntry>();
            if (gameEntry != null)
            {
                return gameEntry;
            }

            return new GameObject("GameEntry").AddComponent<GameEntry>();
        }

        private static GameObject FindOrCreateUiChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                return child.gameObject;
            }

            var childObject = new GameObject(childName, typeof(RectTransform));
            childObject.transform.SetParent(parent, false);
            return childObject;
        }

        private static void ConfigurePanelRect(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(24f, 0f);
            rectTransform.sizeDelta = new Vector2(420f, -48f);
        }

        private static void ConfigureContentStrip(GameObject contentObject)
        {
            var rectTransform = contentObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(16f, 124f);
            rectTransform.offsetMax = new Vector2(-16f, -16f);

            var layoutGroup = contentObject.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                Object.DestroyImmediate(layoutGroup);
            }
        }

        private static InventoryItemCard LoadOrCreateCardPrefab()
        {
            var cardPrefab = AssetDatabase.LoadAssetAtPath<InventoryItemCard>(InventoryCardPrefabPath);
            if (cardPrefab != null)
            {
                return cardPrefab;
            }

            InventoryPrefabBuilder.CreateInventoryItemCardPrefab();
            AssetDatabase.Refresh();
            cardPrefab = AssetDatabase.LoadAssetAtPath<InventoryItemCard>(InventoryCardPrefabPath);
            if (cardPrefab == null)
            {
                throw new FileNotFoundException($"Failed to create inventory card prefab at {InventoryCardPrefabPath}.");
            }

            return cardPrefab;
        }

        private static void CreateDebugButtons(Transform panelTransform, InventoryDebugControls debugControls)
        {
            var debugRoot = FindOrCreateUiChild(panelTransform, "InventoryDebugButtons");
            var rectTransform = debugRoot.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, 12f);
            rectTransform.sizeDelta = new Vector2(-32f, 96f);

            var grid = EnsureComponent<GridLayoutGroup>(debugRoot);
            grid.cellSize = new Vector2(120f, 36f);
            grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.MiddleCenter;

            CreateButton(debugRoot.transform, "AddMoneyButton", "+ Money", debugControls, nameof(InventoryDebugControls.AddMoney));
            CreateButton(debugRoot.transform, "RemoveMoneyButton", "- Money", debugControls, nameof(InventoryDebugControls.RemoveMoney));
            CreateButton(debugRoot.transform, "AddMaterialButton", "+ Material", debugControls, nameof(InventoryDebugControls.AddMaterial));
            CreateButton(debugRoot.transform, "RemoveMaterialButton", "- Material", debugControls, nameof(InventoryDebugControls.RemoveMaterial));
            CreateButton(debugRoot.transform, "AddFoodButton", "+ Food", debugControls, nameof(InventoryDebugControls.AddFood));
            CreateButton(debugRoot.transform, "RemoveFoodButton", "- Food", debugControls, nameof(InventoryDebugControls.RemoveFood));
        }

        private static void CreateButton(Transform parent, string name, string label, InventoryDebugControls target, string methodName)
        {
            var buttonTransform = parent.Find(name);
            var buttonObject = buttonTransform != null
                ? buttonTransform.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));

            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.23f, 0.23f, 0.2f, 1f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            AddPersistentListenerIfMissing(button, target, methodName, CreateAction(target, methodName));

            var labelObject = FindOrCreateUiChild(buttonObject.transform, "Label");
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            RemoveLegacyText(labelObject);

            var text = EnsureComponent<TextMeshProUGUI>(labelObject);
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;
        }

        private static void RemoveLegacyText(GameObject gameObject)
        {
            foreach (var component in gameObject.GetComponents<Component>())
            {
                var componentType = component.GetType();
                if (componentType.FullName == "UnityEngine.UI.Text")
                {
                    Object.DestroyImmediate(component);
                }
            }
        }

        private static void AddPersistentListenerIfMissing(Button button, Object target, string methodName, UnityAction action)
        {
            for (var index = 0; index < button.onClick.GetPersistentEventCount(); index++)
            {
                if (button.onClick.GetPersistentTarget(index) == target &&
                    button.onClick.GetPersistentMethodName(index) == methodName)
                {
                    return;
                }
            }

            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static UnityAction CreateAction(InventoryDebugControls target, string methodName)
        {
            switch (methodName)
            {
                case nameof(InventoryDebugControls.AddMoney):
                    return target.AddMoney;
                case nameof(InventoryDebugControls.RemoveMoney):
                    return target.RemoveMoney;
                case nameof(InventoryDebugControls.AddMaterial):
                    return target.AddMaterial;
                case nameof(InventoryDebugControls.RemoveMaterial):
                    return target.RemoveMaterial;
                case nameof(InventoryDebugControls.AddFood):
                    return target.AddFood;
                case nameof(InventoryDebugControls.RemoveFood):
                    return target.RemoveFood;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(methodName), methodName, "Unsupported inventory debug method.");
            }
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void ConfigureConfigManager(ConfigManager configManager)
        {
            var serializedObject = new SerializedObject(configManager);
            serializedObject.FindProperty("relativeConfigDirectory").stringValue = "Configs/Demo";
            serializedObject.FindProperty("loadOnAwake").boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRuntimeDataService(RuntimeDataService runtimeDataService, ConfigManager configManager)
        {
            var serializedObject = new SerializedObject(runtimeDataService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("createNewGameOnAwake").boolValue = true;
            serializedObject.FindProperty("initialDisasterId").stringValue = "disaster_flood_01";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureInventoryService(
            InventoryService inventoryService,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(inventoryService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureInventoryPanelView(
            InventoryPanelView panelView,
            InventoryService inventoryService,
            RuntimeDataService runtimeDataService,
            GameObject contentRoot,
            InventoryItemCard cardPrefab)
        {
            var serializedObject = new SerializedObject(panelView);
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("contentRoot").objectReferenceValue = contentRoot.GetComponent<RectTransform>();
            serializedObject.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
            serializedObject.FindProperty("showZeroCountItems").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureInventoryDebugControls(InventoryDebugControls debugControls, InventoryService inventoryService)
        {
            var serializedObject = new SerializedObject(debugControls);
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
