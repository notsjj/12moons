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
            LetterSmokeTest.Run();
            StorySmokeTest.Run();
            DocumentSmokeTest.Run();
            DeskLoopSmokeTest.Run();
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

                if (controller.EntryOrbitDuration < 4f)
                {
                    throw new InvalidDataException($"City entry orbit is too fast: {controller.EntryOrbitDuration} seconds.");
                }

                if (!Mathf.Approximately(controller.EntryOrbitDegrees, 360f))
                {
                    throw new InvalidDataException($"City entry orbit is not a full circle: {controller.EntryOrbitDegrees} degrees.");
                }

                if (controller.EntryCinematicEndViewId != "city_upper")
                {
                    throw new InvalidDataException($"City entry cinematic must end at upper city view, got {controller.EntryCinematicEndViewId}.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
