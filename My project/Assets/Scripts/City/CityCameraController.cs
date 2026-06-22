using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TwelveMoons.City
{
    public sealed class CityCameraController : MonoBehaviour
    {
        [Header("\u6444\u50cf\u673a\u5f15\u7528\uff1a\u53ea\u63a7\u5236\u89c2\u5bdf\u4f4d\u7f6e")]
        [Tooltip("\u57ce\u533a\u6444\u50cf\u673a\uff1b\u7559\u7a7a\u65f6\u4f1a\u4f7f\u7528\u5f53\u524d\u7269\u4f53\u4e0a\u7684 Camera \u6216\u573a\u666f MainCamera\u3002\u8be5\u7ec4\u4ef6\u53ea\u79fb\u52a8\u6444\u50cf\u673a\uff0c\u4e0d\u5237\u65b0\u57ce\u533a\u6570\u636e\u3002")]
        [SerializeField] private Camera cityCamera;

        [Header("\u9ed8\u8ba4\u89c6\u89d2\uff1a\u56de\u5230\u5168\u5c40\u5730\u56fe")]
        [Tooltip("\u5168\u5c40\u5730\u56fe\u89c2\u5bdf\u70b9\u7a7a\u7269\u4f53\uff1b\u70b9\u51fb\u56de\u5230\u5168\u5c40\u65f6\u6444\u50cf\u673a\u4f1a\u79fb\u52a8\u5230\u8fd9\u91cc\u3002")]
        [SerializeField] private Transform defaultViewPoint;

        [Header("启动校准：打包后强制对齐默认镜头")]
        [Tooltip("启用后会在 Awake 和 Start 都把城区摄像机对齐到默认全局观察点，避免打包后场景初始 Transform 或脚本初始化顺序导致镜头停在错误位置。")]
        [SerializeField] private bool applyDefaultViewOnStart = true;

        [Header("打包保护：禁用误导入摄像机")]
        [Tooltip("启用后会禁用 FBX 或场景中误导入的全屏 3D 摄像机，只保留本组件控制的城区主摄像机输出到玩家画面。")]
        [SerializeField] private bool disableCompetingSceneCameras = true;

        [Header("\u89c2\u5bdf\u70b9\u5217\u8868\uff1a\u6309\u94ae\u79fb\u52a8\u6444\u50cf\u673a")]
        [Tooltip("\u53ef\u5207\u6362\u7684\u57ce\u533a\u89c2\u5bdf\u70b9\uff1b\u6bcf\u4e00\u9879\u5fc5\u987b\u7ed1\u5b9a\u4e00\u4e2a\u7a7a\u7269\u4f53 Transform\uff0c\u70b9\u51fb\u6309\u94ae\u53ea\u79fb\u52a8\u6444\u50cf\u673a\u3002")]
        [SerializeField] private List<CityCameraViewPoint> viewPoints = new List<CityCameraViewPoint>();

        [Header("\u6309\u94ae\u79fb\u52a8\u53c2\u6570\uff1a\u53ef\u5728 Inspector \u8c03\u6574\u624b\u611f")]
        [Tooltip("\u6444\u50cf\u673a\u79fb\u52a8\u5230\u70b9\u4f4d\u6240\u9700\u79d2\u6570\uff1b\u8bbe\u4e3a 0 \u65f6\u7acb\u5373\u8df3\u8f6c\u3002")]
        [SerializeField] private float moveDuration = 0.6f;

        [Tooltip("\u52fe\u9009\u540e\u79fb\u52a8\u7ed3\u675f\u65f6\u540c\u6b65\u70b9\u4f4d\u65cb\u8f6c\uff1b\u7528\u4e8e\u6bcf\u4e2a\u89c2\u5bdf\u70b9\u8bbe\u7f6e\u56fa\u5b9a\u671d\u5411\u3002")]
        [SerializeField] private bool copyTargetRotation = true;

        [Tooltip("\u79fb\u52a8\u63d2\u503c\u66f2\u7ebf\uff1b\u6a2a\u8f74\u4e3a\u65f6\u95f4\u8fdb\u5ea6\uff0c\u7eb5\u8f74\u4e3a\u4f4d\u7f6e\u63d2\u503c\u3002")]
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("\u624b\u52a8\u89c2\u5bdf\uff1aWASD \u5e73\u79fb")]
        [Tooltip("\u662f\u5426\u5141\u8bb8\u73a9\u5bb6\u7528 WASD \u5728\u57ce\u533a\u5730\u56fe\u4e0a\u624b\u52a8\u79fb\u52a8\u6444\u50cf\u673a\uff1b\u53ea\u6539\u53d8\u89c2\u5bdf\u4f4d\u7f6e\uff0c\u4e0d\u5237\u65b0\u57ce\u533a\u6570\u636e\u3002")]
        [SerializeField] private bool enableKeyboardMove = true;

        [Tooltip("WASD \u5e73\u79fb\u901f\u5ea6\uff0c\u5355\u4f4d\u4e3a\u4e16\u754c\u5750\u6807/\u79d2\u3002")]
        [SerializeField] private float keyboardMoveSpeed = 6f;

        [Header("\u624b\u52a8\u89c2\u5bdf\uff1a\u9f20\u6807\u53f3\u952e\u65cb\u8f6c")]
        [Tooltip("\u662f\u5426\u5141\u8bb8\u73a9\u5bb6\u6309\u4f4f\u9f20\u6807\u53f3\u952e\u65cb\u8f6c\u57ce\u533a\u6444\u50cf\u673a\uff1b\u53ea\u6539\u53d8\u89c2\u5bdf\u65b9\u5411\uff0c\u4e0d\u5237\u65b0\u57ce\u533a\u6570\u636e\u3002")]
        [SerializeField] private bool enableRightMouseRotate = true;

        [Tooltip("\u9f20\u6807\u53f3\u952e\u65cb\u8f6c\u7075\u654f\u5ea6\u3002")]
        [SerializeField] private float rightMouseRotateSpeed = 3f;

        [Tooltip("\u6444\u50cf\u673a\u6700\u4f4e\u4fef\u4ef0\u89d2\uff1b\u7528\u4e8e\u907f\u514d\u89c6\u89d2\u7ffb\u8f6c\u6216\u94bb\u5230\u5730\u9762\u4e0b\u65b9\u3002")]
        [SerializeField] private float minPitchAngle = 18f;

        [Tooltip("\u6444\u50cf\u673a\u6700\u9ad8\u4fef\u4ef0\u89d2\uff1b\u7528\u4e8e\u907f\u514d\u89c6\u89d2\u62ac\u5f97\u8fc7\u9ad8\u3002")]
        [SerializeField] private float maxPitchAngle = 70f;

        [Header("\u79fb\u52a8\u8fb9\u754c\uff1a\u4e0d\u80fd\u8d85\u8fc7\u5730\u56fe\u56db\u89d2\u548c\u9ad8\u5ea6")]
        [Tooltip("\u6444\u50cf\u673a\u5141\u8bb8\u79fb\u52a8\u7684\u4e16\u754c\u5750\u6807 X \u6700\u5c0f\u503c\uff0c\u5bf9\u5e94\u5730\u56fe\u5de6\u8fb9\u754c\u3002")]
        [SerializeField] private float minPositionX = -10f;

        [Tooltip("\u6444\u50cf\u673a\u5141\u8bb8\u79fb\u52a8\u7684\u4e16\u754c\u5750\u6807 X \u6700\u5927\u503c\uff0c\u5bf9\u5e94\u5730\u56fe\u53f3\u8fb9\u754c\u3002")]
        [SerializeField] private float maxPositionX = 10f;

        [Tooltip("\u6444\u50cf\u673a\u5141\u8bb8\u79fb\u52a8\u7684\u4e16\u754c\u5750\u6807 Z \u6700\u5c0f\u503c\uff0c\u5bf9\u5e94\u5730\u56fe\u8fd1\u7aef\u8fb9\u754c\u3002")]
        [SerializeField] private float minPositionZ = -12f;

        [Tooltip("\u6444\u50cf\u673a\u5141\u8bb8\u79fb\u52a8\u7684\u4e16\u754c\u5750\u6807 Z \u6700\u5927\u503c\uff0c\u5bf9\u5e94\u5730\u56fe\u8fdc\u7aef\u8fb9\u754c\u3002")]
        [SerializeField] private float maxPositionZ = 4f;

        [Tooltip("\u6444\u50cf\u673a\u5141\u8bb8\u7684\u6700\u4f4e\u9ad8\u5ea6\uff1b\u6309\u94ae\u70b9\u4f4d\u548c WASD \u79fb\u52a8\u90fd\u4f1a\u88ab\u9650\u5236\u5728\u8be5\u9ad8\u5ea6\u4ee5\u4e0a\u3002")]
        [SerializeField] private float minPositionY = 3f;

        [Tooltip("\u6444\u50cf\u673a\u5141\u8bb8\u7684\u6700\u9ad8\u9ad8\u5ea6\uff1b\u7528\u4e8e\u9632\u6b62\u57ce\u533a\u6444\u50cf\u673a\u98de\u5f97\u8fc7\u9ad8\u3002")]
        [SerializeField] private float maxPositionY = 9f;

        [Header("\u8fd0\u884c\u65f6\u8c03\u8bd5\u5feb\u7167\uff1a\u53ea\u8bfb\u89c2\u5bdf\u5f53\u524d\u89c6\u89d2")]
        [Tooltip("\u5f53\u524d\u6444\u50cf\u673a\u89c2\u5bdf\u70b9 ID\uff1b\u7528\u6765\u786e\u8ba4\u6309\u94ae\u662f\u5426\u5207\u6362\u5230\u4e86\u6b63\u786e\u70b9\u4f4d\u3002\u624b\u52a8\u79fb\u52a8\u540e\u4f1a\u663e\u793a manual\u3002")]
        [SerializeField] private string inspectorCurrentViewId;

        [Tooltip("\u5f53\u524d\u6444\u50cf\u673a\u89c2\u5bdf\u70b9\u4e2d\u6587\u540d\uff1b\u7528\u4e8e Play \u6a21\u5f0f\u4e0b\u76f4\u63a5\u89c2\u5bdf\u72b6\u6001\u3002\u624b\u52a8\u79fb\u52a8\u540e\u4f1a\u663e\u793a\u624b\u52a8\u89c2\u5bdf\u3002")]
        [SerializeField] private string inspectorCurrentViewName;

        [Tooltip("\u5f53\u524d\u6444\u50cf\u673a\u662f\u5426\u4ecd\u5728\u6309\u94ae\u79fb\u52a8\u63d2\u503c\u8fc7\u7a0b\u4e2d\u3002")]
        [SerializeField] private bool inspectorIsMoving;

        [Tooltip("\u5f53\u524d\u6444\u50cf\u673a\u7684\u4f4d\u7f6e\u3001\u65cb\u8f6c\u548c\u8fb9\u754c\u6458\u8981\uff1b\u7528\u4e8e\u786e\u8ba4\u6444\u50cf\u673a\u53ea\u662f\u5728\u5141\u8bb8\u8303\u56f4\u5185\u79fb\u52a8\u3002")]
        [SerializeField] private string inspectorTargetSummary;

        [Header("运行时只读快照：摄像机绑定诊断")]
        [Tooltip("当前摄像机绑定诊断；用于打包后确认玩家视角使用的 Camera 是否就是本组件控制的 Camera。")]
        [SerializeField] private string inspectorCameraBindingSnapshot;

        [Header("入场镜头演出：从全局点移动到指定全局点")]
        [Tooltip("是否允许播放城区入场镜头演出；演出只移动观察位置，不刷新城区数据。")]
        [SerializeField] private bool enableEntryCinematic = true;
        [Tooltip("入场镜头从 GlobalViewPoint 移动到目标点所需时长；值越小，移动越快。")]
        [SerializeField] private float entryOrbitDuration = 0.8f;
        [Tooltip("入场镜头目标点物体名称；打开城区面板后摄像机会从 GlobalViewPoint 移动到这个点。")]
        [SerializeField] private string entryCinematicEndObjectName = "GlobalViewPoint (1)";
        [Tooltip("入场镜头结束后要落到的观察点；留空时会按上方物体名称从场景中自动查找。")]
        [SerializeField] private Transform entryCinematicEndViewPoint;
        [Tooltip("当前是否正在播放城区入场镜头。")]
        [SerializeField] private bool inspectorIsPlayingEntryCinematic;

        private Coroutine moveRoutine;
        private Coroutine entryCinematicRoutine;
        private Vector3 lastMousePosition;
        private bool hasLastMousePosition;

        public string CurrentViewId => inspectorCurrentViewId;

        public string CurrentViewName => inspectorCurrentViewName;

        public bool IsMoving => inspectorIsMoving;

        public bool EntryUsesZoom => false;

        public bool DefaultViewUsesExactTransform => true;

        public bool ApplyDefaultViewOnStart => applyDefaultViewOnStart;

        public float EntryOrbitDuration => entryOrbitDuration;

        public float EntryOrbitDegrees => 0f;

        public string EntryCinematicEndViewId => entryCinematicEndObjectName;

        public string EntryCinematicEndObjectName => entryCinematicEndObjectName;

        private void Awake()
        {
            ResolveCamera();
            DisableCompetingCameras();
            ResolveViewPointReferences();
            ApplyStartupDefaultView();
        }

        private void Start()
        {
            DisableCompetingCameras();
            ApplyStartupDefaultView();
        }

        private void OnEnable()
        {
            ResolveViewPointReferences();
            RefreshCameraBindingSnapshot();
        }

        private void ApplyStartupDefaultView()
        {
            if (!applyDefaultViewOnStart)
            {
                return;
            }

            ResolveCamera();
            ResolveViewPointReferences();
            JumpToDefaultView();
        }

        private void Update()
        {
            ResolveCamera();
            if (cityCamera == null)
            {
                return;
            }

            DisableCompetingCameras();

            if (inspectorIsPlayingEntryCinematic)
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

        [ContextMenu("\u79fb\u52a8\u5230\u5168\u5c40\u89c6\u89d2")]
        public void MoveToDefaultView()
        {
            MoveToTarget("global", "\u5168\u5c40\u5730\u56fe", defaultViewPoint);
        }

        [ContextMenu("\u7acb\u5373\u8df3\u5230\u5168\u5c40\u89c6\u89d2")]
        public void JumpToDefaultView()
        {
            JumpToTarget("global", "\u5168\u5c40\u5730\u56fe", defaultViewPoint);
        }

        [ContextMenu("\u6d4b\u8bd5\uff1a\u79fb\u52a8\u5230\u6559\u4f1a (city_church)")]
        private void TestMoveToChurch()
        {
            MoveToViewId("city_church");
        }

        [ContextMenu("\u6d4b\u8bd5\uff1a\u79fb\u52a8\u5230\u738b\u5ba4 (city_royal)")]
        private void TestMoveToRoyal()
        {
            MoveToViewId("city_royal");
        }

        public void MoveToViewId(string viewId)
        {
            Debug.Log($"[城区摄像机] MoveToViewId 调用: viewId=\"{viewId}\", viewPoints数量={viewPoints.Count}, " +
                      $"cityCamera={(cityCamera != null ? cityCamera.gameObject.name : "空")}, " +
                      $"isActiveAndEnabled={isActiveAndEnabled}", this);

            if (string.IsNullOrEmpty(viewId))
            {
                MoveToDefaultView();
                return;
            }

            for (var i = 0; i < viewPoints.Count; i++)
            {
                var point = viewPoints[i];
                if (point == null)
                {
                    Debug.Log($"[城区摄像机] viewPoints[{i}] 为 null，跳过");
                    continue;
                }

                Debug.Log($"[城区摄像机] 检查 viewPoints[{i}]: viewId=\"{point.ViewId}\", " +
                          $"displayName=\"{point.DisplayName}\", target={(point.Target != null ? point.Target.name : "空")}");

                var target = ResolveViewPointTarget(point, viewId);
                if (target != null)
                {
                    Debug.Log($"[城区摄像机] ✓ 在 viewPoints 中找到匹配: {point.ViewId} → {target.name} " +
                              $"位置={target.position}");
                    MoveToTarget(point.ViewId, point.DisplayName, target);
                    return;
                }
            }

            // 依次尝试按钮 targetViewId 的名称变体查找：
            // 先精确匹配，再将 city_church 转换为 ChurchViewPoint 等场景命名。
            foreach (var variant in BuildViewIdVariants(viewId))
            {
                var sceneTarget = FindSceneTransformIncludingInactive(variant);
                if (sceneTarget != null)
                {
                    Debug.Log($"[城区摄像机] ✓ 通过名称变体找到: {variant} → {sceneTarget.name}");
                    MoveToTarget(variant, variant, sceneTarget);
                    return;
                }
            }

            Debug.LogWarning($"[城区摄像机] ✗ 找不到城区摄像机观察点：{viewId}", this);
        }

        /// <summary>
        /// 根据按钮 targetViewId 生成场景查找变体。
        /// 例如 city_church 会生成 ChurchViewPoint、Church、city_church 等候选名。
        /// </summary>
        private static IEnumerable<string> BuildViewIdVariants(string viewId)
        {
            var seen = new HashSet<string>();
            foreach (var variant in BuildViewIdVariantCandidates(viewId))
            {
                if (!string.IsNullOrEmpty(variant) && seen.Add(variant))
                {
                    yield return variant;
                }
            }
        }

        private static IEnumerable<string> BuildViewIdVariantCandidates(string viewId)
        {
            yield return viewId;

            switch (viewId)
            {
                case "city_royal":
                    yield return "RoyalViewPoint";
                    yield return "Royal";
                    yield break;
                case "city_church":
                    yield return "ChurchViewPoint";
                    yield return "Church";
                    yield break;
                case "city_upper":
                    yield return "UpperCityViewPoint";
                    yield return "UpperCity";
                    yield break;
                case "city_academy":
                    yield return "AcademyViewPoint";
                    yield return "Academy";
                    yield break;
                case "city_lower":
                    yield return "LowerCityViewPoint";
                    yield return "LowerCity";
                    yield break;
            }

            var namePart = viewId;
            var underscore = namePart.IndexOf('_');
            if (underscore >= 0 && underscore < namePart.Length - 1)
            {
                namePart = namePart.Substring(underscore + 1);
            }

            if (string.IsNullOrEmpty(namePart))
            {
                yield break;
            }

            var capitalized = char.ToUpperInvariant(namePart[0]) + namePart.Substring(1);
            yield return capitalized + "ViewPoint";
            yield return capitalized;
        }

        public void MoveToViewIndex(int index)
        {
            if (index < 0 || index >= viewPoints.Count)
            {
                Debug.LogWarning($"\u57ce\u533a\u6444\u50cf\u673a\u89c2\u5bdf\u70b9\u5e8f\u53f7\u8d8a\u754c\uff1a{index}", this);
                return;
            }

            var point = viewPoints[index];
            MoveToTarget(point.ViewId, point.DisplayName, ResolveViewPointTarget(point, point.ViewId));
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

        public void PlayEntryCinematic(Action onCompleted = null)
        {
            PlayEntryCinematic(entryOrbitDuration, onCompleted);
        }

        public void PlayEntryCinematic(float durationOverride, Action onCompleted = null)
        {
            ResolveCamera();
            if (cityCamera == null || !enableEntryCinematic)
            {
                onCompleted?.Invoke();
                return;
            }

            StopButtonMove();
            JumpToDefaultView();
            if (entryCinematicRoutine != null)
            {
                StopCoroutine(entryCinematicRoutine);
            }

            entryCinematicRoutine = StartCoroutine(PlayEntryCinematicRoutine(durationOverride, onCompleted));
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
            Debug.Log($"[城区摄像机] MoveToTarget: viewId={viewId}, viewName={viewName}, " +
                      $"target={(target != null ? target.name : "空")}, targetPos={(target != null ? target.position.ToString() : "N/A")}, " +
                      $"cityCamera={(cityCamera != null ? cityCamera.gameObject.name : "空")}", this);

            if (cityCamera == null || target == null)
            {
                Debug.LogWarning($"[城区摄像机] ✗ MoveToTarget 失败: cityCamera={(cityCamera != null ? "有" : "空")}, target={(target != null ? "有" : "空")}", this);
                return;
            }

            StopButtonMove();
            moveRoutine = StartCoroutine(MoveRoutine(viewId, viewName, target));
            Debug.Log($"[城区摄像机] → 已启动 MoveRoutine 协程, moveDuration={moveDuration}");
        }

        private void JumpToTarget(string viewId, string viewName, Transform target)
        {
            ResolveCamera();
            if (cityCamera == null || target == null)
            {
                return;
            }

            StopButtonMove();
            ApplyCameraTargetExact(target);
            SetInspectorSnapshot(viewId, viewName, target, false);
        }

        private void ApplyCameraTargetExact(Transform target)
        {
            cityCamera.transform.SetPositionAndRotation(target.position, target.rotation);
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

            Debug.Log($"[城区摄像机] MoveRoutine 开始: 从 {startPosition} 移动到 {targetPosition}, " +
                      $"duration={duration}s");

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
            Debug.Log($"[城区摄像机] MoveRoutine 完成: 当前位置={cityCamera.transform.position}");
        }

        private void ApplyCameraTarget(Transform target)
        {
            cityCamera.transform.position = target.position;
            if (copyTargetRotation)
            {
                cityCamera.transform.rotation = target.rotation;
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

        private IEnumerator PlayEntryCinematicRoutine(float durationOverride, Action onCompleted)
        {
            inspectorIsPlayingEntryCinematic = true;
            inspectorIsMoving = true;

            var cameraTransform = cityCamera.transform;
            var startPosition = cameraTransform.position;
            var startRotation = cameraTransform.rotation;
            var endTarget = ResolveEntryCinematicEndTarget(out var endViewId, out var endViewName);
            if (endTarget == null)
            {
                inspectorTargetSummary = $"找不到入场镜头目标点：{entryCinematicEndObjectName}";
                inspectorIsPlayingEntryCinematic = false;
                inspectorIsMoving = false;
                entryCinematicRoutine = null;
                onCompleted?.Invoke();
                yield break;
            }

            var targetPosition = endTarget.position;
            var targetRotation = endTarget.rotation;
            var elapsed = 0f;
            var duration = Mathf.Max(0.01f, durationOverride);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / duration);
                var eased = movementCurve != null ? movementCurve.Evaluate(normalizedTime) : normalizedTime;
                cameraTransform.position = Vector3.LerpUnclamped(startPosition, targetPosition, eased);
                if (copyTargetRotation)
                {
                    cameraTransform.rotation = Quaternion.SlerpUnclamped(startRotation, targetRotation, eased);
                }

                inspectorTargetSummary = BuildCameraSummary();
                yield return null;
            }

            ApplyCameraTargetExact(endTarget);
            SetInspectorSnapshot(endViewId, endViewName, endTarget, false);

            inspectorIsPlayingEntryCinematic = false;
            inspectorIsMoving = false;
            entryCinematicRoutine = null;
            onCompleted?.Invoke();
        }

        private Transform ResolveEntryCinematicEndTarget(out string viewId, out string viewName)
        {
            viewId = string.IsNullOrEmpty(entryCinematicEndObjectName) ? "GlobalViewPoint (1)" : entryCinematicEndObjectName;
            viewName = viewId;
            if (entryCinematicEndViewPoint != null)
            {
                return entryCinematicEndViewPoint;
            }

            var configuredTarget = FindConfiguredViewPointTarget(viewId);
            return configuredTarget != null ? configuredTarget : FindSceneTransformIncludingInactive(viewId);
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
                ? "鐩爣鐐逛綅涓虹┖"
                : BuildCameraSummary();
        }

        private void SetManualInspectorSnapshot()
        {
            inspectorCurrentViewId = "manual";
            inspectorCurrentViewName = "\u624b\u52a8\u89c2\u5bdf";
            inspectorIsMoving = false;
            inspectorTargetSummary = BuildCameraSummary();
        }

        private string BuildCameraSummary()
        {
            if (cityCamera == null)
            {
                return "\u6444\u50cf\u673a\u4e3a\u7a7a";
            }

            var cameraTransform = cityCamera.transform;
            return $"浣嶇疆={cameraTransform.position}, 鏃嬭浆={cameraTransform.eulerAngles}, X鑼冨洿=[{Mathf.Min(minPositionX, maxPositionX)}, {Mathf.Max(minPositionX, maxPositionX)}], Z鑼冨洿=[{Mathf.Min(minPositionZ, maxPositionZ)}, {Mathf.Max(minPositionZ, maxPositionZ)}], 楂樺害鑼冨洿=[{Mathf.Min(minPositionY, maxPositionY)}, {Mathf.Max(minPositionY, maxPositionY)}]";
        }

        private void RefreshCameraBindingSnapshot()
        {
            var mainCamera = Camera.main;
            inspectorCameraBindingSnapshot =
                $"控制摄像机={(cityCamera != null ? cityCamera.gameObject.name : "无")}, " +
                $"MainCamera={(mainCamera != null ? mainCamera.gameObject.name : "无")}, " +
                $"同一台={(cityCamera != null && mainCamera != null && cityCamera == mainCamera)}, " +
                $"启用={(cityCamera != null && cityCamera.enabled)}, " +
                $"Depth={(cityCamera != null ? cityCamera.depth : 0f)}, " +
                $"TargetDisplay={(cityCamera != null ? cityCamera.targetDisplay : -1)}";
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

            RefreshCameraBindingSnapshot();
        }

        private void DisableCompetingCameras()
        {
            if (!disableCompetingSceneCameras)
            {
                return;
            }

            ResolveCamera();
            if (cityCamera == null)
            {
                return;
            }

            var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var camera in cameras)
            {
                if (camera == null || camera == cityCamera || !camera.enabled)
                {
                    continue;
                }

                if (camera.targetTexture != null || camera.targetDisplay != cityCamera.targetDisplay)
                {
                    continue;
                }

                camera.enabled = false;
            }
        }

        private void ResolveViewPointReferences()
        {
            if (defaultViewPoint == null)
            {
                defaultViewPoint =
                    FindSceneTransformIncludingInactive("GlobalViewPoint") ??
                    FindSceneTransformIncludingInactive("GlobalViewPoint (1)");
            }

            if (entryCinematicEndViewPoint == null && !string.IsNullOrEmpty(entryCinematicEndObjectName))
            {
                entryCinematicEndViewPoint =
                    FindConfiguredViewPointTarget(entryCinematicEndObjectName) ??
                    FindSceneTransformIncludingInactive(entryCinematicEndObjectName);
            }
        }

        private Transform ResolveViewPointTarget(CityCameraViewPoint point, string requestedId)
        {
            if (point == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(requestedId))
            {
                var matchesRequestedPoint = point.ViewId == requestedId ||
                    (point.Target != null && point.Target.name == requestedId);
                if (!matchesRequestedPoint)
                {
                    return null;
                }

                if (point.Target != null)
                {
                    return point.Target;
                }

                foreach (var variant in BuildViewIdVariants(requestedId))
                {
                    var target = FindSceneTransformIncludingInactive(variant);
                    if (target != null)
                    {
                        return target;
                    }
                }

                return FindSceneTransformIncludingInactive(point.DisplayName);
            }

            if (point.Target != null)
            {
                return point.Target;
            }

            foreach (var variant in BuildViewIdVariants(point.ViewId))
            {
                var target = FindSceneTransformIncludingInactive(variant);
                if (target != null)
                {
                    return target;
                }
            }

            return FindSceneTransformIncludingInactive(point.DisplayName);
        }

        private Transform FindConfiguredViewPointTarget(string viewIdOrObjectName)
        {
            if (string.IsNullOrEmpty(viewIdOrObjectName))
            {
                return null;
            }

            foreach (var point in viewPoints)
            {
                if (point?.Target == null)
                {
                    continue;
                }

                if (point.ViewId == viewIdOrObjectName || point.Target.name == viewIdOrObjectName)
                {
                    return point.Target;
                }
            }

            return null;
        }

        private static Transform FindSceneTransformIncludingInactive(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in transforms)
            {
                if (candidate != null && candidate.name == objectName)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
