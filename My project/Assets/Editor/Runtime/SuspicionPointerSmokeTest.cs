using System.IO;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class SuspicionPointerSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run Suspicion Pointer Smoke Test")]
        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/DeskPanel.prefab");
            if (prefab == null)
            {
                throw new InvalidDataException("DeskPanel prefab not found.");
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                throw new InvalidDataException("Unable to instantiate DeskPanel prefab.");
            }

            try
            {
                var panel = instance.GetComponentInChildren<SuspicionPanelView>(true);
                if (panel == null)
                {
                    throw new InvalidDataException("SuspicionPanelView not found.");
                }

                var rowsField = typeof(SuspicionPanelView).GetField(
                    "factionRows",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var rows = rowsField?.GetValue(panel) as FactionSuspicionRow[];
                if (rows == null || rows.Length < 4)
                {
                    throw new InvalidDataException("Suspicion rows not configured.");
                }

                foreach (var row in rows)
                {
                    if (row == null || row.PointerTargetRectTransform == null)
                    {
                        throw new InvalidDataException("Suspicion row pointer target is missing.");
                    }

                    if (row.PointerTargetRectTransform == row.RectTransform)
                    {
                        throw new InvalidDataException($"Suspicion row {row.FactionId} still points to the layout shell instead of the visual node.");
                    }
                }

                Debug.Log("Suspicion pointer smoke test passed. Pointer target rects resolve to the visual row nodes.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
