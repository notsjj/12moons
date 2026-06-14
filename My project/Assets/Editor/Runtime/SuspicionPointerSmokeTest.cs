using System.IO;
using System.Reflection;
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

                if (!Mathf.Approximately(panel.PointerShakeDuration, 2f))
                {
                    throw new InvalidDataException($"Suspicion pointer shake duration must be 2 seconds, got {panel.PointerShakeDuration}.");
                }

                if (panel.PointerShakeDistance <= 0f || panel.PointerShakeStepDuration <= 0f)
                {
                    throw new InvalidDataException("Suspicion pointer shake parameters must be positive.");
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

                AssertPointerTargetUsesAnchoredPosition(panel);

                Debug.Log("Suspicion pointer smoke test passed. Pointer target rects resolve to the visual row nodes.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void AssertPointerTargetUsesAnchoredPosition(SuspicionPanelView panel)
        {
            var root = new GameObject("PointerCoordinateRoot", typeof(RectTransform));
            try
            {
                var parent = root.GetComponent<RectTransform>();
                parent.sizeDelta = new Vector2(400f, 300f);
                parent.pivot = new Vector2(0.5f, 0.5f);

                var pointer = new GameObject("Pointer", typeof(RectTransform)).GetComponent<RectTransform>();
                pointer.SetParent(parent, false);
                pointer.anchorMin = new Vector2(1f, 1f);
                pointer.anchorMax = new Vector2(1f, 1f);
                pointer.anchoredPosition = new Vector2(-40f, -120f);

                var row = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
                row.SetParent(parent, false);
                row.anchorMin = new Vector2(0.5f, 0.5f);
                row.anchorMax = new Vector2(0.5f, 0.5f);
                row.sizeDelta = new Vector2(160f, 32f);
                row.anchoredPosition = new Vector2(20f, -24f);

                typeof(SuspicionPanelView)
                    .GetField("pointerIcon", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(panel, pointer);

                var method = typeof(SuspicionPanelView).GetMethod(
                    "GetPointerTargetPosition",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var targetPosition = method != null
                    ? (Vector2)method.Invoke(panel, new object[] { row })
                    : throw new InvalidDataException("Suspicion pointer target method not found.");

                var anchorReferenceY = (pointer.anchorMin.y - parent.pivot.y) * parent.rect.height;
                var expectedY = row.anchoredPosition.y - anchorReferenceY;
                if (!Mathf.Approximately(targetPosition.y, expectedY))
                {
                    throw new InvalidDataException(
                        $"Suspicion pointer target y must use anchoredPosition space. Expected {expectedY}, got {targetPosition.y}.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
