using System.Collections.Generic;
using TwelveMoons.City;
using TwelveMoons.Core;
using TwelveMoons.Core.Config;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwelveMoons.EditorTools
{
    public static class CityPointBindingOnlyBuilder
    {
        [MenuItem("Twelve Moons/Setup/Create City Point Binding Only")]
        public static void CreateCityPointBindingOnly()
        {
            var cityRoot = FindCityRoot();
            if (cityRoot == null)
            {
                Fail("找不到 CityRoot。本工具只做阶段14城区点位局部绑定，不会创建或重建其它界面。");
                return;
            }

            var configManager = Object.FindFirstObjectByType<ConfigManager>(FindObjectsInactive.Include);
            if (configManager == null)
            {
                Fail("找不到 ConfigManager。请先保留已有配置管理器，并确认它读取 Configs/Demo 或正式配置目录。");
                return;
            }

            var worldParent = FindCityWorldParent();
            var pointsRoot = FindOrCreatePointsRoot(worldParent);
            var pointViews = CreateDemoPointViews(pointsRoot.transform);
            var registry = FindOrCreateRegistry(cityRoot.transform, configManager, pointViews);
            registry.RefreshAndBind();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = registry.gameObject;
            Debug.Log("阶段14城区点位绑定已局部创建：CityPointRegistry 挂在 CityRoot/CityPointRegistry，CityPointView 挂在 CityPointViews 下的点位空物体。");
        }

        private static GameObject FindCityRoot()
        {
            var gameEntry = Object.FindFirstObjectByType<GameEntry>(FindObjectsInactive.Include);
            if (gameEntry != null && gameEntry.CityRoot != null)
            {
                return gameEntry.CityRoot;
            }

            return GameObject.Find("CityRoot");
        }

        private static Transform FindCityWorldParent()
        {
            var cityMap = FindSceneObjectByName("城区地图_01") ?? FindSceneObjectByName("City_01");
            if (cityMap != null)
            {
                return cityMap.transform.parent;
            }

            var fallbackRoot = GameObject.Find("CityWorldRoot");
            if (fallbackRoot == null)
            {
                fallbackRoot = new GameObject("CityWorldRoot");
                Debug.LogWarning("找不到 3D 地图 City_01，已创建 CityWorldRoot 承载城区点位空物体。");
            }

            return fallbackRoot.transform;
        }

        private static GameObject FindOrCreatePointsRoot(Transform worldParent)
        {
            var existing = FindSceneObjectByName("CityPointViews");
            if (existing == null)
            {
                existing = new GameObject("CityPointViews");
            }

            if (existing.transform.parent != worldParent)
            {
                existing.transform.SetParent(worldParent, false);
            }

            return existing;
        }

        private static List<CityPointView> CreateDemoPointViews(Transform parent)
        {
            return new List<CityPointView>
            {
                CreatePointView(parent, "RoyalGatePoint", "city_point_royal_gate", new Vector3(-1.4f, 0f, 1.4f)),
                CreatePointView(parent, "ChurchSquarePoint", "city_point_church_square", new Vector3(-3.6f, 0f, 0.4f)),
                CreatePointView(parent, "UpperMarketPoint", "city_point_upper_market", new Vector3(2.6f, 0f, 0.8f)),
                CreatePointView(parent, "AcademyArchivePoint", "city_point_academy_archive", new Vector3(0.4f, 0f, 2.8f)),
                CreatePointView(parent, "LowerHarborPoint", "city_point_lower_harbor", new Vector3(0f, 0f, -2.4f))
            };
        }

        private static CityPointView CreatePointView(Transform parent, string objectName, string pointId, Vector3 localPosition)
        {
            var point = parent.Find(objectName);
            if (point == null)
            {
                point = new GameObject(objectName).transform;
                point.SetParent(parent, false);
            }

            point.localPosition = localPosition;
            var view = point.GetComponent<CityPointView>() ?? point.gameObject.AddComponent<CityPointView>();
            view.Configure(pointId);
            return view;
        }

        private static CityPointRegistry FindOrCreateRegistry(
            Transform cityRoot,
            ConfigManager configManager,
            IReadOnlyList<CityPointView> pointViews)
        {
            var registryTransform = cityRoot.Find("CityPointRegistry");
            if (registryTransform == null)
            {
                registryTransform = new GameObject("CityPointRegistry").transform;
                registryTransform.SetParent(cityRoot, false);
            }

            var registry = registryTransform.GetComponent<CityPointRegistry>() ??
                registryTransform.gameObject.AddComponent<CityPointRegistry>();

            var serializedObject = new SerializedObject(registry);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("bindOnStart").boolValue = true;
            serializedObject.FindProperty("autoCollectSceneViews").boolValue = false;

            var viewsProperty = serializedObject.FindProperty("pointViews");
            viewsProperty.arraySize = pointViews.Count;
            for (var index = 0; index < pointViews.Count; index++)
            {
                viewsProperty.GetArrayElementAtIndex(index).objectReferenceValue = pointViews[index];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return registry;
        }

        private static GameObject FindSceneObjectByName(string objectName)
        {
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in transforms)
            {
                if (candidate != null && candidate.name == objectName)
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private static void Fail(string message)
        {
            Debug.LogError(message);
            EditorUtility.DisplayDialog("Create City Point Binding Only", message, "OK");
        }
    }
}
