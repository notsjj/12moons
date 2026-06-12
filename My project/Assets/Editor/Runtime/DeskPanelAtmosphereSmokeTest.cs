using System.IO;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class DeskPanelAtmosphereSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run DeskPanel Atmosphere Smoke Test")]
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
                var atmosphere = instance.GetComponent<DeskPanelAtmosphereView>();
                if (atmosphere == null)
                {
                    throw new InvalidDataException("DeskPanelAtmosphereView not found.");
                }

                var childCountBeforeSetup = instance.transform.childCount;
                if (atmosphere.VignetteImage == null ||
                    atmosphere.CandleRect == null)
                {
                    throw new InvalidDataException("DeskPanel atmosphere references must be serialized in the prefab.");
                }

                atmosphere.EnsureSetup();
                if (instance.transform.childCount != childCountBeforeSetup)
                {
                    throw new InvalidDataException("DeskPanel atmosphere setup created runtime GameObjects.");
                }

                atmosphere.ApplyVisualSettings();
                if (atmosphere.OverallDimAlpha <= 0f)
                {
                    throw new InvalidDataException("DeskPanel does not lower the overall screen brightness.");
                }

                if (atmosphere.LightTargetCount <= 0)
                {
                    throw new InvalidDataException("DeskPanel atmosphere has no inspector-assigned light targets.");
                }

                if (atmosphere.LightEdgeSoftness < 0.35f)
                {
                    throw new InvalidDataException("DeskPanel light target edge is too sharp.");
                }

                if (!atmosphere.UsesRectangularVignette)
                {
                    throw new InvalidDataException("DeskPanel vignette is not using the rectangular edge-shadow mode.");
                }

                ValidateVignette(instance.transform, atmosphere);
                ValidateCandleLightTargets(instance.transform, atmosphere);
                ValidateHoverShakeBadge(instance.transform, "徽章");
                ValidateHoverShakeBadge(instance.transform, "徽章 (1)");
                Debug.Log("DeskPanel atmosphere smoke test passed. Vignette light targets are configured without candle glow or UI input blocking.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void ValidateVignette(Transform deskPanel, DeskPanelAtmosphereView atmosphere)
        {
            var vignette = deskPanel.Find("桌面暗角") as RectTransform;
            if (vignette == null || atmosphere.VignetteImage == null)
            {
                throw new InvalidDataException("DeskPanel vignette is missing or not bound.");
            }

            if (atmosphere.VignetteImage.raycastTarget)
            {
                throw new InvalidDataException("DeskPanel vignette blocks UI raycasts.");
            }

            if (vignette.anchorMin != Vector2.zero || vignette.anchorMax != Vector2.one ||
                vignette.offsetMin != Vector2.zero || vignette.offsetMax != Vector2.zero)
            {
                throw new InvalidDataException("DeskPanel vignette does not stretch across the full panel.");
            }
        }

        private static void ValidateCandleLightTargets(Transform deskPanel, DeskPanelAtmosphereView atmosphere)
        {
            var candle = deskPanel.Find("蜡烛");
            var glow = deskPanel.Find("蜡烛光晕");
            if (candle == null || atmosphere.CandleRect == null)
            {
                throw new InvalidDataException("Candle is missing or not bound.");
            }

            if (glow != null)
            {
                throw new InvalidDataException("Legacy candle glow object must be removed.");
            }

            if (!atmosphere.ContainsLightTarget(candle as RectTransform))
            {
                throw new InvalidDataException("Candle must be included in the inspector-assigned light targets.");
            }
        }

        private static void ValidateHoverShakeBadge(Transform deskPanel, string badgeName)
        {
            var badge = FindChild(deskPanel, badgeName);
            if (badge == null)
            {
                throw new InvalidDataException($"DeskPanel badge is missing: {badgeName}");
            }

            if (badge.GetComponent<DeskBadgeHoverShake>() == null)
            {
                throw new InvalidDataException($"DeskPanel badge hover shake is missing: {badgeName}");
            }

            var image = badge.GetComponent<Image>();
            if (image == null || !image.raycastTarget)
            {
                throw new InvalidDataException($"DeskPanel badge cannot receive pointer hover events: {badgeName}");
            }
        }

        private static Transform FindChild(Transform root, string childName)
        {
            if (root.name == childName)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                var result = FindChild(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
