using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools
{
    public static class DeskPanelAtmosphereOnlyBuilder
    {
        private const string DeskPanelPrefabPath = "Assets/Resources/Prefabs/UI/DeskPanel.prefab";

        [MenuItem("Twelve Moons/UI/局部更新 DeskPanel 暗角与受光区域")]
        public static void UpdateDeskPanelAtmosphereOnly()
        {
            var root = PrefabUtility.LoadPrefabContents(DeskPanelPrefabPath);
            if (root == null)
            {
                Debug.LogError($"DeskPanel 氛围效果局部更新失败：找不到 Prefab：{DeskPanelPrefabPath}");
                return;
            }

            try
            {
                var candle = root.transform.Find("蜡烛");
                if (candle == null)
                {
                    Debug.LogError("DeskPanel 氛围效果局部更新失败：DeskPanel 根节点下找不到“蜡烛”。");
                    return;
                }

                var candleRect = candle as RectTransform;
                var legacyGlow = root.transform.Find("蜡烛光晕");
                if (legacyGlow != null)
                {
                    Object.DestroyImmediate(legacyGlow.gameObject);
                }

                var vignette = EnsureVignette(root.transform);
                var atmosphere = root.GetComponent<DeskPanelAtmosphereView>();
                if (atmosphere == null)
                {
                    atmosphere = root.AddComponent<DeskPanelAtmosphereView>();
                }

                var serializedObject = new SerializedObject(atmosphere);
                serializedObject.FindProperty("vignetteImage").objectReferenceValue = vignette.GetComponent<Image>();
                serializedObject.FindProperty("candleRect").objectReferenceValue = candleRect;
                var lightTargets = serializedObject.FindProperty("lightTargets");
                lightTargets.arraySize = 1;
                lightTargets.GetArrayElementAtIndex(0).objectReferenceValue = candleRect;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                atmosphere.EnsureSetup();
                atmosphere.ApplyVisualSettings();
                EditorUtility.SetDirty(atmosphere);
                PrefabUtility.SaveAsPrefabAsset(root, DeskPanelPrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("DeskPanel 氛围效果局部更新完成：已删除旧蜡烛光晕，仅更新桌面暗角与受光区域引用。");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static RectTransform EnsureVignette(Transform root)
        {
            var existing = root.Find("桌面暗角") as RectTransform;
            if (existing != null)
            {
                var existingImage = existing.GetComponent<Image>();
                if (existingImage == null)
                {
                    existingImage = existing.gameObject.AddComponent<Image>();
                }

                existingImage.raycastTarget = false;
                return existing;
            }

            var gameObject = new GameObject("桌面暗角", typeof(RectTransform), typeof(Image));
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(root, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var image = gameObject.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            rectTransform.SetAsLastSibling();
            return rectTransform;
        }
    }
}
