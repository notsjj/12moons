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
            var deskPanelType = new UIType("Prefabs/UI/DeskPanel", UILayer.Panel);
            if (deskPanelType.Name != "DeskPanel")
            {
                throw new InvalidOperationException("UIType 未正确解析 UI 名称。");
            }

            if (deskPanelType.Layer != UILayer.Panel)
            {
                throw new InvalidOperationException("UIType 未正确保存 UI 层级。");
            }

            var backslashPanelType = new UIType(@"Prefabs\UI\DeskPanel", UILayer.Panel);
            if (backslashPanelType.Path != "Prefabs/UI/DeskPanel" || backslashPanelType.Name != "DeskPanel")
            {
                throw new InvalidOperationException("UIType 未正确规范化反斜杠路径。");
            }

            var trimmedPanelType = new UIType(" Prefabs/UI/DeskPanel ", UILayer.Panel);
            if (trimmedPanelType.Path != "Prefabs/UI/DeskPanel")
            {
                throw new InvalidOperationException("UIType 未正确裁剪路径前后空白。");
            }

            try
            {
                _ = new UIType("Prefabs/UI/DeskPanel/", UILayer.Panel);
                throw new InvalidOperationException("UIType 未拒绝以斜杠结尾的路径。");
            }
            catch (ArgumentException)
            {
            }

            foreach (var path in RequiredPrefabPaths)
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"缺少 UIFramework Prefab：{path}", path);
                }
            }

            Debug.Log("Base Scene UIFramework smoke test passed.");
        }
    }
}
