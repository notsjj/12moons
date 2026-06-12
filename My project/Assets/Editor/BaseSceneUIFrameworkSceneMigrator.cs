using System;
using System.Collections.Generic;
using System.Linq;
using TwelveMoons.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwelveMoons.EditorTools
{
    public static class BaseSceneUIFrameworkSceneMigrator
    {
        private const string ScenePath = "Assets/Scenes/BaseScene.unity";

        private static readonly string[] LayerRootNames =
        {
            "PersistentRoot",
            "PanelRoot",
            "PopupRoot",
            "OverlayRoot"
        };

        private static readonly string[] MainCanvasMigratedRoots =
        {
            "DeskPanel",
            "StoryPanel",
            "SharedHudRoot",
            "DocumentPopupPanel",
            "NewspaperPanel",
            "LetterReaderPanel"
        };

        private static readonly string[] CityMigratedRoots =
        {
            "CityCameraControls",
            "CityOverlayPanel"
        };

        [MenuItem("Twelve Moons/UIFramework/Apply Base Scene UIFramework Migration")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"无法打开场景：{ScenePath}");
            }

            var mainCanvas = RequireSceneObject("Main Canvas").GetComponent<Canvas>();
            if (mainCanvas == null)
            {
                throw new InvalidOperationException("Main Canvas 缺少 Canvas 组件，无法作为 UIFramework 根画布。");
            }

            EnsureLayerRoots(mainCanvas.transform);
            var uiManagerObject = RequireSceneObject("UI Manager");
            var uiManager = EnsureComponent<UIManager>(uiManagerObject);
            var context = EnsureComponent<BaseSceneUIContext>(uiManagerObject);
            var bootstrap = EnsureComponent<BaseSceneUIBootstrap>(uiManagerObject);

            AssignObjectReference(uiManager, "mainCanvas", mainCanvas);
            AssignObjectReference(bootstrap, "uiContext", context);
            AssignObjectReference(bootstrap, "uiManager", uiManager);
            AssignBool(bootstrap, "showDebugControlsOnStart", false);

            context.ResolveMissingReferences();
            EditorUtility.SetDirty(context);
            EditorUtility.SetDirty(uiManager);
            EditorUtility.SetDirty(bootstrap);

            RemoveNamedChildren(mainCanvas.transform, MainCanvasMigratedRoots);
            RemoveDirectChildrenExceptLayerRoots(mainCanvas.transform);

            var cityRoot = FindSceneObject("CityRoot");
            if (cityRoot != null)
            {
                RemoveNamedChildren(cityRoot.transform, CityMigratedRoots);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("BaseScene UIFramework 场景迁移已完成：Main Canvas 仅保留框架根节点，业务 UI 将在运行时动态创建。");
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void EnsureLayerRoots(Transform canvasTransform)
        {
            for (var i = 0; i < LayerRootNames.Length; i++)
            {
                var rootName = LayerRootNames[i];
                var child = canvasTransform.Find(rootName);
                if (child == null)
                {
                    var root = new GameObject(rootName, typeof(RectTransform), typeof(CanvasGroup));
                    child = root.transform;
                    child.SetParent(canvasTransform, false);
                }

                child.SetSiblingIndex(i);
                var rect = child.GetComponent<RectTransform>();
                if (rect == null)
                {
                    rect = child.gameObject.AddComponent<RectTransform>();
                }

                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        private static void RemoveNamedChildren(Transform parent, IEnumerable<string> childNames)
        {
            foreach (var childName in childNames)
            {
                var child = FindChild(parent, childName);
                if (child != null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void RemoveDirectChildrenExceptLayerRoots(Transform canvasTransform)
        {
            var allowedNames = new HashSet<string>(LayerRootNames);
            var childrenToRemove = canvasTransform
                .Cast<Transform>()
                .Where(child => !allowedNames.Contains(child.name))
                .ToList();

            foreach (var child in childrenToRemove)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void AssignObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"{target.GetType().Name} 缺少序列化字段：{propertyName}");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignBool(UnityEngine.Object target, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"{target.GetType().Name} 缺少序列化字段：{propertyName}");
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject RequireSceneObject(string objectName)
        {
            var sceneObject = FindSceneObject(objectName);
            if (sceneObject == null)
            {
                throw new InvalidOperationException($"BaseScene 缺少必要对象：{objectName}");
            }

            return sceneObject;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
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
