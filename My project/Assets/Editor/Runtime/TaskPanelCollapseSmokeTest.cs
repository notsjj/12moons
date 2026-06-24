using System;
using System.IO;
using System.Linq;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class TaskPanelCollapseSmokeTest
    {
        private const string SharedHudPrefabPath = "Assets/Resources/Prefabs/UI/共享HUD面板.prefab";

        [MenuItem("Twelve Moons/Tests/Run Task Panel Collapse Smoke Test")]
        public static void Run()
        {
            ValidateRuntimeApi();
            ValidateSharedHudPrefabBinding();
            Debug.Log("Task panel collapse smoke test passed. The shared HUD task panel has a panel-level DOTween collapse button.");
        }

        private static void ValidateRuntimeApi()
        {
            var toggleMethod = typeof(TaskPanelView).GetMethod(nameof(TaskPanelView.TogglePanelCollapsed));
            if (toggleMethod == null)
            {
                throw new InvalidOperationException("TaskPanelView 缺少供按钮 OnClick 绑定的 TogglePanelCollapsed 方法。");
            }

            var calculateMethod = typeof(TaskPanelView).GetMethod(nameof(TaskPanelView.EditorCalculateCollapsedAnchoredPosition));
            if (calculateMethod == null)
            {
                throw new InvalidOperationException("TaskPanelView 缺少编辑器测试用的收起位置计算方法。");
            }

            var sourcePath = "Assets/Scripts/UI/TaskPanelView.cs";
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("缺少 TaskPanelView 脚本。");
            }

            var source = File.ReadAllText(sourcePath);
            if (!source.Contains("DG.Tweening") ||
                !source.Contains("DOAnchorPos") ||
                !source.Contains("SetUpdate(true)") ||
                !source.Contains("initialPanelCollapsed") ||
                !source.Contains("ApplyInitialPanelCollapsedIfNeeded") ||
                !source.Contains("收起按钮") ||
                !source.Contains("计算这个按钮的左边界碰到屏幕左边界"))
            {
                throw new InvalidOperationException("TaskPanelView 必须使用 DOTween 移动面板，并用中文 Header/Tooltip 说明收起按钮和计算规则。");
            }
        }

        private static void ValidateSharedHudPrefabBinding()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedHudPrefabPath);
            if (prefab == null)
            {
                throw new FileNotFoundException($"缺少共享 HUD Prefab：{SharedHudPrefabPath}", SharedHudPrefabPath);
            }

            var panel = prefab.GetComponentInChildren<TaskPanelView>(true);
            if (panel == null)
            {
                throw new InvalidOperationException("共享 HUD Prefab 下缺少 TaskPanelView。");
            }

            var serializedPanel = new SerializedObject(panel);
            var buttonProperty = serializedPanel.FindProperty("panelCollapseButton");
            if (buttonProperty == null || buttonProperty.objectReferenceValue == null)
            {
                throw new InvalidOperationException("TaskPanelView 的面板收起按钮必须绑定到共享 HUD 任务面板外侧按钮。");
            }

            var button = buttonProperty.objectReferenceValue as Button;
            if (button == null)
            {
                throw new InvalidOperationException("TaskPanelView 的面板收起按钮引用不是 Button。");
            }

            if (!button.name.Contains("展开"))
            {
                throw new InvalidOperationException($"面板收起按钮应使用中文命名，当前为：{button.name}");
            }

            var hasPersistentToggle = Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                .Any(index =>
                    button.onClick.GetPersistentTarget(index) == panel &&
                    button.onClick.GetPersistentMethodName(index) == nameof(TaskPanelView.TogglePanelCollapsed));
            if (!hasPersistentToggle)
            {
                throw new InvalidOperationException("面板收起按钮 OnClick 必须绑定 TaskPanelView.TogglePanelCollapsed，用于验证点击收起/展开。");
            }
        }
    }
}
