using System.IO;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class DeskPanelVisualSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run DeskPanel Visual Smoke Test")]
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
                ValidateAtmosphereRemoved(instance);
                ValidateHoverShakeBadge(instance.transform, "徽章");
                ValidateHoverShakeBadge(instance.transform, "徽章 (1)");
                Debug.Log("DeskPanel visual smoke test passed. Atmosphere darkening and candle glow are absent, and badges keep hover shake.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void ValidateAtmosphereRemoved(GameObject deskPanel)
        {
            foreach (var component in deskPanel.GetComponents<MonoBehaviour>())
            {
                if (component != null && component.GetType().Name == "DeskPanelAtmosphereView")
                {
                    throw new InvalidDataException("DeskPanelAtmosphereView must be removed from DeskPanel.");
                }
            }

            if (FindChild(deskPanel.transform, "桌面暗角") != null)
            {
                throw new InvalidDataException("桌面暗角 must be removed from DeskPanel.");
            }

            if (FindChild(deskPanel.transform, "蜡烛光晕") != null)
            {
                throw new InvalidDataException("蜡烛光晕 must be removed from DeskPanel.");
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
