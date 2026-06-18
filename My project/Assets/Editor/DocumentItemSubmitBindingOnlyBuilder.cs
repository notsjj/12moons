using System;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools
{
    public static class DocumentItemSubmitBindingOnlyBuilder
    {
        private const string InventoryPanelPrefabPath = "Assets/Resources/Prefabs/UI/物品面板.prefab";
        private const string InventoryPanelResourcePath = "Prefabs/UI/物品面板";

        [MenuItem("Twelve Moons/Setup/Update Document Item Submit Binding Only")]
        public static void UpdateDocumentItemSubmitBindingOnly()
        {
            var popup = UnityEngine.Object.FindFirstObjectByType<DocumentPopupPanelView>(FindObjectsInactive.Include);
            if (popup == null)
            {
                Fail("未找到 DocumentPopupPanelView。本工具只绑定已有公文弹窗，不会创建或重建 UI。");
                return;
            }

            var submitSlot = UnityEngine.Object.FindFirstObjectByType<DocumentSubmitSlot>(FindObjectsInactive.Include);
            if (submitSlot == null)
            {
                Fail("未找到 DocumentSubmitSlot。本工具只绑定已有提交槽，不会创建提交槽。");
                return;
            }

            var inventoryPanelPrefab = AssetDatabase.LoadAssetAtPath<InventoryPanelView>(InventoryPanelPrefabPath);
            if (inventoryPanelPrefab == null)
            {
                Fail($"未找到物品面板预制体：{InventoryPanelPrefabPath}。请确认物品面板 prefab 已放在 Resources/Prefabs/UI 下。");
                return;
            }

            var leftScroll = FindRequiredChild(popup.transform, "左滚轴");
            var contentViewport = FindRequiredChild(popup.transform, "内容视口");
            if (leftScroll == null || contentViewport == null)
            {
                return;
            }

            var popupSerializedObject = new SerializedObject(popup);
            popupSerializedObject.FindProperty("inventoryPanel").objectReferenceValue = null;
            popupSerializedObject.FindProperty("inventoryPanelPrefab").objectReferenceValue = inventoryPanelPrefab;
            popupSerializedObject.FindProperty("inventoryPanelResourcePath").stringValue = InventoryPanelResourcePath;
            popupSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(popup);

            var submitSerializedObject = new SerializedObject(submitSlot);
            submitSerializedObject.FindProperty("leftScrollEnd").objectReferenceValue = leftScroll;
            submitSerializedObject.FindProperty("contentViewport").objectReferenceValue = contentViewport;
            submitSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(submitSlot);

            Debug.Log("Document item submit binding updated only. It now uses the inventory panel prefab and did not create, move, resize, delete, or rebuild UI objects.");
        }

        private static RectTransform FindRequiredChild(Transform root, string childName)
        {
            var child = FindChildRecursive(root, childName);
            if (child == null)
            {
                Fail($"未找到 {childName}。请保留当前布局并先确认该对象已存在，本工具不会自动创建它。");
                return null;
            }

            var rectTransform = child as RectTransform;
            if (rectTransform == null)
            {
                Fail($"{childName} 不是 RectTransform，无法作为提交卡牌动画锚点。");
                return null;
            }

            return rectTransform;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            if (string.Equals(root.name, childName, StringComparison.Ordinal))
            {
                return root;
            }

            foreach (Transform child in root)
            {
                var result = FindChildRecursive(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void Fail(string message)
        {
            Debug.LogError(message);
            EditorUtility.DisplayDialog("Update Document Item Submit Binding Only", message, "OK");
        }
    }
}
