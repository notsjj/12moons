using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using TwelveMoons.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TwelveMoons.EditorTools
{
    public static class BaseSceneUIFrameworkPrefabBuilder
    {
        private const string ScenePath = "Assets/Scenes/BaseScene.unity";
        private const string PrefabRoot = "Assets/Resources/Prefabs/UI";

        private static readonly string[] DebugRootNames =
        {
            "TestPanel",
            "StoryDebugButtons",
            "RoundDebugButtons",
            "SuspicionDebugButtons",
            "LetterDebugButtons"
        };

        [MenuItem("Twelve Moons/UIFramework/Rebuild Base Scene UI Prefabs Only")]
        public static void Rebuild()
        {
            Directory.CreateDirectory(PrefabRoot);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"无法打开场景：{ScenePath}");
            }

            SaveSceneObjectAsPrefab("DeskPanel", "DeskPanel", RepairDeskPanel);
            SaveSceneObjectAsPrefab("SharedHudRoot", "SharedHudPanel", RepairSharedHudPanel);
            SaveSceneObjectAsPrefab("StoryPanel", "StoryPanel", null);
            SaveSceneObjectAsPrefab("DocumentPopupPanel", "DocumentPopupPanel", null);
            SaveSceneObjectAsPrefab("NewspaperPanel", "NewspaperPanel", null);
            SaveSceneObjectAsPrefab("LetterReaderPanel", "LetterReaderPanel", null);
            SaveCityHudPanelPrefab();
            UiArtChinesePrefabStyler.ApplyChineseUiArtAndRenamePrefabs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Base Scene UIFramework Prefab 已局部重建完成。");
        }

        private static void SaveSceneObjectAsPrefab(string sceneObjectName, string prefabName, Action<GameObject> repair)
        {
            var source = FindSceneObject(sceneObjectName);
            if (source == null)
            {
                throw new InvalidOperationException($"场景中缺少 UI 对象：{sceneObjectName}");
            }

            var copy = UnityEngine.Object.Instantiate(source);
            copy.name = prefabName;
            PreparePrefabRoot(copy);
            repair?.Invoke(copy);
            SaveAndDestroy(copy, prefabName);
        }

        private static void SaveCityHudPanelPrefab()
        {
            var cityRoot = FindSceneObject("CityRoot");
            if (cityRoot == null)
            {
                throw new InvalidOperationException("场景中缺少 CityRoot，无法生成 CityHudPanel。");
            }

            var cityCameraControls = FindChild(cityRoot.transform, "CityCameraControls");
            var cityOverlayPanel = FindChild(cityRoot.transform, "CityOverlayPanel");
            if (cityCameraControls == null || cityOverlayPanel == null)
            {
                throw new InvalidOperationException("CityRoot 下缺少 CityCameraControls 或 CityOverlayPanel，无法生成 CityHudPanel。");
            }

            var root = new GameObject("CityHudPanel", typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            CopyChildTo(cityCameraControls.gameObject, root.transform);
            CopyChildTo(cityOverlayPanel.gameObject, root.transform);

            PreparePrefabRoot(root);
            RepairCityHudPanel(root);
            SaveAndDestroy(root, "CityHudPanel");
        }

        private static void RepairDeskPanel(GameObject root)
        {
            var deskPanel = root.GetComponent<TwelveMoons.UI.DeskPanelView>();
            if (deskPanel != null)
            {
                var serializedObject = new SerializedObject(deskPanel);
                ClearObjectReference(serializedObject, "taskPanel");
                SetObjectReference(serializedObject, "suspicionPanel", FindComponent<TwelveMoons.UI.SuspicionPanelView>(root, "SuspicionPanel"));
                SetObjectReference(serializedObject, "letterArea", FindComponent<TwelveMoons.UI.LetterAreaView>(root, "LetterArea"));
                SetObjectReference(serializedObject, "inventoryPanel", FindComponent<TwelveMoons.UI.InventoryPanelView>(root, "InventoryPanel"));
                SetObjectReference(serializedObject, "sharedActorSlot", FindComponent<TwelveMoons.UI.SharedActorSlotView>(root, "SharedActorSlot"));
                ClearObjectReference(serializedObject, "documentPopupPanel");
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            var deskLoop = root.GetComponentInChildren<TwelveMoons.UI.DeskLoopController>(true);
            if (deskLoop != null)
            {
                var serializedObject = new SerializedObject(deskLoop);
                SetObjectReference(serializedObject, "documentPopupPanel", FindComponent<TwelveMoons.UI.DocumentPopupPanelView>(root, "DocumentPopupPanel"));
                SetObjectReference(serializedObject, "newspaperPanel", FindComponent<TwelveMoons.UI.NewspaperPanelView>(root, "NewspaperPanel"));
                SetObjectReference(serializedObject, "storyButton", FindComponent<UnityEngine.UI.Button>(root, "StoryButton"));
                SetObjectReference(serializedObject, "documentButton", FindComponent<UnityEngine.UI.Button>(root, "DocumentButton"));
                SetObjectReference(serializedObject, "endRoundButton", FindComponent<UnityEngine.UI.Button>(root, "EndRoundButton"));
                SetObjectReference(serializedObject, "newspaperButton", FindComponent<UnityEngine.UI.Button>(root, "NewspaperButton"));
                SetObjectReference(serializedObject, "cityButton", FindComponent<UnityEngine.UI.Button>(root, "CityButton"));
                SetObjectReference(serializedObject, "statusText", FindComponent<TMP_Text>(root, "StatusText"));
                SetObjectReference(serializedObject, "sharedActorSlot", FindComponent<TwelveMoons.UI.SharedActorSlotView>(root, "SharedActorSlot"));
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void RepairSharedHudPanel(GameObject root)
        {
            var taskPanel = root.GetComponentInChildren<TwelveMoons.UI.TaskPanelView>(true);
            if (taskPanel != null)
            {
                var serializedObject = new SerializedObject(taskPanel);
                SetObjectReference(serializedObject, "contentRoot", FindRectTransform(root, "TaskContent"));
                SetObjectReference(serializedObject, "emptyText", FindComponent<TMP_Text>(root, "EmptyText"));
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            var roundPanel = root.GetComponentInChildren<TwelveMoons.UI.RoundPanelView>(true);
            if (roundPanel != null)
            {
                var serializedObject = new SerializedObject(roundPanel);
                SetObjectReference(serializedObject, "roundText", FindComponent<TMP_Text>(root, "RoundText"));
                SetObjectReference(serializedObject, "totalRoundText", FindComponent<TMP_Text>(root, "TotalRoundText"));
                SetObjectReference(serializedObject, "disasterStageText", FindComponent<TMP_Text>(root, "DisasterStageText"));
                SetObjectReference(serializedObject, "feedbackText", FindComponent<TMP_Text>(root, "RoundFeedbackText"));
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void RepairCityHudPanel(GameObject root)
        {
            var overlay = root.GetComponentInChildren<TwelveMoons.UI.City.CityOverlayPanelView>(true);
            if (overlay == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(overlay);
            SetObjectReference(serializedObject, "taskPanel", FindComponent<TwelveMoons.UI.TaskPanelView>(root, "TaskPanel"));
            SetObjectReference(serializedObject, "citySuspicionPanel", FindComponent<TwelveMoons.UI.SuspicionPanelView>(root, "CitySuspicionPanel"));
            SetObjectReference(serializedObject, "roundPanel", FindComponent<TwelveMoons.UI.RoundPanelView>(root, "RoundPanel"));
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PreparePrefabRoot(GameObject root)
        {
            var panelRoot = root.GetComponent<BaseSceneUIPanelRoot>();
            if (panelRoot == null)
            {
                panelRoot = root.AddComponent<BaseSceneUIPanelRoot>();
            }

            AssignDebugRoots(panelRoot, root);
            EnsureTextHeights(root);
        }

        private static void AssignDebugRoots(BaseSceneUIPanelRoot panelRoot, GameObject root)
        {
            var debugRoots = new List<GameObject>();
            foreach (var debugRootName in DebugRootNames)
            {
                var child = FindChild(root.transform, debugRootName);
                if (child != null)
                {
                    child.gameObject.SetActive(false);
                    debugRoots.Add(child.gameObject);
                }
            }

            var serializedObject = new SerializedObject(panelRoot);
            var showDebugControls = serializedObject.FindProperty("showDebugControls");
            if (showDebugControls != null)
            {
                showDebugControls.boolValue = false;
            }

            var debugRootsProperty = serializedObject.FindProperty("debugRoots");
            if (debugRootsProperty != null)
            {
                debugRootsProperty.arraySize = debugRoots.Count;
                for (var i = 0; i < debugRoots.Count; i++)
                {
                    debugRootsProperty.GetArrayElementAtIndex(i).objectReferenceValue = debugRoots[i];
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureTextHeights(GameObject root)
        {
            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (var text in texts)
            {
                var rect = text.GetComponent<RectTransform>();
                if (rect != null && rect.sizeDelta.y < 0f)
                {
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, 0f);
                    EditorUtility.SetDirty(rect);
                }
            }
        }

        private static void SaveAndDestroy(GameObject root, string prefabName)
        {
            var path = $"{PrefabRoot}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void ClearObjectReference(SerializedObject serializedObject, string propertyName)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = null;
            }
        }

        private static RectTransform FindRectTransform(GameObject root, string childName)
        {
            var child = FindChild(root.transform, childName);
            return child == null ? null : child.GetComponent<RectTransform>();
        }

        private static T FindComponent<T>(GameObject root, string childName) where T : Component
        {
            var child = FindChild(root.transform, childName);
            if (child != null && child.TryGetComponent<T>(out var directComponent))
            {
                return directComponent;
            }

            foreach (var nestedComponent in root.GetComponentsInChildren<T>(true))
            {
                if (nestedComponent.name == childName)
                {
                    return nestedComponent;
                }
            }

            return null;
        }

        private static void CopyChildTo(GameObject source, Transform parent)
        {
            var copy = UnityEngine.Object.Instantiate(source, parent, false);
            copy.name = source.name;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    return root;
                }

                var child = FindChild(root.transform, objectName);
                if (child != null)
                {
                    return child.gameObject;
                }
            }

            return null;
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
