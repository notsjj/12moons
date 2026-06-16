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
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/桌面面板.prefab");
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

                if (panel.PointerSwingAngle <= 0f || panel.PointerSwingStepDuration <= 0f)
                {
                    throw new InvalidDataException("Suspicion pointer rotation swing parameters must be positive.");
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

                    var targetImage = row.PointerTargetRectTransform.GetComponent<UnityEngine.UI.Image>();
                    if (targetImage == null || targetImage.color.a <= 0f)
                    {
                        throw new InvalidDataException(
                            $"Suspicion row {row.FactionId} pointer target must be a visible row image.");
                    }
                }

                AssertAwakeKeepsDeskRowTargetsSeparated(panel, rows);
                AssertContentLayoutGroupStaysEnabled(panel);
                AssertPointerUsesRightPivot(panel);
                AssertPointerTargetUsesRowVisualCenter(panel);

                Debug.Log("Suspicion pointer smoke test passed. Pointer target rects resolve to the visual row nodes.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void AssertContentLayoutGroupStaysEnabled(SuspicionPanelView panel)
        {
            var contentRootField = typeof(SuspicionPanelView).GetField(
                "contentRoot",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var contentRoot = contentRootField?.GetValue(panel) as RectTransform;
            var layoutGroup = contentRoot != null ? contentRoot.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() : null;
            if (layoutGroup == null)
            {
                throw new InvalidDataException("Suspicion content must keep a VerticalLayoutGroup.");
            }

            if (!layoutGroup.enabled)
            {
                throw new InvalidDataException(
                    "Suspicion content VerticalLayoutGroup must stay enabled so suspicion rows keep separate coordinates.");
            }
        }

        private static void AssertPointerTargetUsesRowVisualCenter(SuspicionPanelView panel)
        {
            var root = new GameObject("PointerCoordinateRoot", typeof(RectTransform));
            try
            {
                var parent = root.GetComponent<RectTransform>();
                parent.sizeDelta = new Vector2(400f, 300f);
                parent.pivot = new Vector2(0.5f, 0.5f);

                var pointer = new GameObject("Pointer", typeof(RectTransform)).GetComponent<RectTransform>();
                pointer.SetParent(parent, false);
                pointer.anchorMin = new Vector2(1f, 0.5f);
                pointer.anchorMax = new Vector2(1f, 0.5f);
                pointer.anchoredPosition = new Vector2(-40f, -120f);

                var row = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
                row.SetParent(parent, false);
                row.anchorMin = new Vector2(1f, 0.5f);
                row.anchorMax = new Vector2(1f, 0.5f);
                row.sizeDelta = new Vector2(160f, 32f);
                row.anchoredPosition = new Vector2(20f, -24f);
                row.pivot = new Vector2(0.5f, 0f);

                typeof(SuspicionPanelView)
                    .GetField("pointerIcon", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(panel, pointer);

                var method = typeof(SuspicionPanelView).GetMethod(
                    "GetPointerTargetPosition",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var targetPosition = method != null
                    ? (Vector2)method.Invoke(panel, new object[] { row })
                    : throw new InvalidDataException("Suspicion pointer target method not found.");

                var expectedY = row.localPosition.y + (row.rect.center.y * row.localScale.y);
                if (!Mathf.Approximately(targetPosition.y, expectedY))
                {
                    throw new InvalidDataException(
                        $"Suspicion pointer target y must match the visual row center. Expected {expectedY}, got {targetPosition.y}.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssertAwakeKeepsDeskRowTargetsSeparated(SuspicionPanelView panel, FactionSuspicionRow[] rows)
        {
            var awakeMethod = typeof(SuspicionPanelView).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (awakeMethod == null)
            {
                throw new InvalidDataException("SuspicionPanelView Awake method not found.");
            }

            awakeMethod.Invoke(panel, null);

            var targetMethod = typeof(SuspicionPanelView).GetMethod(
                "GetPointerTargetPosition",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (targetMethod == null)
            {
                throw new InvalidDataException("Suspicion pointer target method not found.");
            }

            var previousY = float.PositiveInfinity;
            for (var index = 0; index < rows.Length; index++)
            {
                var row = rows[index];
                var targetPosition = (Vector2)targetMethod.Invoke(panel, new object[] { row.PointerTargetRectTransform });
                if (index > 0 && targetPosition.y >= previousY - 1f)
                {
                    throw new InvalidDataException(
                        $"Suspicion row targets must remain vertically separated after Awake. Row {row.FactionId} y={targetPosition.y}, previous y={previousY}.");
                }

                previousY = targetPosition.y;
            }
        }

        private static void AssertPointerUsesRightPivot(SuspicionPanelView panel)
        {
            var pointerField = typeof(SuspicionPanelView).GetField(
                "pointerIcon",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var pointer = pointerField?.GetValue(panel) as RectTransform;
            if (pointer == null || pointer.pivot.x < 0.99f)
            {
                throw new InvalidDataException("Suspicion pointer pivot must be on the right edge for rotation swing.");
            }
        }
    }
}
