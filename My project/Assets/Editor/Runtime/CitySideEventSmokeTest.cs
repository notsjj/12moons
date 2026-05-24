using System;
using System.Linq;
using TwelveMoons.City;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class CitySideEventSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run City Side Event Smoke Test")]
        public static void Run()
        {
            var root = new GameObject("CitySideEventSmokeTestRoot");

            try
            {
                var configManager = root.AddComponent<ConfigManager>();
                ConfigureConfigManager(configManager);
                configManager.BuildDefaultProviders();

                if (!configManager.TryGetTable("SideEventConfig", out var sideEventTable) ||
                    sideEventTable.Rows.Count < 2 ||
                    !sideEventTable.TryFindById("SideEventId", "side_event_lower_harbor_courier", out _))
                {
                    throw new InvalidOperationException("SideEventConfig demo data is missing required stage 16 rows.");
                }

                var runtimeDataService = root.AddComponent<RuntimeDataService>();
                ConfigureRuntimeDataService(runtimeDataService, configManager);
                runtimeDataService.CreateNewGame("disaster_flood_01");

                var inventoryService = root.AddComponent<InventoryService>();
                ConfigureInventoryService(inventoryService, configManager, runtimeDataService);
                inventoryService.Refresh();

                var taskService = root.AddComponent<TaskService>();
                ConfigureTaskService(taskService, configManager, runtimeDataService);
                taskService.Refresh();

                var storyService = root.AddComponent<StoryService>();
                ConfigureStoryService(storyService, configManager, runtimeDataService, inventoryService, taskService);
                storyService.Refresh();

                var cityRoot = new GameObject("CityRoot");
                cityRoot.transform.SetParent(root.transform, false);

                var lowerHarborPoint = CreatePointView(cityRoot.transform, "LowerHarborPointView", "city_point_lower_harbor");
                var academyPoint = CreatePointView(cityRoot.transform, "AcademyArchivePointView", "city_point_academy_archive");
                var pointRegistry = cityRoot.AddComponent<CityPointRegistry>();
                ConfigurePointRegistry(pointRegistry, configManager, lowerHarborPoint, academyPoint);
                pointRegistry.RefreshAndBind();

                var sideEventService = cityRoot.AddComponent<CitySideEventService>();
                ConfigureSideEventService(sideEventService, configManager, runtimeDataService, storyService, taskService, inventoryService);
                sideEventService.Refresh();

                var sideEventRegistry = cityRoot.AddComponent<CitySideEventRegistry>();
                ConfigureSideEventRegistry(sideEventRegistry, sideEventService, pointRegistry);
                sideEventRegistry.RefreshAndBind();

                var firstRoundEvents = sideEventService.GetVisibleEvents();
                if (firstRoundEvents.Count != 1 ||
                    firstRoundEvents[0].SideEventId != "side_event_lower_harbor_courier")
                {
                    throw new InvalidOperationException("Round 1 should only show the lower harbor side event.");
                }

                var lowerHarborView = lowerHarborPoint.GetComponentInChildren<CitySideEventView>(true);
                if (lowerHarborView == null || lowerHarborView.SideEventId != "side_event_lower_harbor_courier")
                {
                    throw new InvalidOperationException("CitySideEventRegistry did not create the side event view under the configured CityPointView.");
                }

                lowerHarborView.OnClicked();
                if (storyService.CurrentPlayback == null ||
                    storyService.CurrentPlayback.Story.StoryId != "story_side_lower_harbor" ||
                    !runtimeDataService.Data.TryGetSideEvent("side_event_lower_harbor_courier", out var triggeredState) ||
                    !triggeredState.HasTriggered)
                {
                    throw new InvalidOperationException("Clicking the side event view did not start the configured story or record runtime trigger state.");
                }

                storyService.EndCurrentStory();
                sideEventRegistry.RefreshAndBind();
                if (sideEventService.GetVisibleEvents().Any(candidate => candidate.SideEventId == "side_event_lower_harbor_courier"))
                {
                    throw new InvalidOperationException("One-time side event should hide after it has been triggered.");
                }

                runtimeDataService.Data.SetCurrentRound(2);
                taskService.ActivateTask("task_demo_relief_01");
                sideEventService.Refresh();
                var secondRoundEvents = sideEventService.GetVisibleEvents();
                if (!secondRoundEvents.Any(candidate => candidate.SideEventId == "side_event_academy_engineer"))
                {
                    throw new InvalidOperationException("Task-gated side event did not become visible when RequiredTaskId and RequiredTaskState were satisfied.");
                }

                if (!sideEventService.TryStartSideEvent("side_event_academy_engineer", out var resultMessage) ||
                    storyService.CurrentPlayback == null ||
                    storyService.CurrentPlayback.Story.StoryId != "story_side_academy_archive")
                {
                    throw new InvalidOperationException($"Task-gated side event failed to play its configured story: {resultMessage}");
                }

                Debug.Log("City side event smoke test passed. SideEventConfig loads, round/task conditions filter visibility, point views spawn side characters, click starts StoryService, and one-time trigger state is recorded.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static CityPointView CreatePointView(Transform parent, string objectName, string pointId)
        {
            var pointObject = new GameObject(objectName);
            pointObject.transform.SetParent(parent, false);
            var pointView = pointObject.AddComponent<CityPointView>();
            pointView.Configure(pointId);
            return pointView;
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

        private static void ConfigureTaskService(
            TaskService taskService,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(taskService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureStoryService(
            StoryService storyService,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService,
            InventoryService inventoryService,
            TaskService taskService)
        {
            var serializedObject = new SerializedObject(storyService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePointRegistry(
            CityPointRegistry pointRegistry,
            ConfigManager configManager,
            params CityPointView[] pointViews)
        {
            var serializedObject = new SerializedObject(pointRegistry);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("bindOnStart").boolValue = false;
            serializedObject.FindProperty("autoCollectSceneViews").boolValue = false;

            var viewsProperty = serializedObject.FindProperty("pointViews");
            viewsProperty.arraySize = pointViews.Length;
            for (var index = 0; index < pointViews.Length; index++)
            {
                viewsProperty.GetArrayElementAtIndex(index).objectReferenceValue = pointViews[index];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSideEventService(
            CitySideEventService sideEventService,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService,
            StoryService storyService,
            TaskService taskService,
            InventoryService inventoryService)
        {
            var serializedObject = new SerializedObject(sideEventService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("storyService").objectReferenceValue = storyService;
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSideEventRegistry(
            CitySideEventRegistry sideEventRegistry,
            CitySideEventService sideEventService,
            CityPointRegistry pointRegistry)
        {
            var serializedObject = new SerializedObject(sideEventRegistry);
            serializedObject.FindProperty("sideEventService").objectReferenceValue = sideEventService;
            serializedObject.FindProperty("pointRegistry").objectReferenceValue = pointRegistry;
            serializedObject.FindProperty("createMissingViews").boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
