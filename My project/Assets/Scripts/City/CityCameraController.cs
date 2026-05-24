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
        [Tooltip("全局地图观察点空物体；点击回到全局时摄像机会移动到这里。")]
        [SerializeField] private Transform defaultViewPoint;

        [Header("观察点列表：按钮移动摄像机")]
        [Tooltip("可切换的城区观察点；每一项必须绑定一个空物体 Transform，点击按钮只移动摄像机。")]
        [SerializeField] private List<CityCameraViewPoint> viewPoints = new List<CityCameraViewPoint>();

        [Header("按钮移动参数：可在 Inspector 调整手感")]
        [Tooltip("摄像机移动到点位所需秒数；设为 0 时立即跳转。")]
        [SerializeField] private float moveDuration = 0.6f;

        [Tooltip("勾选后移动结束时同步点位旋转；用于每个观察点设置固定朝向。")]
        [SerializeField] private bool copyTargetRotation = true;

        [Tooltip("移动插值曲线；横轴为时间进度，纵轴为位置插值。")]
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("手动观察：WASD 平移")]
        [Tooltip("是否允许玩家用 WASD 在城区地图上手动移动摄像机；只改变观察位置，不刷新城区数据。")]
        [SerializeField] private bool enableKeyboardMove = true;

        [Tooltip("WASD 平移速度，单位为世界坐标/秒。")]
        [SerializeField] private float keyboardMoveSpeed = 6f;

        [Header("手动观察：鼠标右键旋转")]
        [Tooltip("是否允许玩家按住鼠标右键旋转城区摄像机；只改变观察方向，不刷新城区数据。")]
        [SerializeField] private bool enableRightMouseRotate = true;

        [Tooltip("鼠标右键旋转灵敏度。")]
        [SerializeField] private float rightMouseRotateSpeed = 3f;

        [Tooltip("摄像机最低俯仰角；用于避免视角翻转或钻到地面下方。")]
        [SerializeField] private float minPitchAngle = 18f;

        [Tooltip("摄像机最高俯仰角；用于避免视角抬得过高。")]
        [SerializeField] private float maxPitchAngle = 70f;

        [Header("移动边界：不能超过地图四角和高度")]
        [Tooltip("摄像机允许移动的世界坐标 X 最小值，对应地图左边界。")]
        [SerializeField] private float minPositionX = -10f;

        [Tooltip("摄像机允许移动的世界坐标 X 最大值，对应地图右边界。")]
        [SerializeField] private float maxPositionX = 10f;

        [Tooltip("摄像机允许移动的世界坐标 Z 最小值，对应地图下/近端边界。")]
        [SerializeField] private float minPositionZ = -12f;

        [Tooltip("摄像机允许移动的世界坐标 Z 最大值，对应地图上/远端边界。")]
        [SerializeField] private float maxPositionZ = 4f;

        [Tooltip("摄像机允许的最低高度；按钮点位和 WASD 移动都会被限制在该高度以上。")]
        [SerializeField] private float minPositionY = 3f;

        [Tooltip("摄像机允许的最高高度；用于防止城区摄像机飞得过高。")]
        [SerializeField] private float maxPositionY = 9f;

        [Header("运行时调试快照：只读观察当前视角")]
        [Tooltip("当前摄像机观察点 ID；用来确认按钮是否切换到了正确点位。手动移动后会显示 manual。")]
        [SerializeField] private string inspectorCurrentViewId;

        [Tooltip("当前摄像机观察点中文名；用于 Play 模式下直接观察状态。手动移动后会显示手动观察。")]
        [SerializeField] private string inspectorCurrentViewName;

        [Tooltip("当前摄像机是否仍在按钮移动插值过程中。")]
        [SerializeField] private bool inspectorIsMoving;

        [Tooltip("当前摄像机的位置、旋转和边界摘要；用于确认摄像机只是在允许范围内移动。")]
        [SerializeField] private string inspectorTargetSummary;

        private Coroutine moveRoutine;
        private Vector3 lastMousePosition;
        private bool hasLastMousePosition;

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

        private void Update()
        {
            ResolveCamera();
            if (cityCamera == null)
            {
                return;
            }

            var moved = TryHandleKeyboardMove();
            var rotated = TryHandleRightMouseRotate();
            if (moved || rotated)
            {
                StopButtonMove();
                ClampCameraTransform();
                SetManualInspectorSnapshot();
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

        private bool TryHandleKeyboardMove()
        {
            if (!enableKeyboardMove)
            {
                return false;
            }

            var horizontal = 0f;
            if (Input.GetKey(KeyCode.A))
            {
                horizontal -= 1f;
            }

            if (Input.GetKey(KeyCode.D))
            {
                horizontal += 1f;
            }

            var vertical = 0f;
            if (Input.GetKey(KeyCode.S))
            {
                vertical -= 1f;
            }

            if (Input.GetKey(KeyCode.W))
            {
                vertical += 1f;
            }

            var input = new Vector3(horizontal, 0f, vertical);
            if (input.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            input = Vector3.ClampMagnitude(input, 1f);
            var cameraTransform = cityCamera.transform;
            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            if (right.sqrMagnitude <= 0.0001f)
            {
                right = Vector3.right;
            }

            var delta = (right * input.x + forward * input.z) * Mathf.Max(0f, keyboardMoveSpeed) * Time.deltaTime;
            cameraTransform.position += delta;
            return true;
        }

        private bool TryHandleRightMouseRotate()
        {
            if (!enableRightMouseRotate || !Input.GetMouseButton(1))
            {
                hasLastMousePosition = false;
                return false;
            }

            var currentMousePosition = Input.mousePosition;
            if (!hasLastMousePosition || Input.GetMouseButtonDown(1))
            {
                lastMousePosition = currentMousePosition;
                hasLastMousePosition = true;
                return false;
            }

            var mouseDelta = currentMousePosition - lastMousePosition;
            lastMousePosition = currentMousePosition;
            var mouseX = mouseDelta.x;
            var mouseY = mouseDelta.y;
            if (Mathf.Abs(mouseX) <= 0.0001f && Mathf.Abs(mouseY) <= 0.0001f)
            {
                return false;
            }

            var cameraTransform = cityCamera.transform;
            var euler = cameraTransform.eulerAngles;
            var rotationScale = rightMouseRotateSpeed * 0.1f;
            var pitch = NormalizeAngle(euler.x) - mouseY * rotationScale;
            var yaw = euler.y + mouseX * rotationScale;
            pitch = Mathf.Clamp(pitch, Mathf.Min(minPitchAngle, maxPitchAngle), Mathf.Max(minPitchAngle, maxPitchAngle));
            cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            return true;
        }

        private void MoveToTarget(string viewId, string viewName, Transform target)
        {
            ResolveCamera();
            if (cityCamera == null || target == null)
            {
                Debug.LogWarning("城区摄像机或目标点位为空，无法移动。", this);
                return;
            }

            StopButtonMove();
            moveRoutine = StartCoroutine(MoveRoutine(viewId, viewName, target));
        }

        private void JumpToTarget(string viewId, string viewName, Transform target)
        {
            ResolveCamera();
            if (cityCamera == null || target == null)
            {
                return;
            }

            StopButtonMove();
            ApplyCameraTarget(target);
            SetInspectorSnapshot(viewId, viewName, target, false);
        }

        private IEnumerator MoveRoutine(string viewId, string viewName, Transform target)
        {
            inspectorIsMoving = true;
            var cameraTransform = cityCamera.transform;
            var startPosition = cameraTransform.position;
            var startRotation = cameraTransform.rotation;
            var targetPosition = ClampPosition(target.position);
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
                    ClampCameraRotationPitch();
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
            cityCamera.transform.position = ClampPosition(target.position);
            if (copyTargetRotation)
            {
                cityCamera.transform.rotation = target.rotation;
                ClampCameraRotationPitch();
            }
        }

        private void StopButtonMove()
        {
            if (moveRoutine == null)
            {
                return;
            }

            StopCoroutine(moveRoutine);
            moveRoutine = null;
            inspectorIsMoving = false;
        }

        private void ClampCameraTransform()
        {
            cityCamera.transform.position = ClampPosition(cityCamera.transform.position);
            ClampCameraRotationPitch();
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            var minX = Mathf.Min(minPositionX, maxPositionX);
            var maxX = Mathf.Max(minPositionX, maxPositionX);
            var minY = Mathf.Min(minPositionY, maxPositionY);
            var maxY = Mathf.Max(minPositionY, maxPositionY);
            var minZ = Mathf.Min(minPositionZ, maxPositionZ);
            var maxZ = Mathf.Max(minPositionZ, maxPositionZ);
            return new Vector3(
                Mathf.Clamp(position.x, minX, maxX),
                Mathf.Clamp(position.y, minY, maxY),
                Mathf.Clamp(position.z, minZ, maxZ));
        }

        private void ClampCameraRotationPitch()
        {
            var cameraTransform = cityCamera.transform;
            var euler = cameraTransform.eulerAngles;
            var pitch = NormalizeAngle(euler.x);
            pitch = Mathf.Clamp(pitch, Mathf.Min(minPitchAngle, maxPitchAngle), Mathf.Max(minPitchAngle, maxPitchAngle));
            cameraTransform.rotation = Quaternion.Euler(pitch, euler.y, 0f);
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }

        private void SetInspectorSnapshot(string viewId, string viewName, Transform target, bool isMoving)
        {
            inspectorCurrentViewId = viewId ?? string.Empty;
            inspectorCurrentViewName = viewName ?? string.Empty;
            inspectorIsMoving = isMoving;
            inspectorTargetSummary = target == null
                ? "目标点位为空"
                : BuildCameraSummary();
        }

        private void SetManualInspectorSnapshot()
        {
            inspectorCurrentViewId = "manual";
            inspectorCurrentViewName = "手动观察";
            inspectorIsMoving = false;
            inspectorTargetSummary = BuildCameraSummary();
        }

        private string BuildCameraSummary()
        {
            if (cityCamera == null)
            {
                return "摄像机为空";
            }

            var cameraTransform = cityCamera.transform;
            return $"位置={cameraTransform.position}, 旋转={cameraTransform.eulerAngles}, X范围=[{Mathf.Min(minPositionX, maxPositionX)}, {Mathf.Max(minPositionX, maxPositionX)}], Z范围=[{Mathf.Min(minPositionZ, maxPositionZ)}, {Mathf.Max(minPositionZ, maxPositionZ)}], 高度范围=[{Mathf.Min(minPositionY, maxPositionY)}, {Mathf.Max(minPositionY, maxPositionY)}]";
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
