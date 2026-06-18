using System.IO;
using System;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class LoadingPanelSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run Loading Panel Smoke Test")]
        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/加载过场面板.prefab");
            if (prefab == null)
            {
                throw new InvalidDataException("LoadingPanel prefab not found.");
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                throw new InvalidDataException("Unable to instantiate LoadingPanel prefab.");
            }

            try
            {
                var view = instance.GetComponent<LoadingPanelTransitionView>();
                if (view == null)
                {
                    view = instance.AddComponent<LoadingPanelTransitionView>();
                }

                view.EditorForceInitialize();
                if (view.ResolvedLayerCount != 7)
                {
                    throw new InvalidDataException($"LoadingPanel resolved {view.ResolvedLayerCount} layers instead of 7.");
                }

                if (view.ResolvedLeftLayerCount <= 0 || view.ResolvedRightLayerCount <= 0)
                {
                    throw new InvalidDataException("LoadingPanel did not resolve both left and right transition groups.");
                }

                if (view.ResolvedGroupCount != 4)
                {
                    throw new InvalidDataException($"LoadingPanel resolved {view.ResolvedGroupCount} groups instead of 4.");
                }

                if (view.EnterGroupOrderSnapshot != "0,1,2,3")
                {
                    throw new InvalidDataException($"Unexpected enter group order: {view.EnterGroupOrderSnapshot}");
                }

                if (view.ExitGroupOrderSnapshot != "3,2,1,0")
                {
                    throw new InvalidDataException($"Unexpected exit group order: {view.ExitGroupOrderSnapshot}");
                }

                if (!Mathf.Approximately(view.CoveredHoldDuration, 1f))
                {
                    throw new InvalidDataException($"LoadingPanel covered hold duration is {view.CoveredHoldDuration} instead of 1 second.");
                }

                var synchronizedTransitionOverload = typeof(LoadingPanelTransitionView).GetMethod(
                    "PlayEnterCityTransition",
                    new[] { typeof(Action), typeof(float), typeof(Action) });
                if (synchronizedTransitionOverload == null)
                {
                    throw new InvalidDataException("LoadingPanel must expose an enter-city transition overload whose covered hold duration can be synchronized with the city camera move duration.");
                }

                var synchronizedFromStartOverload = typeof(LoadingPanelTransitionView).GetMethod(
                    "PlayEnterCityTransitionSynchronized",
                    new[] { typeof(Action), typeof(float), typeof(Action) });
                if (synchronizedFromStartOverload == null)
                {
                    throw new InvalidDataException("LoadingPanel must expose a synchronized enter-city transition that starts the camera at panel open and finishes panel close at the same duration.");
                }

                var deskLoopSource = System.IO.File.ReadAllText("Assets/Scripts/UI/DeskLoopController.cs");
                if (!deskLoopSource.Contains("PlayEnterCityTransitionSynchronized") ||
                    deskLoopSource.Contains("GetEntryCameraCoveredHoldDuration"))
                {
                    throw new InvalidDataException("DeskLoopController must start the city camera when LoadingPanel opens and must not keep the old covered-hold wait for camera movement.");
                }

                if (!deskLoopSource.Contains("synchronizedEntryTransitionDuration"))
                {
                    throw new InvalidDataException("DeskLoopController must expose the synchronized city entry transition duration in Inspector.");
                }

                var cameraSource = System.IO.File.ReadAllText("Assets/Scripts/City/CityCameraController.cs");
                if (!cameraSource.Contains("PlayEntryCinematic(float durationOverride"))
                {
                    throw new InvalidDataException("CityCameraController must allow DeskLoopController to drive entry camera duration from the synchronized transition setting.");
                }

                var bootstrapObject = new GameObject("LoadingPanelDebugHotkeySmokeTest");
                try
                {
                    var bootstrap = bootstrapObject.AddComponent<BaseSceneUIBootstrap>();
                    if (!bootstrap.IsLoadingPanelDebugHotkeyEnabled)
                    {
                        throw new InvalidDataException("BaseSceneUIBootstrap does not expose the enabled LoadingPanel P-key debug capability.");
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(bootstrapObject);
                }

                Debug.Log("Loading panel smoke test passed. Transition layers resolve into hierarchy-ordered groups with reversed exit order.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
