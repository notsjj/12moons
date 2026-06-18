using System;
using System.Reflection;
using TwelveMoons.City;
using TwelveMoons.Core;
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

                var documentService = root.AddComponent<DocumentService>();
                ConfigureDocumentService(documentService, configManager, runtimeDataService, inventoryService, factionService);
                documentService.Refresh();

                var cityRoot = new GameObject("CityRoot");
                cityRoot.transform.SetParent(root.transform, false);

                var buildingService = cityRoot.AddComponent<CityBuildingService>();
                ConfigureBuildingService(buildingService, configManager, runtimeDataService, inventoryService, factionService);
                buildingService.Refresh();

                var reliefViewObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                reliefViewObject.name = "ReliefDepotBuildingView";
                reliefViewObject.transform.SetParent(cityRoot.transform, false);
                var reliefRenderer = reliefViewObject.GetComponent<Renderer>();
                var reliefView = reliefViewObject.AddComponent<CityBuildingView>();
                reliefView.Configure("building_relief_depot", "city_point_royal_gate");

                var shelterViewObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelterViewObject.name = "ChurchShelterBuildingView";
                shelterViewObject.transform.SetParent(cityRoot.transform, false);
                var shelterView = shelterViewObject.AddComponent<CityBuildingView>();
                shelterView.Configure("building_church_shelter", "city_point_church_square");

                ValidateStandaloneHoverAutoBinding();
                ValidateHoverBlockedUntilDeskHidden();

                var registry = cityRoot.AddComponent<CityBuildingRegistry>();
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

                cityRoot.SetActive(false);
                var entry = runtimeDataService.Data.QueueDocument("document_market_notice", string.Empty, string.Empty, string.Empty);
                var result = documentService.ResolveDocument(entry, DocumentOptionType.A);
                cityRoot.SetActive(true);
                if (!result.Success)
                {
                    throw new InvalidOperationException($"Document option A should unlock the relief depot building: {result.Message}");
                }

                if (!buildingService.IsUnlocked("building_relief_depot") || !reliefRenderer.enabled)
                {
                    throw new InvalidOperationException("Document building unlock did not enable the MeshRenderer while the city root was inactive.");
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

        private static void ValidateHoverBlockedUntilDeskHidden()
        {
            var root = new GameObject("CityHoverGateSmokeTestRoot");
            var deskRoot = new GameObject("DeskRoot");
            var cityRoot = new GameObject("CityRoot");
            var buildingObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                deskRoot.transform.SetParent(root.transform, false);
                cityRoot.transform.SetParent(root.transform, false);
                buildingObject.transform.SetParent(cityRoot.transform, false);

                var gameEntry = root.AddComponent<GameEntry>();
                var entryObject = new SerializedObject(gameEntry);
                entryObject.FindProperty("deskRoot").objectReferenceValue = deskRoot;
                entryObject.FindProperty("cityRoot").objectReferenceValue = cityRoot;
                entryObject.ApplyModifiedPropertiesWithoutUndo();

                var view = buildingObject.AddComponent<CityBuildingView>();
                var viewObject = new SerializedObject(view);
                viewObject.FindProperty("gameEntry").objectReferenceValue = gameEntry;
                viewObject.ApplyModifiedPropertiesWithoutUndo();

                var method = typeof(CityBuildingView).GetMethod(
                    "IsCityInteractionEnabled",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (method == null)
                {
                    throw new MissingMethodException(nameof(CityBuildingView), "IsCityInteractionEnabled");
                }

                deskRoot.SetActive(true);
                cityRoot.SetActive(true);
                if ((bool)method.Invoke(view, Array.Empty<object>()))
                {
                    throw new InvalidOperationException("City buildings must not show hover outlines while DeskRoot is still visible before entering the city.");
                }

                deskRoot.SetActive(false);
                cityRoot.SetActive(true);
                if (!(bool)method.Invoke(view, Array.Empty<object>()))
                {
                    throw new InvalidOperationException("City buildings should allow hover outlines after DeskRoot is hidden and CityRoot is visible.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ValidateStandaloneHoverAutoBinding()
        {
            var buildingObject = new GameObject("StandaloneHoverBuilding");
            var childObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                childObject.transform.SetParent(buildingObject.transform, false);
                childObject.transform.localPosition = new Vector3(2f, 0f, 0f);
                var childCollider = childObject.GetComponent<Collider>();
                if (childCollider != null)
                {
                    UnityEngine.Object.DestroyImmediate(childCollider);
                }

                var view = buildingObject.AddComponent<CityBuildingView>();
                view.InitializeRuntimeHoverDependenciesForTest();

                if (!view.IsHoverOutlineRuntimeReady)
                {
                    throw new InvalidOperationException("Standalone CityBuildingView must auto-bind renderers, outline effect, and a same-object collider for hover outlines.");
                }

                var ownCollider = buildingObject.GetComponent<Collider>();
                if (ownCollider == null)
                {
                    throw new InvalidOperationException("Standalone CityBuildingView must add a collider to the same GameObject so OnMouseEnter reaches the view script.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(buildingObject);
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

        private static void ConfigureDocumentService(
            DocumentService documentService,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService,
            InventoryService inventoryService,
            FactionService factionService)
        {
            var serializedObject = new SerializedObject(documentService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.FindProperty("factionService").objectReferenceValue = factionService;
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
