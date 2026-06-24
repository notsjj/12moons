using System.IO;
using System.Linq;
using TMPro;
using TwelveMoons.City;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class SettlementPanelSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run Settlement Panel Smoke Test")]
        public static void Run()
        {
            ValidateSettlementPanelWritesTextsAndReturnButtonCloses();
            ValidateCityBuildingsCollectAvailableOutputsForSettlement();
            Debug.Log("Settlement panel smoke test passed: panel fills building/document text and auto-collects available city building outputs.");
        }

        private static void ValidateSettlementPanelWritesTextsAndReturnButtonCloses()
        {
            var root = new GameObject("结算面板测试根");
            try
            {
                var buildingText = CreateText(root.transform, "建筑产出");
                var documentText = CreateText(root.transform, "公文奖励");
                var buttonObject = new GameObject("返回按钮", typeof(RectTransform), typeof(Button));
                buttonObject.transform.SetParent(root.transform, false);

                var view = root.AddComponent<SettlementPanelView>();
                view.Show("获得 金币 x2", "公文处理：测试公文 - 获得奖励");

                if (buildingText.text != "获得 金币 x2" ||
                    documentText.text != "公文处理：测试公文 - 获得奖励")
                {
                    throw new InvalidDataException("结算面板没有把建筑产出和公文奖励分别写入同名 TMP 文本。");
                }

                buttonObject.GetComponent<Button>().onClick.Invoke();
                if (root.activeSelf)
                {
                    throw new InvalidDataException("结算面板返回按钮没有关闭面板。");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ValidateCityBuildingsCollectAvailableOutputsForSettlement()
        {
            var root = new GameObject("结算建筑测试根");
            try
            {
                var configManager = root.AddComponent<ConfigManager>();
                var runtimeDataService = root.AddComponent<RuntimeDataService>();
                var inventoryService = root.AddComponent<InventoryService>();
                var factionService = root.AddComponent<FactionService>();
                var buildingService = root.AddComponent<CityBuildingService>();

                ConfigureConfigManager(configManager);
                ConfigureRuntimeDataService(runtimeDataService, configManager);
                ConfigureInventoryService(inventoryService, configManager, runtimeDataService);
                ConfigureFactionService(factionService, configManager, runtimeDataService);
                ConfigureBuildingService(buildingService, configManager, runtimeDataService, inventoryService, factionService);

                configManager.BuildDefaultProviders();
                runtimeDataService.CreateNewGame("DI0001");
                inventoryService.Refresh();
                factionService.Refresh();
                buildingService.Refresh();

                runtimeDataService.UnlockBuilding("B0004");
                runtimeDataService.UnlockBuilding("B0006");

                var messages = buildingService.CollectAvailableOutputsForSettlement();
                if (messages.Count < 2 ||
                    !messages.Any(message => message.Contains("获得")) ||
                    !inventoryService.HasItem("I0001", 1) ||
                    !inventoryService.HasItem("I0003", 2))
                {
                    throw new InvalidDataException("结算时没有自动领取所有可领取的资源类城区建筑产出。");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static TMP_Text CreateText(Transform parent, string name)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            return textObject.GetComponent<TMP_Text>();
        }

        private static void ConfigureConfigManager(ConfigManager configManager)
        {
            var serializedObject = new SerializedObject(configManager);
            serializedObject.FindProperty("relativeConfigDirectory").stringValue = "Configs/Plot";
            serializedObject.FindProperty("loadOnAwake").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRuntimeDataService(RuntimeDataService runtimeDataService, ConfigManager configManager)
        {
            var serializedObject = new SerializedObject(runtimeDataService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("createNewGameOnAwake").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureInventoryService(InventoryService inventoryService, ConfigManager configManager, RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(inventoryService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureFactionService(FactionService factionService, ConfigManager configManager, RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(factionService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureBuildingService(
            CityBuildingService buildingService,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService,
            InventoryService inventoryService,
            FactionService factionService)
        {
            var serializedObject = new SerializedObject(buildingService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.FindProperty("factionService").objectReferenceValue = factionService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
