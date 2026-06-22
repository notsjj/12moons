using System;
using System.Linq;
using TMPro;
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

                if (!configManager.TryGetTable("CityPointConfig", out var pointTable) ||
                    !configManager.TryGetTable("SideEventConfig", out var sideEventTable) ||
                    sideEventTable.Rows.Count < 30)
                {
                    throw new InvalidOperationException("CityPointConfig and SideEventConfig must load the new plot point and side event rows.");
                }

                var pointIds = pointTable.Rows
                    .Select(row => row.GetString("PointId"))
                    .Where(pointId => !string.IsNullOrEmpty(pointId))
                    .ToArray();
                var sideEventPointIds = sideEventTable.Rows
                    .Select(row => row.GetString("PointId"))
                    .Where(pointId => !string.IsNullOrEmpty(pointId))
                    .Distinct()
                    .ToArray();
                var missingPointIds = sideEventPointIds.Except(pointIds).ToArray();
                if (missingPointIds.Length > 0)
                {
                    throw new InvalidOperationException($"SideEventConfig references missing CityPointConfig.PointId values: {string.Join(", ", missingPointIds)}");
                }

                if (sideEventTable.Rows.Any(row => string.IsNullOrEmpty(row.GetString("SideEventId"))))
                {
                    throw new InvalidOperationException("Every SideEventConfig row must have a stable SideEventId so runtime trigger state can be recorded.");
                }

                var runtimeDataService = root.AddComponent<RuntimeDataService>();
                ConfigureRuntimeDataService(runtimeDataService, configManager);
                runtimeDataService.CreateNewGame("DI0001");
                runtimeDataService.Data.SetCurrentRound(1);

                var inventoryService = root.AddComponent<InventoryService>();
                ConfigureInventoryService(inventoryService, configManager, runtimeDataService);
                inventoryService.Refresh();

                var roundService = root.AddComponent<RoundService>();
                ConfigureRoundService(roundService, configManager, runtimeDataService);

                var taskService = root.AddComponent<TaskService>();
                ConfigureTaskService(taskService, configManager, runtimeDataService, roundService);
                taskService.Refresh();
                // 第一回合支线不依赖任务激活。

                var storyService = root.AddComponent<StoryService>();
                ConfigureStoryService(storyService, configManager, runtimeDataService, inventoryService, taskService);
                storyService.Refresh();

                var cityRoot = new GameObject("CityRoot");
                cityRoot.transform.SetParent(root.transform, false);

                var pointViews = pointIds
                    .Select(pointId => CreatePointView(cityRoot.transform, $"PointView_{pointId}", pointId))
                    .ToArray();
                var pointRegistry = cityRoot.AddComponent<CityPointRegistry>();
                ConfigurePointRegistry(pointRegistry, configManager, pointViews);
                pointRegistry.RefreshAndBind();

                var sideEventService = cityRoot.AddComponent<CitySideEventService>();
                ConfigureSideEventService(sideEventService, configManager, runtimeDataService, roundService, storyService, taskService, inventoryService);
                sideEventService.Refresh();

                var sideEventRegistry = cityRoot.AddComponent<CitySideEventRegistry>();
                ConfigureSideEventRegistry(sideEventRegistry, sideEventService, pointRegistry);
                sideEventRegistry.RefreshAndBind();

                var visibleEvents = sideEventService.GetVisibleEvents();
                if (!visibleEvents.Any(candidate => candidate.SideEventId == "SE0001" && candidate.PointId == "P0013"))
                {
                    throw new InvalidOperationException("Round 1 should show SE0001 at P0013/教会 from the new SideEventConfig.");
                }

                foreach (var definition in visibleEvents)
                {
                    var view = cityRoot.GetComponentsInChildren<CitySideEventView>(true)
                        .FirstOrDefault(candidate => candidate.SideEventId == definition.SideEventId);
                    if (view == null || !view.IsBound || !view.IsModelVisualVisible || view.GetComponentInChildren<TextMeshPro>(true) == null)
                    {
                        throw new InvalidOperationException($"Visible side event {definition.SideEventId} was not bound to a scene view at {definition.PointId}.");
                    }

                    if (!pointRegistry.TryGetView(definition.PointId, out var pointView) ||
                        pointView.ActiveSideEventId != definition.SideEventId ||
                        !pointView.HasActiveSideEvent)
                    {
                        throw new InvalidOperationException($"Visible side event {definition.SideEventId} was not bound to clickable building point {definition.PointId}.");
                    }
                }

                if (!pointRegistry.TryGetView("P0013", out var clickablePoint) || !clickablePoint.TryTriggerBoundSideEvent())
                {
                    throw new InvalidOperationException("Clicking building point P0013/教会 did not trigger its bound side event.");
                }

                if (
                    storyService.CurrentPlayback == null ||
                    storyService.CurrentPlayback.Story.StoryId != "S0004" ||
                    !runtimeDataService.Data.TryGetSideEvent("SE0001", out var triggeredState) ||
                    !triggeredState.HasTriggered)
                {
                    throw new InvalidOperationException("Clicking SE0001 did not start its configured story or record trigger state.");
                }

                runtimeDataService.Data.SetCurrentRound(3);
                roundService.Refresh();
                var roundFiveEvents = sideEventService.GetVisibleEvents();
                if (!roundFiveEvents.Any(candidate => candidate.PointId == "P0004"))
                {
                    throw new InvalidOperationException("Round 3 side events should refresh and appear at P0004.");
                }

                sideEventRegistry.RefreshAndBind();
                var roundFiveView = cityRoot.GetComponentsInChildren<CitySideEventView>(true)
                    .FirstOrDefault(view => roundFiveEvents.Any(definition => definition.SideEventId == view.SideEventId));
                if (roundFiveView == null)
                {
                    throw new InvalidOperationException("CitySideEventRegistry did not bind a visible round 5 side event after round refresh.");
                }

                Debug.Log("City side event smoke test passed. SideEventConfig point ids exist, round visibility refreshes, scene point views bind side events, and clicks start configured stories.");
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

            var serializedObject = new SerializedObject(pointView);
            serializedObject.FindProperty("requireCityRootActive").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
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

        private static void ConfigureInventoryService(InventoryService inventoryService, ConfigManager configManager, RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(inventoryService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRoundService(RoundService roundService, ConfigManager configManager, RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(roundService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTaskService(TaskService taskService, ConfigManager configManager, RuntimeDataService runtimeDataService, RoundService roundService)
        {
            var serializedObject = new SerializedObject(taskService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("roundService").objectReferenceValue = roundService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureStoryService(StoryService storyService, ConfigManager configManager, RuntimeDataService runtimeDataService, InventoryService inventoryService, TaskService taskService)
        {
            var serializedObject = new SerializedObject(storyService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePointRegistry(CityPointRegistry pointRegistry, ConfigManager configManager, params CityPointView[] pointViews)
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
            RoundService roundService,
            StoryService storyService,
            TaskService taskService,
            InventoryService inventoryService)
        {
            var serializedObject = new SerializedObject(sideEventService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("roundService").objectReferenceValue = roundService;
            serializedObject.FindProperty("storyService").objectReferenceValue = storyService;
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSideEventRegistry(CitySideEventRegistry sideEventRegistry, CitySideEventService sideEventService, CityPointRegistry pointRegistry)
        {
            var serializedObject = new SerializedObject(sideEventRegistry);
            serializedObject.FindProperty("sideEventService").objectReferenceValue = sideEventService;
            serializedObject.FindProperty("pointRegistry").objectReferenceValue = pointRegistry;
            serializedObject.FindProperty("createMissingViews").boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
