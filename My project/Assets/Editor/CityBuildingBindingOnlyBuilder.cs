using TwelveMoons.City;
using TwelveMoons.Core;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwelveMoons.EditorTools
{
    public static class CityBuildingBindingOnlyBuilder
    {
        [MenuItem("Twelve Moons/Setup/Create Building Binding Only")]
        public static void CreateBuildingBindingOnly()
        {
            var cityRoot = FindCityRoot();
            if (cityRoot == null)
            {
                Fail("找不到 CityRoot。本工具只做阶段15建筑绑定局部创建，不会创建或重建其它界面。");
                return;
            }

            var configManager = Object.FindFirstObjectByType<ConfigManager>(FindObjectsInactive.Include);
            var runtimeDataService = Object.FindFirstObjectByType<RuntimeDataService>(FindObjectsInactive.Include);
            var inventoryService = Object.FindFirstObjectByType<InventoryService>(FindObjectsInactive.Include);
            var factionService = Object.FindFirstObjectByType<FactionService>(FindObjectsInactive.Include);
            if (configManager == null || runtimeDataService == null)
            {
                Fail("缺少 ConfigManager 或 RuntimeDataService。请先保留已有基础场景服务，再运行建筑绑定局部工具。");
                return;
            }

            var service = FindOrCreateService(cityRoot.transform, configManager, runtimeDataService, inventoryService, factionService);
            var registry = FindOrCreateRegistry(cityRoot.transform, service, configManager);
            FindOrCreateDebugControls(cityRoot.transform, configManager, runtimeDataService, service, registry);
            registry.RefreshAndBind();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = registry.gameObject;
            Debug.Log("阶段15建筑绑定已局部创建：CityBuildingService 与 CityBuildingRegistry 挂在 CityRoot 下；请把 CityBuildingView 手动挂到实际建筑模型子物体。");
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

        private static CityBuildingService FindOrCreateService(
            Transform cityRoot,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService,
            InventoryService inventoryService,
            FactionService factionService)
        {
            var serviceTransform = cityRoot.Find("CityBuildingService");
            if (serviceTransform == null)
            {
                serviceTransform = new GameObject("CityBuildingService").transform;
                serviceTransform.SetParent(cityRoot, false);
            }

            var service = serviceTransform.GetComponent<CityBuildingService>() ??
                serviceTransform.gameObject.AddComponent<CityBuildingService>();

            var serializedObject = new SerializedObject(service);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("inventoryService").objectReferenceValue = inventoryService;
            serializedObject.FindProperty("factionService").objectReferenceValue = factionService;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return service;
        }

        private static CityBuildingRegistry FindOrCreateRegistry(
            Transform cityRoot,
            CityBuildingService service,
            ConfigManager configManager)
        {
            var registryTransform = cityRoot.Find("CityBuildingRegistry");
            if (registryTransform == null)
            {
                registryTransform = new GameObject("CityBuildingRegistry").transform;
                registryTransform.SetParent(cityRoot, false);
            }

            var registry = registryTransform.GetComponent<CityBuildingRegistry>() ??
                registryTransform.gameObject.AddComponent<CityBuildingRegistry>();

            var serializedObject = new SerializedObject(registry);
            serializedObject.FindProperty("buildingService").objectReferenceValue = service;
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("bindOnStart").boolValue = true;
            serializedObject.FindProperty("autoCollectSceneViews").boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return registry;
        }

        private static MonoBehaviour FindOrCreateDebugControls(
            Transform cityRoot,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService,
            CityBuildingService service,
            CityBuildingRegistry registry)
        {
            var debugTransform = cityRoot.Find("CityBuildingDebugControls");
            if (debugTransform == null)
            {
                debugTransform = new GameObject("CityBuildingDebugControls").transform;
                debugTransform.SetParent(cityRoot, false);
            }

            var debugType = System.Type.GetType("TwelveMoons.City.CityBuildingDebugControls, Assembly-CSharp");
            if (debugType == null)
            {
                Debug.LogWarning("找不到 CityBuildingDebugControls 类型；Unity 刷新脚本后重新运行本工具会自动补上调试组件。");
                return null;
            }

            var debugControls = debugTransform.GetComponent(debugType) as MonoBehaviour ??
                debugTransform.gameObject.AddComponent(debugType) as MonoBehaviour;
            if (debugControls == null)
            {
                Debug.LogWarning("无法创建 CityBuildingDebugControls 调试组件。");
                return null;
            }

            var documentService = Object.FindFirstObjectByType<DocumentService>(FindObjectsInactive.Include);
            var serializedObject = new SerializedObject(debugControls);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("documentService").objectReferenceValue = documentService;
            serializedObject.FindProperty("buildingService").objectReferenceValue = service;
            serializedObject.FindProperty("buildingRegistry").objectReferenceValue = registry;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return debugControls;
        }

        private static void Fail(string message)
        {
            Debug.LogError(message);
            EditorUtility.DisplayDialog("Create Building Binding Only", message, "OK");
        }
    }
}
