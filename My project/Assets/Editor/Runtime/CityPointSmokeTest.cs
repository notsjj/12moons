using System;
using System.Linq;
using TwelveMoons.City;
using TwelveMoons.Core;
using TwelveMoons.Core.Config;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class CityPointSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run City Point Smoke Test")]
        public static void Run()
        {
            var root = new GameObject("CityPointSmokeTestRoot");

            try
            {
                var configManager = root.AddComponent<ConfigManager>();
                ConfigureConfigManager(configManager);
                configManager.BuildDefaultProviders();

                if (!configManager.TryGetTable("CityPointConfig", out var table) ||
                    !table.TryFindById("PointId", "P0001", out _) ||
                    !table.TryFindById("PointId", "P0014", out _))
                {
                    throw new InvalidOperationException("CityPointConfig must contain the new P0001-P0014 city point ids.");
                }

                var pointViews = table.Rows
                    .Select(row => row.GetString("PointId"))
                    .Where(pointId => !string.IsNullOrEmpty(pointId))
                    .Select(pointId => CreatePointView(root.transform, $"PointView_{pointId}", pointId))
                    .ToArray();

                var registry = root.AddComponent<CityPointRegistry>();
                ConfigureRegistry(registry, configManager, pointViews);
                registry.RefreshAndBind();

                if (registry.ConfigCount != table.Rows.Count ||
                    registry.MatchedViewCount != table.Rows.Count ||
                    !string.IsNullOrEmpty(registry.UnmatchedViewPointIds) ||
                    !string.IsNullOrEmpty(registry.UnusedConfigPointIds) ||
                    !string.IsNullOrEmpty(registry.DuplicateViewPointIds))
                {
                    throw new InvalidOperationException("Every CityPointConfig.PointId must have one matching CityPointView and no duplicate scene point ids.");
                }

                if (!registry.TryGetView("P0001", out var view) ||
                    !view.IsMatched ||
                    view.Definition.PointId != "P0001")
                {
                    throw new InvalidOperationException("CityPointView did not receive the matched P0001 definition.");
                }

                ValidatePointHoverOutlineApi();
                ValidatePointInteractionRequiresCityEntry();

                Debug.Log("City point smoke test passed. All P0001-P0014 CityPointConfig ids match CityPointView instances, with no missing or duplicate point ids.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ValidatePointHoverOutlineApi()
        {
            var pointObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointObject.name = "HoverOutlinePointView";
            try
            {
                var pointView = pointObject.AddComponent<CityPointView>();
                pointView.InitializeRuntimeHoverDependenciesForTest();

                if (!pointView.IsHoverOutlineRuntimeReady)
                {
                    throw new InvalidOperationException("CityPointView must auto-bind renderers, a same-object collider, and CityBuildingOutlineEffect for hover outlines.");
                }

                if (pointObject.GetComponents<CityPointView>().Length != 1)
                {
                    throw new InvalidOperationException("CityPointView should not allow duplicate components on the same GameObject.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(pointObject);
            }
        }


        private static void ValidatePointInteractionRequiresCityEntry()
        {
            var root = new GameObject("CityPointEntryGateRoot");
            var deskRoot = new GameObject("DeskRoot");
            var cityRoot = new GameObject("CityRoot");
            var pointObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                deskRoot.transform.SetParent(root.transform, false);
                cityRoot.transform.SetParent(root.transform, false);
                pointObject.transform.SetParent(root.transform, false);

                var entry = root.AddComponent<GameEntry>();
                SetGameEntryRoots(entry, deskRoot, cityRoot);

                var pointView = pointObject.AddComponent<CityPointView>();
                pointView.Configure("P0001");
                pointView.InitializeRuntimeHoverDependenciesForTest();

                entry.ShowDesk();
                pointObject.SendMessage("OnMouseEnter", SendMessageOptions.DontRequireReceiver);
                if (pointView.IsHoverOutlineVisible)
                {
                    throw new InvalidOperationException("CityPointView must keep hover outline disabled before the player enters the city.");
                }

                entry.ShowCity();
                pointObject.SendMessage("OnMouseEnter", SendMessageOptions.DontRequireReceiver);
                if (!pointView.IsHoverOutlineVisible)
                {
                    throw new InvalidOperationException("CityPointView should enable hover outline after GameEntry switches into the city.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(pointObject);
            }
        }

        private static void SetGameEntryRoots(GameEntry entry, GameObject deskRoot, GameObject cityRoot)
        {
            var serializedObject = new SerializedObject(entry);
            serializedObject.FindProperty("deskRoot").objectReferenceValue = deskRoot;
            serializedObject.FindProperty("cityRoot").objectReferenceValue = cityRoot;
            serializedObject.FindProperty("showDeskOnStart").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
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

        private static void ConfigureRegistry(
            CityPointRegistry registry,
            ConfigManager configManager,
            params CityPointView[] pointViews)
        {
            var serializedObject = new SerializedObject(registry);
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
    }
}
