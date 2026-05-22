using System;
using TwelveMoons.City;
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
                    table.Rows.Count < 4 ||
                    !table.TryFindById("PointId", "city_point_royal_gate", out _))
                {
                    throw new InvalidOperationException("CityPointConfig demo data is missing required point rows.");
                }

                var royalGateView = CreatePointView(root.transform, "RoyalGatePointView", "city_point_royal_gate");
                var churchSquareView = CreatePointView(root.transform, "ChurchSquarePointView", "city_point_church_square");
                var upperMarketView = CreatePointView(root.transform, "UpperMarketPointView", "city_point_upper_market");
                var academyArchiveView = CreatePointView(root.transform, "AcademyArchivePointView", "city_point_academy_archive");

                var registry = root.AddComponent<CityPointRegistry>();
                ConfigureRegistry(
                    registry,
                    configManager,
                    royalGateView,
                    churchSquareView,
                    upperMarketView,
                    academyArchiveView);
                registry.RefreshAndBind();

                if (registry.ConfigCount != table.Rows.Count ||
                    registry.MatchedViewCount != 4 ||
                    !string.IsNullOrEmpty(registry.UnmatchedViewPointIds) ||
                    !registry.UnusedConfigPointIds.Contains("city_point_lower_harbor"))
                {
                    throw new InvalidOperationException("CityPointRegistry failed to match CityPointView PointId values against CityPointConfig.");
                }

                if (!registry.TryGetDefinition("city_point_royal_gate", out var definition) ||
                    definition.PointName != "王宫门前")
                {
                    throw new InvalidOperationException("CityPointRegistry could not resolve the royal gate point definition.");
                }

                if (!registry.TryGetView("city_point_royal_gate", out var view) ||
                    !view.IsMatched ||
                    view.Definition.PointId != "city_point_royal_gate")
                {
                    throw new InvalidOperationException("CityPointView did not receive the matched CityPointDefinition.");
                }

                Debug.Log("City point smoke test passed. CityPointView PointId values match CityPointConfig rows and expose unmatched config IDs for scene completion.");
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
