using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace TwelveMoons.UI.Editor
{
    [CustomEditor(typeof(LoadingPanelTransitionView))]
    public sealed class LoadingPanelTransitionViewEditor : UnityEditor.Editor
    {
        private static LoadingPanelTransitionView previewTarget;
        private static double previewPhaseStartTime;
        private static PreviewPhase previewPhase = PreviewPhase.None;

        private enum PreviewPhase
        {
            None,
            Close,
            Hold,
            Open
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("\u52a8\u753b\u8c03\u8bd5", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "\u8fd0\u884c\u6e38\u620f\u540e\uff0c\u53ef\u4ee5\u5728\u8fd9\u91cc\u91cd\u64ad LoadingPanel \u7684\u5206\u5c42\u8fc7\u573a\u52a8\u753b\u3002\u8c03\u8bd5\u6309\u94ae\u4f1a\u4f18\u5148\u64ad\u653e\u573a\u666f\u4e2d\u7684\u8fd0\u884c\u65f6\u5b9e\u4f8b\uff0c\u4e0d\u76f4\u63a5\u5bf9 Prefab \u8d44\u6e90\u672c\u4f53\u64ad\u653e\u3002",
                MessageType.Info);

            if (GUILayout.Button(Application.isPlaying
                    ? "\u64ad\u653e\u573a\u666f\u4e2d\u7684\u52a0\u8f7d\u8fc7\u573a\u52a8\u753b"
                    : "\u5728 Prefab \u7f16\u8f91\u72b6\u6001\u9884\u89c8\u52a0\u8f7d\u8fc7\u573a\u52a8\u753b"))
            {
                if (Application.isPlaying)
                {
                    var view = ResolvePlaybackTarget((LoadingPanelTransitionView)target);
                    if (view != null)
                    {
                        view.PlayDebugTransition();
                        EditorUtility.SetDirty(view);
                        Selection.activeObject = view.gameObject;
                    }
                }
                else
                {
                    StartEditorPreview(ResolveEditorPreviewTarget((LoadingPanelTransitionView)target));
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "\u4e0d\u8fdb\u5165 Play \u6a21\u5f0f\u4e5f\u53ef\u4ee5\u9884\u89c8\uff0c\u64ad\u653e\u5b8c\u6210\u540e\u4f1a\u81ea\u52a8\u56de\u5230\u521d\u59cb\u72b6\u6001\uff0c\u4e0d\u5e94\u8be5\u628a\u64ad\u653e\u4e2d\u72b6\u6001\u7559\u5728 Prefab \u91cc\u3002",
                    MessageType.None);
            }
            else
            {
                var targetView = ResolvePlaybackTarget((LoadingPanelTransitionView)target);
                if (targetView == null)
                {
                    EditorGUILayout.HelpBox(
                        "\u6ca1\u6709\u627e\u5230\u573a\u666f\u4e2d\u7684 LoadingPanelTransitionView \u8fd0\u884c\u65f6\u5b9e\u4f8b\u3002\u8bf7\u5148\u8ba9 LoadingPanel \u51fa\u73b0\u5728\u573a\u666f\u91cc\u3002",
                        MessageType.Warning);
                }
                else if (EditorUtility.IsPersistent(target))
                {
                    EditorGUILayout.HelpBox(
                        "\u4f60\u5f53\u524d\u9009\u4e2d\u7684\u662f Prefab \u8d44\u6e90\u3002\u6309\u94ae\u70b9\u51fb\u540e\u4f1a\u81ea\u52a8\u5bf9\u573a\u666f\u4e2d\u7684\u8fd0\u884c\u65f6\u5b9e\u4f8b\u64ad\u653e\u3002",
                        MessageType.None);
                }
            }
        }

        private static void StartEditorPreview(LoadingPanelTransitionView targetView)
        {
            if (targetView == null)
            {
                return;
            }

            StopEditorPreview();
            previewTarget = targetView;
            previewTarget.EditorPreviewBegin();
            ForcePreviewRefresh(previewTarget);
            previewPhase = PreviewPhase.Close;
            previewPhaseStartTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += UpdateEditorPreview;
            SceneView.RepaintAll();
        }

        private static void StopEditorPreview()
        {
            EditorApplication.update -= UpdateEditorPreview;
            if (previewTarget != null)
            {
                previewTarget.EditorPreviewEnd();
                ForcePreviewRefresh(previewTarget);
            }

            previewTarget = null;
            previewPhase = PreviewPhase.None;
        }

        private static void UpdateEditorPreview()
        {
            if (previewTarget == null)
            {
                StopEditorPreview();
                return;
            }

            var elapsed = (float)(EditorApplication.timeSinceStartup - previewPhaseStartTime);
            switch (previewPhase)
            {
                case PreviewPhase.Close:
                    UpdateClosePhase(elapsed);
                    break;
                case PreviewPhase.Hold:
                    UpdateHoldPhase(elapsed);
                    break;
                case PreviewPhase.Open:
                    UpdateOpenPhase(elapsed);
                    break;
            }

            ForcePreviewRefresh(previewTarget);
            SceneView.RepaintAll();
            InternalEditorUtility.RepaintAllViews();
        }

        private static void UpdateClosePhase(float elapsed)
        {
            var duration = Mathf.Max(0.01f, previewTarget.CloseDuration);
            var progress = Mathf.Clamp01(elapsed / duration);
            previewTarget.EditorPreviewSampleClose(progress);
            if (progress >= 1f)
            {
                previewTarget.EditorPreviewHoldCovered();
                previewPhase = PreviewPhase.Hold;
                previewPhaseStartTime = EditorApplication.timeSinceStartup;
            }
        }

        private static void UpdateHoldPhase(float elapsed)
        {
            if (elapsed < previewTarget.CoveredHoldDuration)
            {
                return;
            }

            previewPhase = PreviewPhase.Open;
            previewPhaseStartTime = EditorApplication.timeSinceStartup;
        }

        private static void UpdateOpenPhase(float elapsed)
        {
            var duration = Mathf.Max(0.01f, previewTarget.OpenDuration);
            var progress = Mathf.Clamp01(elapsed / duration);
            previewTarget.EditorPreviewSampleOpen(progress);
            if (progress >= 1f)
            {
                StopEditorPreview();
            }
        }

        private static LoadingPanelTransitionView ResolvePlaybackTarget(LoadingPanelTransitionView editorTarget)
        {
            if (editorTarget != null && !EditorUtility.IsPersistent(editorTarget) && editorTarget.gameObject.scene.IsValid())
            {
                return editorTarget;
            }

            var candidates = Object.FindObjectsByType<LoadingPanelTransitionView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in candidates)
            {
                if (candidate == null || !candidate.gameObject.scene.IsValid())
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private static LoadingPanelTransitionView ResolveEditorPreviewTarget(LoadingPanelTransitionView editorTarget)
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage?.prefabContentsRoot != null)
            {
                var stageTarget = prefabStage.prefabContentsRoot.GetComponentInChildren<LoadingPanelTransitionView>(true);
                if (stageTarget != null)
                {
                    return stageTarget;
                }
            }

            if (editorTarget != null && !EditorUtility.IsPersistent(editorTarget) && editorTarget.gameObject.scene.IsValid())
            {
                return editorTarget;
            }

            return null;
        }

        private static void ForcePreviewRefresh(LoadingPanelTransitionView targetView)
        {
            if (targetView == null)
            {
                return;
            }

            var rootRect = targetView.transform as RectTransform;
            if (rootRect != null)
            {
                rootRect.ForceUpdateRectTransforms();
                EditorUtility.SetDirty(rootRect);
            }

            var childRects = targetView.GetComponentsInChildren<RectTransform>(true);
            foreach (var rect in childRects)
            {
                if (rect == null)
                {
                    continue;
                }

                rect.ForceUpdateRectTransforms();
                EditorUtility.SetDirty(rect);
            }

            var canvas = targetView.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Canvas.ForceUpdateCanvases();
                EditorUtility.SetDirty(canvas);
            }

            EditorUtility.SetDirty(targetView);
        }
    }
}
