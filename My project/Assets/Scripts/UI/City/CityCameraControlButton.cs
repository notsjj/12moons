using TMPro;
using TwelveMoons.City;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI.City
{
    public sealed class CityCameraControlButton : MonoBehaviour
    {
        [Header("按钮目标：点击后移动城区摄像机")]
        [Tooltip("城区摄像机控制器；按钮点击后只调用摄像机移动，不刷新城区数据。")]
        [SerializeField] private CityCameraController cameraController;

        [Tooltip("目标观察点 ID；留空表示回到全局地图视角。")]
        [SerializeField] private string targetViewId;

        [Header("按钮显示：阶段13调试用")]
        [Tooltip("按钮组件；留空时会自动查找当前物体上的 Button。")]
        [SerializeField] private Button button;

        [Tooltip("按钮文字；用于显示观察点中文名，必须使用 TextMeshPro。")]
        [SerializeField] private TMP_Text labelText;

        [Tooltip("按钮显示名称；用于 Inspector 中确认该按钮会切到哪个视角。")]
        [SerializeField] private string displayName;

        private void Awake()
        {
            ResolveReferences();
            RefreshLabel();
        }

        public void Configure(CityCameraController controller, string viewId, string buttonName)
        {
            cameraController = controller;
            targetViewId = viewId ?? string.Empty;
            displayName = buttonName ?? string.Empty;
            ResolveReferences();
            RefreshLabel();
        }

        public void MoveCamera()
        {
            if (cameraController == null)
            {
                Debug.LogWarning("城区摄像机按钮缺少 CityCameraController 引用。", this);
                return;
            }

            if (string.IsNullOrEmpty(targetViewId))
            {
                cameraController.MoveToDefaultView();
                return;
            }

            cameraController.MoveToViewId(targetViewId);
        }

        private void RefreshLabel()
        {
            if (labelText != null)
            {
                labelText.text = displayName;
            }
        }

        private void ResolveReferences()
        {
            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<CityCameraController>(FindObjectsInactive.Include);
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (labelText == null)
            {
                labelText = GetComponentInChildren<TMP_Text>(true);
            }
        }
    }
}
