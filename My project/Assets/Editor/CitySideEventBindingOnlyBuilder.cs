using TwelveMoons.City;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwelveMoons.EditorTools
{
    public static class CitySideEventBindingOnlyBuilder
    {
        [MenuItem("Twelve Moons/Setup/Create City Side Event Binding Only")]
        public static void CreateCitySideEventBindingOnly()
        {
            var cityRoot = GameObject.Find("CityRoot");
            if (cityRoot == null)
            {
                Fail("找不到 CityRoot。本工具只做阶段16支线事件局部绑定，不会创建或重建桌面 UI。");
                return;
            }

            var configManager = Object.FindFirstObjectByType<ConfigManager>(FindObjectsInactive.Include);
            var runtimeDataService = Object.FindFirstObjectByType<RuntimeDataService>(FindObjectsInactive.Include);
            var storyService = Object.FindFirstObjectByType<StoryService>(FindObjectsInactive.Include);
            var taskService = Object.FindFirstObjectByType<TaskService>(FindObjectsInactive.Include);
            var inventoryService = Object.FindFirstObjectByType<InventoryService>(FindObjectsInactive.Include);
            var pointRegistry = Object.FindFirstObjectByType<CityPointRegistry>(FindObjectsInactive.Include);

            if (configManager == null || runtimeDataService == null || storyService == null || pointRegistry == null)
            {
                Fail("缺少 ConfigManager、RuntimeDataService、StoryService 或 CityPointRegistry。请先完成并保留前置阶段对象。");
                return;
            }

            var service = FindOrCreateService(
                cityRoot.transform,
                configManager,
                runtimeDataService,
                storyService,
                taskService,
                inventoryService);
            var registry = FindOrCreateRegistry(cityRoot.transform, service, pointRegistry);
            service.Refresh();
            registry.RefreshAndBind();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = registry.gameObject;
            Debug.Log("阶段16支线事件绑定已局部创建：CitySideEventService 和 CitySideEventRegistry 挂在 CityRoot 下，支线角色会按 SideEventConfig 生成到 CityPointView 点位。");
        }

        private static CitySideEventService FindOrCreateService(
            Transform cityRoot,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService,
            StoryService storyService,
            TaskService taskService,
            InventoryService inventoryService)
        {
            var serviceTransform = cityRoot.Find("CitySideEventService");
            if (serviceTransform == null)
            {
                serviceTransform = new GameObject("CitySideEventService").transform;
                serviceTransform.SetParent(cityRoot, false);
            }

            var service = serviceTransform.GetComponent<CitySideEventService>() ??
                serviceTransform.gameObject.AddComponent<CitySideEventService>();

            var serializedObject = new SerializedObject(service);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("storyService").objectReferenceValue = storyService;
            serializedObject.FindProperty("taskService").objectReferenceValue = taskService;
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return service;
        }

        private static CitySideEventRegistry FindOrCreateRegistry(
            Transform cityRoot,
            CitySideEventService service,
            CityPointRegistry pointRegistry)
        {
            var registryTransform = cityRoot.Find("CitySideEventRegistry");
            if (registryTransform == null)
            {
                registryTransform = new GameObject("CitySideEventRegistry").transform;
                registryTransform.SetParent(cityRoot, false);
            }

            var registry = registryTransform.GetComponent<CitySideEventRegistry>() ??
                registryTransform.gameObject.AddComponent<CitySideEventRegistry>();

            var viewRoot = registryTransform.Find("SideEventViews");
            if (viewRoot == null)
            {
                viewRoot = new GameObject("SideEventViews").transform;
                viewRoot.SetParent(registryTransform, false);
            }

            var serializedObject = new SerializedObject(registry);
            serializedObject.FindProperty("sideEventService").objectReferenceValue = service;
            serializedObject.FindProperty("pointRegistry").objectReferenceValue = pointRegistry;
            serializedObject.FindProperty("sideEventViewRoot").objectReferenceValue = viewRoot;
            serializedObject.FindProperty("createMissingViews").boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return registry;
        }

        private static void Fail(string message)
        {
            Debug.LogError(message);
            EditorUtility.DisplayDialog("Create City Side Event Binding Only", message, "OK");
        }
    }
}
