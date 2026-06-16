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
                ValidateAtmosphereRemoved(instance);
                ValidateNoiseEffectMaterials(instance.transform);
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

        private static void ValidateNoiseEffectMaterials(Transform deskPanel)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/UI/DeskNoisePulse.mat");
            if (material == null)
            {
                throw new InvalidDataException("Desk noise material is missing: Assets/Materials/UI/DeskNoisePulse.mat");
            }

            if (material.GetFloat("_PixelSize") > 1.5f ||
                material.GetFloat("_Alpha") > 0.35f ||
                material.GetFloat("_Contrast") > 1f)
            {
                throw new InvalidDataException("Desk noise material must use fine, low-contrast grain.");
            }

            var noiseNodes = new System.Collections.Generic.List<Transform>();
            FindChildren(deskPanel, "噪点效果", noiseNodes);
            if (noiseNodes.Count != 2)
            {
                throw new InvalidDataException($"DeskPanel must have exactly two 噪点效果 nodes, got {noiseNodes.Count}.");
            }

            foreach (var node in noiseNodes)
            {
                var image = node.GetComponent<Image>();
                if (image == null || image.material != material)
                {
                    throw new InvalidDataException($"DeskPanel noise node must use DeskNoisePulse material: {node.name}");
                }
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

        private static void FindChildren(Transform root, string childName, System.Collections.Generic.List<Transform> results)
        {
            if (root.name == childName)
            {
                results.Add(root);
            }

            foreach (Transform child in root)
            {
                FindChildren(child, childName, results);
            }
        }
    }
}
