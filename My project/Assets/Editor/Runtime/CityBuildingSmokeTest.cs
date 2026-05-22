using System;
using TwelveMoons.City;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class CityBuildingSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run City Building Smoke Test")]
        public static void Run()
        {
            var root = new GameObject("CityBuildingSmokeTestRoot");

            try
            {
                var configManager = root.AddComponent<ConfigManager>();
                ConfigureConfigManager(configManager);
                configManager.BuildDefaultProviders();

                if (!configManager.TryGetTable("CityBuildingConfig", out var buildingTable) ||
                    buildingTable.Rows.Count < 2 ||
                    !buildingTable.TryFindById("BuildingId", "building_relief_depot", out _))
                {
                    throw new InvalidOperationException("CityBuildingConfig demo data is missing required building rows.");
                }

                if (!configManager.TryGetTable("DocumentConfig", out var documentTable) ||
                    !documentTable.TryFindById("DocumentId", "document_market_notice", out var buildDocumentRow) ||
                    buildDocumentRow.GetString("OptionA_UnlockBuildingId") != "building_relief_depot")
                {
                    throw new InvalidOperationException("DocumentConfig must include a visible global demo document whose option A unlocks building_relief_depot.");
                }

                if (buildDocumentRow.GetInt("OptionA_MoneyChange") < 0 ||
                    buildDocumentRow.GetInt("OptionA_MaterialChange") < 0 ||
                    buildDocumentRow.GetInt("OptionA_FoodChange") < 0 ||
                    !string.IsNullOrEmpty(buildDocumentRow.GetString("OptionA_RequiredItemId")))
                {
                    throw new InvalidOperationException("The building unlock demo document must not require resources or items, otherwise option A can fail before unlocking the building.");
                }

                var runtimeDataService = root.AddComponent<RuntimeDataService>();
                ConfigureRuntimeDataService(runtimeDataService, configManager);
                runtimeDataService.CreateNewGame("disaster_flood_01");

                var inventoryService = root.AddComponent<InventoryService>();
                ConfigureInventoryService(inventoryService, configManager, runtimeDataService);
                inventoryService.Refresh();

                var factionService = root.AddComponent<FactionService>();
                ConfigureFactionService(factionService, configManager, runtimeDataService);
                factionService.Refresh();

                var buildingService = root.AddComponent<CityBuildingService>();
                ConfigureBuildingService(buildingService, configManager, runtimeDataService, inventoryService, factionService);
                buildingService.Refresh();

                var reliefViewObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                reliefViewObject.name = "ReliefDepotBuildingView";
                reliefViewObject.transform.SetParent(root.transform, false);
                var reliefRenderer = reliefViewObject.GetComponent<Renderer>();
                var reliefView = reliefViewObject.AddComponent<CityBuildingView>();
                reliefView.Configure("building_relief_depot", "city_point_royal_gate");

                var shelterViewObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelterViewObject.name = "ChurchShelterBuildingView";
                shelterViewObject.transform.SetParent(root.transform, false);
                var shelterView = shelterViewObject.AddComponent<CityBuildingView>();
                shelterView.Configure("building_church_shelter", "city_point_church_square");

                var registry = root.AddComponent<CityBuildingRegistry>();
                ConfigureRegistry(registry, buildingService, configManager, reliefView, shelterView);
                registry.RefreshAndBind();

                if (registry.MatchedViewCount != 2 ||
                    !string.IsNullOrEmpty(registry.UnmatchedViewBuildingIds) ||
                    !string.IsNullOrEmpty(registry.DuplicateViewBuildingIds))
                {
                    throw new InvalidOperationException("CityBuildingRegistry failed to match CityBuildingView BuildingId values against CityBuildingConfig.");
                }

                if (reliefRenderer.enabled)
                {
                    throw new InvalidOperationException("Locked building should be hidden before document unlock.");
                }

                buildingService.enabled = false;
                registry.enabled = false;
                runtimeDataService.UnlockBuilding("building_relief_depot");
                buildingService.enabled = true;
                registry.enabled = true;
                if (!buildingService.IsUnlocked("building_relief_depot") || !reliefRenderer.enabled)
                {
                    throw new InvalidOperationException("Document building unlock did not make the building visible after the city root was re-enabled.");
                }

                var foodBefore = inventoryService.GetCount("item_food");
                if (!buildingService.TryCollect("building_relief_depot", out _) ||
                    inventoryService.GetCount("item_food") != foodBefore + 2)
                {
                    throw new InvalidOperationException("Resource building click did not add configured item output.");
                }

                if (buildingService.TryCollect("building_relief_depot", out _))
                {
                    throw new InvalidOperationException("Resource building should not be collectible twice in the same round.");
                }

                var churchBefore = factionService.GetSuspicion("church");
                if (!buildingService.TryCollect("building_church_shelter", out _) ||
                    factionService.GetSuspicion("church") != churchBefore - 5)
                {
                    throw new InvalidOperationException("Suspicion building click did not reduce configured faction suspicion.");
                }

                Debug.Log("City building smoke test passed. Config loading, document unlock visibility, resource output, suspicion reduction, and cooldown all work.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureConfigManager(ConfigManager configManager)
        {
            var serializedObject = new SerializedObject(configManager);
            serializedObject.FindProperty("relativeConfigDirectory").stringValue = "Configs/Demo";
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

        private static void ConfigureFactionService(
            FactionService factionService,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService)
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

        private static void ConfigureRegistry(
            CityBuildingRegistry registry,
            CityBuildingService buildingService,
            ConfigManager configManager,
            params CityBuildingView[] buildingViews)
        {
            var serializedObject = new SerializedObject(registry);
            serializedObject.FindProperty("buildingService").objectReferenceValue = buildingService;
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("bindOnStart").boolValue = false;
            serializedObject.FindProperty("autoCollectSceneViews").boolValue = false;

            var viewsProperty = serializedObject.FindProperty("buildingViews");
            viewsProperty.arraySize = buildingViews.Length;
            for (var index = 0; index < buildingViews.Length; index++)
            {
                viewsProperty.GetArrayElementAtIndex(index).objectReferenceValue = buildingViews[index];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
