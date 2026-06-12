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
            DeskPanelAtmosphereSmokeTest.Run();
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

                if (controller.EntryOrbitDuration < 4f)
                {
                    throw new InvalidDataException($"City entry orbit is too fast: {controller.EntryOrbitDuration} seconds.");
                }

                if (!Mathf.Approximately(controller.EntryOrbitDegrees, 360f))
                {
                    throw new InvalidDataException($"City entry orbit is not a full circle: {controller.EntryOrbitDegrees} degrees.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
