using System.IO;
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
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/LoadingPanel.prefab");
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
                    Object.DestroyImmediate(bootstrapObject);
                }

                Debug.Log("Loading panel smoke test passed. Transition layers resolve into hierarchy-ordered groups with reversed exit order.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
