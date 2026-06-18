using TwelveMoons.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools
{
    public static class TaskPanelCollapseBindingOnlyBuilder
    {
        private const string SharedHudPrefabPath = "Assets/Resources/Prefabs/UI/共享HUD面板.prefab";

        [MenuItem("Twelve Moons/Setup/Bind Task Panel Collapse Button Only")]
        public static void BindTaskPanelCollapseButtonOnly()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(SharedHudPrefabPath);
            try
            {
                var taskPanel = prefabRoot.GetComponentInChildren<TaskPanelView>(true);
                if (taskPanel == null)
                {
                    Debug.LogError("局部绑定失败：共享HUD面板.prefab 下找不到 TaskPanelView。");
                    return;
                }

                var taskPanelRect = taskPanel.transform as RectTransform;
                var collapseButton = FindDirectChildButton(taskPanel.transform, "展开按钮");
                if (collapseButton == null)
                {
                    Debug.LogError("局部绑定失败：任务面板下找不到名为“展开按钮”的 Button。");
                    return;
                }

                var serializedPanel = new SerializedObject(taskPanel);
                serializedPanel.FindProperty("panelCollapseButton").objectReferenceValue = collapseButton;
                serializedPanel.FindProperty("panelMoveRoot").objectReferenceValue = taskPanelRect;
                serializedPanel.FindProperty("panelCollapseDuration").floatValue = 0.35f;
                serializedPanel.FindProperty("panelCollapseEase").enumValueIndex = 9;
                serializedPanel.ApplyModifiedPropertiesWithoutUndo();

                AddPersistentListenerIfMissing(collapseButton, taskPanel);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, SharedHudPrefabPath);
                Debug.Log("已局部绑定任务面板展开按钮：点击后使用 DOTween 收起/展开任务面板，其它 UI 未重建。");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static Button FindDirectChildButton(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static void AddPersistentListenerIfMissing(Button button, TaskPanelView taskPanel)
        {
            for (var index = 0; index < button.onClick.GetPersistentEventCount(); index++)
            {
                if (button.onClick.GetPersistentTarget(index) == taskPanel &&
                    button.onClick.GetPersistentMethodName(index) == nameof(TaskPanelView.TogglePanelCollapsed))
                {
                    return;
                }
            }

            UnityEventTools.AddPersistentListener(button.onClick, taskPanel.TogglePanelCollapsed);
        }
    }
}
