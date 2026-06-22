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

            var pointViews = BindNamedScenePointViews(configManager);
            if (pointViews.Count == 0)
            {
                Fail("没有找到任何与 CityPointConfig.PointName 同名的场景建筑物体，未修改点位绑定。");
                return;
            }
            var registry = FindOrCreateRegistry(cityRoot.transform, configManager, pointViews);
            registry.RefreshAndBind();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = registry.gameObject;
            Debug.Log("阶段14城区点位绑定已局部创建：CityPointRegistry 挂在 CityRoot/CityPointRegistry，CityPointView 已按 CityPointConfig.PointName 绑定到同名场景建筑。");
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

        private static List<CityPointView> BindNamedScenePointViews(ConfigManager configManager)
        {
            var pointViews = new List<CityPointView>();
            if (configManager == null || !configManager.TryGetTable("CityPointConfig", out var table))
            {
                Debug.LogError("无法读取 CityPointConfig，不能按点位名称绑定场景建筑。");
                return pointViews;
            }

            foreach (var row in table.Rows)
            {
                var pointId = row.GetString("PointId");
                var pointName = row.GetString("PointName");
                if (string.IsNullOrWhiteSpace(pointId) || string.IsNullOrWhiteSpace(pointName))
                {
                    continue;
                }

                var pointObject = FindSceneObjectByName(pointName.Trim());
                if (pointObject == null)
                {
                    Debug.LogWarning($"CityPointConfig 点位 {pointId}/{pointName} 找不到同名场景建筑物体，已跳过。只会影响该点位支线生成。 ");
                    continue;
                }

                var view = pointObject.GetComponent<CityPointView>() ?? pointObject.AddComponent<CityPointView>();
                view.Configure(pointId);
                pointViews.Add(view);
            }

            return pointViews;
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
