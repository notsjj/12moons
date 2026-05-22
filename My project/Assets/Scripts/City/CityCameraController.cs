using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TwelveMoons.City
{
    public sealed class CityCameraController : MonoBehaviour
    {
        [Header("摄像机引用：只控制观察位置")]
        [Tooltip("城区摄像机；留空时会使用当前物体上的 Camera 或场景 MainCamera。该组件只移动摄像机，不刷新城区数据。")]
        [SerializeField] private Camera cityCamera;

        [Header("默认视角：回到全局地图")]
        [Tooltip("全局地图观察点空物体；点击回到全局时摄像机移动到这里。")]
        [SerializeField] private Transform defaultViewPoint;

        [Header("观察点列表：阶段13只做摄像机移动")]
        [Tooltip("可切换的城区观察点；每一项必须绑定一个空物体 Transform。")]
        [SerializeField] private List<CityCameraViewPoint> viewPoints = new List<CityCameraViewPoint>();

        [Header("移动参数：可在 Inspector 调整手感")]
        [Tooltip("摄像机移动到点位所需秒数；设为 0 时立即跳转。")]
        [SerializeField] private float moveDuration = 0.6f;

        [Tooltip("勾选后移动结束时同步点位旋转；用于每个观察点设置固定朝向。")]
        [SerializeField] private bool copyTargetRotation = true;

        [Tooltip("移动插值曲线；横轴为时间进度，纵轴为位置插值。")]
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("运行时调试快照：只读观察当前视角")]
        [Tooltip("当前摄像机观察点 ID；用于确认按钮是否切换到了正确点位。")]
        [SerializeField] private string inspectorCurrentViewId;

        [Tooltip("当前摄像机观察点中文名；用于 Play 模式下直接观察状态。")]
        [SerializeField] private string inspectorCurrentViewName;

        [Tooltip("当前摄像机是否仍在移动。")]
        [SerializeField] private bool inspectorIsMoving;

        [Tooltip("当前目标点位的位置和旋转摘要；用于确认摄像机只是移动到空物体点位。")]
        [SerializeField] private string inspectorTargetSummary;

        private Coroutine moveRoutine;

        public string CurrentViewId => inspectorCurrentViewId;

        public string CurrentViewName => inspectorCurrentViewName;

        public bool IsMoving => inspectorIsMoving;

        private void Awake()
        {
            ResolveCamera();
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(inspectorCurrentViewId) && defaultViewPoint != null)
            {
                JumpToDefaultView();
            }
        }

        [ContextMenu("移动到全局视角")]
        public void MoveToDefaultView()
        {
            MoveToTarget("global", "全局地图", defaultViewPoint);
        }

        [ContextMenu("立即跳到全局视角")]
        public void JumpToDefaultView()
        {
            JumpToTarget("global", "全局地图", defaultViewPoint);
        }

        public void MoveToViewId(string viewId)
        {
            if (string.IsNullOrEmpty(viewId))
            {
                MoveToDefaultView();
                return;
            }

            foreach (var point in viewPoints)
            {
                if (point != null && point.ViewId == viewId)
                {
                    MoveToTarget(point.ViewId, point.DisplayName, point.Target);
                    return;
                }
            }

            Debug.LogWarning($"找不到城区摄像机观察点：{viewId}", this);
        }

        public void MoveToViewIndex(int index)
        {
            if (index < 0 || index >= viewPoints.Count)
            {
                Debug.LogWarning($"城区摄像机观察点序号越界：{index}", this);
                return;
            }

            var point = viewPoints[index];
            MoveToTarget(point.ViewId, point.DisplayName, point.Target);
        }

        public void MoveToView1()
        {
            MoveToViewIndex(0);
        }

        public void MoveToView2()
        {
            MoveToViewIndex(1);
        }

        public void MoveToView3()
        {
            MoveToViewIndex(2);
        }

        private void MoveToTarget(string viewId, string viewName, Transform target)
        {
            ResolveCamera();
            if (cityCamera == null || target == null)
            {
                Debug.LogWarning("城区摄像机或目标点位为空，无法移动。", this);
                return;
            }

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            moveRoutine = StartCoroutine(MoveRoutine(viewId, viewName, target));
        }

        private void JumpToTarget(string viewId, string viewName, Transform target)
        {
            ResolveCamera();
            if (cityCamera == null || target == null)
            {
                return;
            }

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
                moveRoutine = null;
            }

            ApplyCameraTarget(target);
            SetInspectorSnapshot(viewId, viewName, target, false);
        }

        private IEnumerator MoveRoutine(string viewId, string viewName, Transform target)
        {
            inspectorIsMoving = true;
            var cameraTransform = cityCamera.transform;
            var startPosition = cameraTransform.position;
            var startRotation = cameraTransform.rotation;
            var targetPosition = target.position;
            var targetRotation = target.rotation;
            var duration = Mathf.Max(0f, moveDuration);
            var elapsed = 0f;

            if (duration <= 0f)
            {
                ApplyCameraTarget(target);
                SetInspectorSnapshot(viewId, viewName, target, false);
                moveRoutine = null;
                yield break;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / duration);
                var t = movementCurve != null ? movementCurve.Evaluate(normalizedTime) : normalizedTime;
                cameraTransform.position = Vector3.LerpUnclamped(startPosition, targetPosition, t);
                if (copyTargetRotation)
                {
                    cameraTransform.rotation = Quaternion.SlerpUnclamped(startRotation, targetRotation, t);
                }

                SetInspectorSnapshot(viewId, viewName, target, true);
                yield return null;
            }

            ApplyCameraTarget(target);
            SetInspectorSnapshot(viewId, viewName, target, false);
            moveRoutine = null;
        }

        private void ApplyCameraTarget(Transform target)
        {
            cityCamera.transform.position = target.position;
            if (copyTargetRotation)
            {
                cityCamera.transform.rotation = target.rotation;
            }
        }

        private void SetInspectorSnapshot(string viewId, string viewName, Transform target, bool isMoving)
        {
            inspectorCurrentViewId = viewId ?? string.Empty;
            inspectorCurrentViewName = viewName ?? string.Empty;
            inspectorIsMoving = isMoving;
            inspectorTargetSummary = target == null
                ? "目标点位为空"
                : $"位置={target.position}, 旋转={target.eulerAngles}";
        }

        private void ResolveCamera()
        {
            if (cityCamera == null)
            {
                cityCamera = GetComponent<Camera>();
            }

            if (cityCamera == null)
            {
                cityCamera = Camera.main;
            }
        }
    }
}
