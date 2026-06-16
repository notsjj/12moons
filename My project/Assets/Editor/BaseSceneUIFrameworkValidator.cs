using System;
using System.Collections.Generic;
using TMPro;
using TwelveMoons.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwelveMoons.EditorTools
{
    public static class BaseSceneUIFrameworkValidator
    {
        private const string ScenePath = "Assets/Scenes/BaseScene.unity";

        private static readonly string[] RequiredPrefabPaths =
        {
            "Assets/Resources/Prefabs/UI/桌面面板.prefab",
            "Assets/Resources/Prefabs/UI/共享HUD面板.prefab",
            "Assets/Resources/Prefabs/UI/剧情面板.prefab",
            "Assets/Resources/Prefabs/UI/城区HUD面板.prefab",
            "Assets/Resources/Prefabs/UI/公文弹窗面板.prefab",
            "Assets/Resources/Prefabs/UI/报纸面板.prefab",
            "Assets/Resources/Prefabs/UI/信件阅读面板.prefab",
            "Assets/Resources/Prefabs/UI/任务行.prefab",
            "Assets/Resources/Prefabs/UI/物品卡片.prefab",
            "Assets/Resources/Prefabs/UI/阵营质疑行.prefab"
        };

        private static readonly HashSet<string> AllowedMainCanvasChildren = new HashSet<string>
        {
            "PersistentRoot",
            "PanelRoot",
            "PopupRoot",
            "OverlayRoot"
        };

        private static readonly string[] ForbiddenSceneObjects =
        {
            "DeskPanel",
            "StoryPanel",
            "SharedHudRoot",
            "DocumentPopupPanel",
            "NewspaperPanel",
            "LetterReaderPanel"
        };

        private static readonly string[] ForbiddenCityObjects =
        {
            "CityCameraControls",
            "CityOverlayPanel"
        };

        [MenuItem("Twelve Moons/UIFramework/Validate Base Scene UIFramework")]
        public static void Validate()
        {
            ValidatePrefabs();
            ValidateScene();
            Debug.Log("BaseScene UIFramework 验证通过：Prefab、场景残留、层级根节点和 TMP 文本高度均符合要求。");
        }

        private static void ValidatePrefabs()
        {
            foreach (var path in RequiredPrefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"缺少 UIFramework Prefab：{path}");
                }

                if (!IsStandaloneItemPrefab(path))
                {
                    var panelRoot = prefab.GetComponent<BaseSceneUIPanelRoot>();
                    if (panelRoot == null)
                    {
                        throw new InvalidOperationException($"UIFramework Prefab 根节点缺少 BaseSceneUIPanelRoot：{path}");
                    }
                }

                ValidateTextHeights(prefab, path);
            }
        }

        private static bool IsStandaloneItemPrefab(string path)
        {
            return path.EndsWith("任务行.prefab", StringComparison.Ordinal) ||
                   path.EndsWith("物品卡片.prefab", StringComparison.Ordinal) ||
                   path.EndsWith("阵营质疑行.prefab", StringComparison.Ordinal);
        }

        private static void ValidateTextHeights(GameObject root, string ownerPath)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                var rect = text.GetComponent<RectTransform>();
                if (rect != null && rect.sizeDelta.y < 0f)
                {
                    throw new InvalidOperationException($"TMP 文本高度不能为负数：{ownerPath}/{text.name}");
                }
            }
        }

        private static void ValidateScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"无法打开场景：{ScenePath}");
            }

            var mainCanvasObject = RequireSceneObject("Main Canvas");
            var mainCanvas = mainCanvasObject.GetComponent<Canvas>();
            if (mainCanvas == null)
            {
                throw new InvalidOperationException("Main Canvas 缺少 Canvas 组件。");
            }

            foreach (var requiredRoot in AllowedMainCanvasChildren)
            {
                if (mainCanvas.transform.Find(requiredRoot) == null)
                {
                    throw new InvalidOperationException($"缺少 UIFramework 层级根节点：{requiredRoot}");
                }
            }

            foreach (Transform child in mainCanvas.transform)
            {
                if (!AllowedMainCanvasChildren.Contains(child.name))
                {
                    throw new InvalidOperationException($"Main Canvas 进入 Play Mode 前只能保留 UIFramework 根节点，发现多余对象：{child.name}");
                }

                if (child.childCount > 0)
                {
                    throw new InvalidOperationException($"Main Canvas 的 {child.name} 进入 Play Mode 前必须为空，业务 UI 应运行时动态创建。");
                }
            }

            foreach (var objectName in ForbiddenSceneObjects)
            {
                if (FindChild(mainCanvas.transform, objectName) != null)
                {
                    throw new InvalidOperationException($"Main Canvas 仍残留已迁移业务 UI：{objectName}");
                }
            }

            var cityRoot = FindSceneObject("CityRoot");
            if (cityRoot != null)
            {
                foreach (var objectName in ForbiddenCityObjects)
                {
                    if (FindChild(cityRoot.transform, objectName) != null)
                    {
                        throw new InvalidOperationException($"CityRoot 仍残留已迁移城区 UI：{objectName}");
                    }
                }
            }

            var uiManagerObject = RequireSceneObject("UI Manager");
            if (uiManagerObject.GetComponent<UIManager>() == null)
            {
                throw new InvalidOperationException("UI Manager 缺少 UIManager 组件。");
            }

            if (uiManagerObject.GetComponent<BaseSceneUIContext>() == null)
            {
                throw new InvalidOperationException("UI Manager 缺少 BaseSceneUIContext 组件。");
            }

            if (uiManagerObject.GetComponent<BaseSceneUIBootstrap>() == null)
            {
                throw new InvalidOperationException("UI Manager 缺少 BaseSceneUIBootstrap 组件。");
            }
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
