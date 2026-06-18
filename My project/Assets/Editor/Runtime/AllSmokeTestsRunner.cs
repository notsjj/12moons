using System;
using System.IO;
using TwelveMoons.City;
using TwelveMoons.EditorTools.Config;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class AllSmokeTestsRunner
    {
        [MenuItem("Twelve Moons/Tests/Run All Core Smoke Tests")]
        public static void Run()
        {
            ConfigLoaderSmokeTest.Run();
            RuntimeDataSmokeTest.Run();
            InventorySmokeTest.Run();
            FactionSmokeTest.Run();
            RoundSmokeTest.Run();
            TaskSmokeTest.Run();
            TaskPanelCollapseSmokeTest.Run();
            LetterSmokeTest.Run();
            StorySmokeTest.Run();
            DocumentSmokeTest.Run();
            DeskLoopSmokeTest.Run();
            DeskLoopButtonBindingSmokeTest.Run();
            DeskLoopControllerPresenceSmokeTest.Run();
            CityPointSmokeTest.Run();
            CityBuildingSmokeTest.Run();
            CitySideEventSmokeTest.Run();
            BaseSceneUIFrameworkSmokeTest.Run();
            DeskPanelVisualSmokeTest.Run();
            SuspicionPointerSmokeTest.Run();
            ValidateCityCameraEntryCinematic();

            Debug.Log("All core smoke tests passed.");
        }

        private static void ValidateCityCameraEntryCinematic()
        {
            var root = new GameObject("CityCameraEntryCinematicSmokeTest");
            try
            {
                root.AddComponent<Camera>();
                var controller = root.AddComponent<CityCameraController>();
                if (controller.EntryUsesZoom)
                {
                    throw new InvalidDataException("City entry cinematic still uses zoom.");
                }

                if (!controller.DefaultViewUsesExactTransform)
                {
                    throw new InvalidDataException("City camera default view must copy the exact GlobalViewPoint transform.");
                }

                if (!controller.ApplyDefaultViewOnStart)
                {
                    throw new InvalidDataException("City camera must apply the default GlobalViewPoint again on Start for packaged-player startup order.");
                }

                if (controller.EntryOrbitDuration <= 0f)
                {
                    throw new InvalidDataException($"City entry camera move duration must be positive, got {controller.EntryOrbitDuration} seconds.");
                }

                if (!Mathf.Approximately(controller.EntryOrbitDegrees, 0f))
                {
                    throw new InvalidDataException($"City entry camera should move between view points instead of orbiting, got {controller.EntryOrbitDegrees} degrees.");
                }

                if (controller.EntryCinematicEndObjectName != "GlobalViewPoint (1)")
                {
                    throw new InvalidDataException($"City entry cinematic must end at GlobalViewPoint (1), got {controller.EntryCinematicEndObjectName}.");
                }

                var source = File.ReadAllText("Assets/Scripts/City/CityCameraController.cs");
                if (source.Contains("GameObject.Find(") ||
                    !source.Contains("FindObjectsByType<Transform>(FindObjectsInactive.Include"))
                {
                    throw new InvalidDataException("CityCameraController must resolve view points with build-safe inactive-scene lookup instead of GameObject.Find.");
                }

                var onEnableIndex = source.IndexOf("private void OnEnable()", StringComparison.Ordinal);
                var updateIndex = source.IndexOf("private void Update()", StringComparison.Ordinal);
                var onEnableSource = onEnableIndex >= 0 && updateIndex > onEnableIndex
                    ? source.Substring(onEnableIndex, updateIndex - onEnableIndex)
                    : string.Empty;
                if (onEnableSource.Contains("JumpToDefaultView()", StringComparison.Ordinal))
                {
                    throw new InvalidDataException("CityCameraController must not reset to default view from OnEnable, otherwise packaged activation order can overwrite camera moves.");
                }

                if (!source.Contains("DisableCompetingCameras") ||
                    !source.Contains("camera.enabled = false"))
                {
                    throw new InvalidDataException("CityCameraController must disable imported full-screen cameras so packaged builds render through Main Camera.");
                }

                var scene = File.ReadAllText("Assets/Scenes/BaseScene.unity");
                if (!scene.Contains("applyDefaultViewOnStart: 1"))
                {
                    throw new InvalidDataException("BaseScene Main Camera must keep CityCameraController startup default alignment enabled.");
                }

                var mapMeta = File.ReadAllText("Assets/Resources/Art/Map.fbx.meta");
                var map01Meta = File.ReadAllText("Assets/Resources/Art/Map_01.fbx.meta");
                if (mapMeta.Contains("importCameras: 1") || map01Meta.Contains("importCameras: 1"))
                {
                    throw new InvalidDataException("City map FBX assets must not import embedded cameras into packaged builds.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
