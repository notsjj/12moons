using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class BaseSceneUIFrameworkSmokeTest
    {
        private static readonly string[] RequiredPrefabPaths =
        {
            "Assets/Resources/Prefabs/UI/DeskPanel.prefab",
            "Assets/Resources/Prefabs/UI/SharedHudPanel.prefab",
            "Assets/Resources/Prefabs/UI/StoryPanel.prefab",
            "Assets/Resources/Prefabs/UI/CityHudPanel.prefab",
            "Assets/Resources/Prefabs/UI/DocumentPopupPanel.prefab",
            "Assets/Resources/Prefabs/UI/NewspaperPanel.prefab",
            "Assets/Resources/Prefabs/UI/LetterReaderPanel.prefab"
        };

        [MenuItem("Twelve Moons/Tests/Run Base Scene UIFramework Smoke Test")]
        public static void Run()
        {
            foreach (var path in RequiredPrefabPaths)
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"缺少 UIFramework Prefab：{path}", path);
                }
            }

            var deskPanelType = new UIType("Prefabs/UI/DeskPanel", UILayer.Panel);
            if (deskPanelType.Name != "DeskPanel")
            {
                throw new InvalidOperationException("UIType 未正确解析 UI 名称。");
            }

            if (deskPanelType.Layer != UILayer.Panel)
            {
                throw new InvalidOperationException("UIType 未正确保存 UI 层级。");
            }

            Debug.Log("Base Scene UIFramework smoke test passed.");
        }
    }
}
