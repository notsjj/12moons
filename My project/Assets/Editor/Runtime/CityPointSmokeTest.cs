using System;
using System.Collections.Generic;
using System.IO;
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
                ValidateBaseSceneHasAllPlotPointViews();
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
                ValidatePointEventPromptVisibility();
                ValidateBuildingActorPointFacesCamera();
                ValidateNoRuntimeMarkerGeneration();

                Debug.Log("City point smoke test passed. All P0001-P0014 CityPointConfig ids match CityPointView instances, with no missing or duplicate point ids.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ValidateBaseSceneHasAllPlotPointViews()
        {
            var providerRoot = Path.Combine(Application.dataPath, "StreamingAssets", "Configs", "Plot");
            var csvProvider = new CsvConfigProvider(providerRoot);
            var pointTable = csvProvider.LoadTable("CityPointConfig");
            var requiredPointIds = pointTable.Rows
                .Select(row => row.GetString("PointId"))
                .Where(IsPointId)
                .OrderBy(id => id)
                .ToArray();
            var sceneText = File.ReadAllText("Assets/Scenes/BaseScene.unity");
            var missingIds = requiredPointIds
                .Where(pointId => !sceneText.Contains($"pointId: {pointId}"))
                .ToArray();
            if (missingIds.Length > 0)
            {
                throw new InvalidOperationException("BaseScene is missing CityPointView point ids from Plot CityPointConfig: " + string.Join(", ", missingIds));
            }

            var duplicateIds = requiredPointIds
                .Where(pointId => sceneText.Split(new[] { $"pointId: {pointId}" }, StringSplitOptions.None).Length - 1 != 1)
                .ToArray();
            if (duplicateIds.Length > 0)
            {
                throw new InvalidOperationException("BaseScene must contain exactly one CityPointView for each Plot CityPointConfig id: " + string.Join(", ", duplicateIds));
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


        private static void ValidatePointEventPromptVisibility()
        {
            var root = new GameObject("CityPointEventPromptRoot");
            try
            {
                var pointView = root.AddComponent<CityPointView>();
                pointView.Configure("P0001");
                var portraitRoot = new GameObject("\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d");
                portraitRoot.transform.SetParent(root.transform, false);
                var model = new GameObject("Man", typeof(SpriteRenderer));
                model.transform.SetParent(portraitRoot.transform, false);
                var modelRenderer = model.GetComponent<SpriteRenderer>();
                modelRenderer.enabled = true;
                var prompt = new GameObject("\u4e8b\u4ef6\u63d0\u793a", typeof(SpriteRenderer));
                prompt.transform.SetParent(portraitRoot.transform, false);
                prompt.SetActive(true);

                pointView.RefreshPortraitDisplay();
                if (pointView.IsEventPromptVisible || prompt.activeSelf)
                {
                    throw new InvalidOperationException("CityPointView must keep the event prompt hidden when no event is bound.");
                }

                pointView.BindSideEvent(CreateSideEventDefinition("SE_TEST", "P0001", "S0001"), null);
                if (!pointView.IsEventPromptVisible || !prompt.activeSelf)
                {
                    throw new InvalidOperationException("CityPointView must show the event prompt when an event is bound.");
                }

                if (!model.activeSelf || !modelRenderer.enabled)
                {
                    throw new InvalidOperationException("CityPointView must keep the building actor point model visible while only hiding generated character portrait sprites.");
                }

                pointView.ClearSideEventBinding();
                if (pointView.IsEventPromptVisible || prompt.activeSelf)
                {
                    throw new InvalidOperationException("CityPointView must hide the event prompt after the event binding is cleared.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ValidateBuildingActorPointFacesCamera()
        {
            var root = new GameObject("CityPointBuildingActorBillboardRoot");
            var cameraObject = new GameObject("CityPointBuildingActorBillboardCamera", typeof(Camera));
            try
            {
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);

                var pointView = root.AddComponent<CityPointView>();
                pointView.Configure("P0001");

                var marker = new GameObject("\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d");
                marker.transform.SetParent(root.transform, false);
                marker.transform.position = Vector3.zero;
                marker.transform.rotation = Quaternion.identity;

                var model = new GameObject("Man", typeof(SpriteRenderer));
                model.transform.SetParent(marker.transform, false);

                typeof(CityPointView)
                    .GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(pointView, null);

                var expected = Quaternion.LookRotation(cameraObject.transform.position - marker.transform.position, Vector3.up);
                if (Quaternion.Angle(marker.transform.rotation, expected) > 0.1f)
                {
                    throw new InvalidOperationException("CityPointView must rotate the building actor point root toward MainCamera even when no generated portrait renderer exists.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void ValidateNoRuntimeMarkerGeneration()
        {
            var root = new GameObject("CityPointNoRuntimeMarkerGenerationRoot");
            try
            {
                var pointView = root.AddComponent<CityPointView>();
                pointView.Configure("P0001");
                pointView.BindSideEvent(CreateSideEventDefinition("SE_TEST", "P0001", "S0001"), null);
                if (root.transform.Find("\u5efa\u7b51\u4eba\u7269\u70b9\u4f4d") != null)
                {
                    throw new InvalidOperationException("CityPointView must not create building actor point children at runtime; they must be placed in the scene for manual adjustment.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
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

        private static SideEventDefinition CreateSideEventDefinition(string sideEventId, string pointId, string storyId)
        {
            return new SideEventDefinition(new ConfigRow(new Dictionary<string, string>
            {
                { "SideEventId", sideEventId },
                { "Round", "1" },
                { "PointId", pointId },
                { "DisplayCharacterId", "C0001" },
                { "StoryId", storyId },
                { "ExpireRound", "1" },
                { "IsOneTime", "1" },
                { "RequiredTaskId", string.Empty },
                { "RequiredTaskState", string.Empty },
                { "RequiredItemId", string.Empty },
                { "RequiredItemCount", "0" },
                { "Remark", string.Empty }
            }));
        }

        private static bool IsPointId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Length == 5 &&
                   value[0] == 'P' &&
                   value.Skip(1).All(char.IsDigit);
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
