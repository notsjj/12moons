using System;
using System.IO;
using System.Linq;
using TwelveMoons.UI;
using TwelveMoons.UI.City;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class BaseSceneUIFrameworkSmokeTest
    {
        private static readonly string[] RequiredPrefabPaths =
        {
            "Assets/Resources/Prefabs/UI/桌面面板.prefab",
            "Assets/Resources/Prefabs/UI/开始面板.prefab",
            "Assets/Resources/Prefabs/UI/共享HUD面板.prefab",
            "Assets/Resources/Prefabs/UI/剧情面板.prefab",
            "Assets/Resources/Prefabs/UI/城区HUD面板.prefab",
            "Assets/Resources/Prefabs/UI/公文弹窗面板.prefab",
            "Assets/Resources/Prefabs/UI/报纸面板.prefab",
            "Assets/Resources/Prefabs/UI/信件阅读面板.prefab"
        };

        [MenuItem("Twelve Moons/Tests/Run Base Scene UIFramework Smoke Test")]
        public static void Run()
        {
            var deskPanelType = new UIType("Prefabs/UI/桌面面板", UILayer.Panel);
            if (deskPanelType.Name != "桌面面板")
            {
                throw new InvalidOperationException("UIType 未正确解析 UI 名称。");
            }

            if (deskPanelType.Layer != UILayer.Panel)
            {
                throw new InvalidOperationException("UIType 未正确保存 UI 层级。");
            }

            var backslashPanelType = new UIType(@"Prefabs\UI\桌面面板", UILayer.Panel);
            if (backslashPanelType.Path != "Prefabs/UI/桌面面板" || backslashPanelType.Name != "桌面面板")
            {
                throw new InvalidOperationException("UIType 未正确规范化反斜杠路径。");
            }

            var trimmedPanelType = new UIType(" Prefabs/UI/桌面面板 ", UILayer.Panel);
            if (trimmedPanelType.Path != "Prefabs/UI/桌面面板")
            {
                throw new InvalidOperationException("UIType 未正确裁剪路径前后空白。");
            }

            try
            {
                _ = new UIType("Prefabs/UI/桌面面板/", UILayer.Panel);
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

            ValidateCityHudGlobalButtonTarget();
            ValidateChineseObjectNames("Assets/Resources/Prefabs/UI/共享HUD面板.prefab");
            ValidateChineseObjectNames("Assets/Resources/Prefabs/UI/物品卡片.prefab");
            ValidateSharedHudVisibilityApi();
            ValidateSharedHudLayerOrderSource();
            ValidateSharedHudOpeningSource();
            ValidateLoadingPanelLayerOrderSource();
            ValidateStartPanelOpeningSource();
            ValidateStoryPortraitOutlineApi();
            ValidateStoryPortraitOutlineShader();
            ValidateStoryPortraitGlowOnlyPrefab();
            ValidateStoryPortraitNativeSizeSource();
            ValidateStorySpeakerExpressionDisabledSource();
            ValidateStoryTypewriterClickApi();
            ValidateCityPointPortraitDisplaySource();
            ValidateCityButtonMaskDocumentExitApi();
            ValidateDocumentExitHintBehavior();
            ValidateSuspicionPointerFloatSource();
            ValidateSuspicionPointerFloatSpeed();
            ValidateCityHudSuspicionRows();
            ValidateWorkflowButtonHoverScaleApi();

            Debug.Log("Base Scene UIFramework smoke test passed.");
        }

        private static void ValidateChineseObjectNames(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new FileNotFoundException($"缺少待检查命名的 Prefab：{prefabPath}");
            }

            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (transform != null && ContainsAsciiLetter(transform.name))
                {
                    throw new InvalidOperationException($"{prefabPath} 下仍有非中文物体名：{transform.name}");
                }
            }
        }

        private static bool ContainsAsciiLetter(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            foreach (var character in value)
            {
                if ((character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z'))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateSharedHudVisibilityApi()
        {
            var method = typeof(BaseSceneUIBootstrap).GetMethod(
                "ShowSharedHud",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("BaseSceneUIBootstrap 缺少共享 HUD 显示控制方法。");
            }
        }

        private static void ValidateSharedHudOpeningSource()
        {
            var sourcePath = "Assets/Scripts/UI/BaseSceneUIBootstrap.cs";
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("缺少 BaseSceneUIBootstrap 脚本。");
            }

            var source = File.ReadAllText(sourcePath);
            if (!source.Contains("taskPanel.gameObject.SetActive(showTaskPanel)") ||
                source.Contains("ShowSharedHud(false)") ||
                source.Contains("taskPanel.gameObject.SetActive(true)"))
            {
                throw new InvalidOperationException("共享 HUD 下的任务栏在进入城区前也必须显示，剧情面板不能隐藏任务面板。");
            }

            if (!source.Contains("ShowAndPrepare(DeskPanel);\r\n            ShowSharedHud(true);") &&
                !source.Contains("ShowAndPrepare(DeskPanel);\n            ShowSharedHud(true);"))
            {
                throw new InvalidOperationException("打开桌面面板时必须同步打开共享 HUD 面板，并显示任务面板。");
            }

            if (!source.Contains("ShowAndPrepare(StoryPanel);\r\n            ShowSharedHud(true);") &&
                !source.Contains("ShowAndPrepare(StoryPanel);\n            ShowSharedHud(true);"))
            {
                throw new InvalidOperationException("打开剧情面板时必须先显示剧情面板，再显示共享 HUD 面板和任务面板，确保未进入城区前也能看到任务。");
            }

            if (!source.Contains("ShowSharedHud(true);\r\n            ShowAndPrepare(CityHudPanel);") &&
                !source.Contains("ShowSharedHud(true);\n            ShowAndPrepare(CityHudPanel);"))
            {
                throw new InvalidOperationException("进入城区时必须同时打开共享 HUD 面板和城区 HUD 面板。");
            }
        }

        private static void ValidateSharedHudLayerOrderSource()
        {
            var sourcePath = "Assets/Scripts/UI/BaseSceneUIBootstrap.cs";
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("缺少 BaseSceneUIBootstrap 脚本。");
            }

            var source = File.ReadAllText(sourcePath);
            if (!source.Contains("SharedHudPanel = new UIType(\"Prefabs/UI/共享HUD面板\", UILayer.Overlay)") ||
                !source.Contains("BringSharedHudToFront") ||
                !source.Contains("sharedHudObject.transform.SetAsLastSibling()"))
            {
                throw new InvalidOperationException("共享 HUD 必须显示在 OverlayRoot 并置于同层前方，避免被桌面或剧情面板盖住。");
            }
        }

        private static void ValidateLoadingPanelLayerOrderSource()
        {
            var sourcePath = "Assets/Scripts/UI/BaseSceneUIBootstrap.cs";
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("缺少 BaseSceneUIBootstrap 脚本。");
            }

            var source = File.ReadAllText(sourcePath);
            if (!source.Contains("BringLoadingPanelToFront(loadingObject)") ||
                !source.Contains("overlayRoot?.SetAsLastSibling()") ||
                !source.Contains("loadingObject.transform.SetAsLastSibling()") ||
                !source.Contains("BringActiveLoadingPanelToFront()"))
            {
                throw new InvalidOperationException("加载过场面板必须显示在 OverlayRoot 最上层，并盖住共享 HUD 面板。");
            }
        }

        private static void ValidateStartPanelOpeningSource()
        {
            var sourcePath = "Assets/Scripts/UI/BaseSceneUIBootstrap.cs";
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("缺少 BaseSceneUIBootstrap 脚本。");
            }

            var source = File.ReadAllText(sourcePath);
            if (!source.Contains("StartPanel = new UIType(\"Prefabs/UI/开始面板\", UILayer.Overlay)") ||
                !source.Contains("ShowStartPanel();") ||
                !source.Contains("startPanel.Initialize(EnterStoryFromStartPanel)") ||
                !source.Contains("uiManager?.HideUI(StartPanel)") ||
                !source.Contains("ShowAndPrepare(DeskPanel)") ||
                !source.Contains("deskLoopController?.BeginCurrentRoundFromEntry()") ||
                !source.Contains("ShowStory();"))
            {
                throw new InvalidOperationException("进入游戏时必须先显示开始面板，开始视频播放完成后启动桌面回合流程并进入剧情面板。");
            }

            var deskLoopPath = "Assets/Scripts/UI/DeskLoopController.cs";
            if (!File.Exists(deskLoopPath))
            {
                throw new FileNotFoundException("缺少 DeskLoopController 脚本。");
            }

            var deskLoopSource = File.ReadAllText(deskLoopPath);
            if (!deskLoopSource.Contains("BeginCurrentRoundFromEntry") ||
                !deskLoopSource.Contains("hasStartedInitialRoundFlow") ||
                !deskLoopSource.Contains("BeginCurrentRound();"))
            {
                throw new InvalidOperationException("DeskLoopController 必须提供开始面板结束后的单次当前回合启动入口，避免只打开剧情面板但不播放第一段剧情。");
            }

            var startPanelPath = "Assets/Scripts/UI/StartPanelView.cs";
            if (!File.Exists(startPanelPath))
            {
                throw new FileNotFoundException("缺少 StartPanelView 脚本。");
            }

            var startPanelSource = File.ReadAllText(startPanelPath);
            if (!startPanelSource.Contains("Resources.Load<VideoClip>(defaultVideoResourcePath)") ||
                !startPanelSource.Contains("videoPlayer.frame = 0") ||
                !startPanelSource.Contains("videoPlayer.Play()") ||
                !startPanelSource.Contains("loopPointReached") ||
                !startPanelSource.Contains("renderTextureWidth = 2560") ||
                !startPanelSource.Contains("renderTextureHeight = 1440") ||
                !startPanelSource.Contains("EnsureRootFillsParent") ||
                !startPanelSource.Contains("AspectRatioFitter.AspectMode.EnvelopeParent") ||
                !startPanelSource.Contains("ApplyVideoNativeResolution") ||
                !startPanelSource.Contains("开始游戏") ||
                !startPanelSource.Contains("退出游戏") ||
                !startPanelSource.Contains("设置"))
            {
                throw new InvalidOperationException("StartPanelView 必须绑定开始面板按钮、定格视频首帧，并在点击开始后全屏无拉伸播放完整视频。");
            }
        }

        private static void ValidateStoryPortraitOutlineApi()
        {
            var method = typeof(StoryPanelView).GetMethod(
                "ApplyPortraitOutlineEffects",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("StoryPanelView 缺少直接作用在角色图片上的白色描边晕圈刷新方法。");
            }

            var runtimeObjectFactory = typeof(StoryPanelView).GetMethod(
                "EnsurePortraitVignetteImage",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (runtimeObjectFactory != null)
            {
                throw new InvalidOperationException("StoryPanelView 不应再运行时创建剧情人物晕影物体。");
            }
        }

        private static void ValidateStoryPortraitOutlineShader()
        {
            var shaderPath = "Assets/Shaders/PortraitAlphaOutline.shader";
            if (!File.Exists(shaderPath))
            {
                throw new FileNotFoundException("缺少剧情人物白色描边 Shader。");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/UI/PortraitAlphaOutlineRuntime.mat");
            if (material == null || material.shader == null || material.shader.name != "TwelveMoons/UI/PortraitAlphaOutline")
            {
                throw new InvalidOperationException("剧情人物白色描边晕圈必须有 Resources 材质硬引用，避免打包后 Shader.Find 找不到 Shader。");
            }

            var shaderSource = File.ReadAllText(shaderPath);
            if (!shaderSource.Contains("pow(radialFade, glowFalloffPower)"))
            {
                throw new InvalidOperationException("剧情人物光晕 Shader 应使用曲线衰减，让外缘自然淡出。");
            }
        }

        private static void ValidateStoryPortraitGlowOnlyPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/剧情面板.prefab");
            if (prefab == null)
            {
                throw new FileNotFoundException("缺少剧情面板 Prefab。");
            }

            var panel = prefab.GetComponent<StoryPanelView>();
            if (panel == null)
            {
                throw new InvalidOperationException("剧情面板缺少 StoryPanelView。");
            }

            var serializedPanel = new SerializedObject(panel);
            var activeOutline = serializedPanel.FindProperty("activePortraitOutlineColor");
            var inactiveOutline = serializedPanel.FindProperty("inactivePortraitOutlineColor");
            var activeGlow = serializedPanel.FindProperty("activePortraitGlowColor");
            var glowWidth = serializedPanel.FindProperty("portraitGlowPixelWidth");
            var glowIntensity = serializedPanel.FindProperty("portraitGlowIntensity");
            var glowFalloffPower = serializedPanel.FindProperty("portraitGlowFalloffPower");
            if (activeOutline == null ||
                inactiveOutline == null ||
                activeGlow == null ||
                glowWidth == null ||
                glowIntensity == null ||
                glowFalloffPower == null)
            {
                throw new InvalidOperationException("剧情面板缺少人物光晕参数，无法检查无描边光晕效果。");
            }

            if (activeOutline.colorValue.a > 0.01f || inactiveOutline.colorValue.a > 0.01f)
            {
                throw new InvalidOperationException("剧情人物白色效果应只保留光晕，描边透明度必须为 0。");
            }

            if (glowWidth.intValue < 28)
            {
                throw new InvalidOperationException($"剧情人物自然光晕宽度应至少为 28，当前为 {glowWidth.intValue}。");
            }

            if (activeGlow.colorValue.a > 0.5f)
            {
                throw new InvalidOperationException($"剧情人物光晕透明度峰值不应过高，当前为 {activeGlow.colorValue.a}。");
            }

            if (glowIntensity.floatValue > 0.8f)
            {
                throw new InvalidOperationException($"剧情人物光晕强度不应过硬，当前为 {glowIntensity.floatValue}。");
            }

            if (glowFalloffPower.floatValue < 2f)
            {
                throw new InvalidOperationException($"剧情人物光晕应使用更柔和的边缘衰减，当前为 {glowFalloffPower.floatValue}。");
            }
        }

        private static void ValidateStoryTypewriterClickApi()
        {
            var revealMethod = typeof(StoryPanelView).GetMethod(
                "RevealTypewriterIfNeeded",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var bindingMethod = typeof(StoryPanelView).GetMethod(
                "EnsureStoryAreaButtonBinding",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (revealMethod == null || bindingMethod == null)
            {
                throw new InvalidOperationException("StoryPanelView 缺少点击剧情面板快速显示完整打字机内容的兜底逻辑。");
            }
        }

        private static void ValidateStoryPortraitNativeSizeSource()
        {
            var sourcePath = "Assets/Scripts/UI/StoryPanelView.cs";
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("缺少剧情面板脚本。");
            }

            var source = File.ReadAllText(sourcePath);
            if (!source.Contains("portrait.SetNativeSize()"))
            {
                throw new InvalidOperationException("剧情人物立绘替换 Sprite 后必须设置为原生大小。");
            }
        }

        private static void ValidateStorySpeakerExpressionDisabledSource()
        {
            var sourcePath = "Assets/Scripts/UI/StoryPanelView.cs";
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("缺少剧情面板脚本。");
            }

            var source = File.ReadAllText(sourcePath);
            if (source.Contains("SetPortraitSprite(speakerExpressionImage"))
            {
                throw new InvalidOperationException("剧情面板说话者表情图暂时不应关联刷新成人物立绘。");
            }

            if (source.Contains("speakerExpressionImage.enabled = false") ||
                source.Contains("speakerExpressionImage.sprite =") ||
                source.Contains("speakerExpressionImage.material ="))
            {
                throw new InvalidOperationException("剧情面板说话者表情图应保留 Prefab 原图，代码不应修改它的图片、显隐或材质。");
            }
        }

        private static void ValidateCityPointPortraitDisplaySource()
        {
            var sourcePath = "Assets/Scripts/City/CityPointView.cs";
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("缺少城区点位视图脚本。");
            }

            var source = File.ReadAllText(sourcePath);
            if (!source.Contains("RefreshPortraitDisplay") ||
                !source.Contains("SetAllPortraitsVisible") ||
                !source.Contains("CharacterPlaceholderPortraitProvider.LoadPortrait") ||
                !source.Contains("new GameObject(\"") ||
                !source.Contains("FacePortraitToCamera") ||
                !source.Contains("Quaternion.LookRotation") ||
                !source.Contains("new Vector3(0.058333f, 0.058333f, 0.058333f)") ||
                !source.Contains("portraitMapScaleMultiplier = 0.333333f") ||
                !source.Contains("portraitLocalScale * portraitMapScaleMultiplier") ||
                !source.Contains("OnDisable()"))
            {
                throw new InvalidOperationException("CityPointView 应在当前阶段进入城区后为所有点位运行时显示三分之一尺寸人物立绘，退出城区隐藏，并持续面向摄像机。");
            }
        }

        private static void ValidateCityButtonMaskDocumentExitApi()
        {
            var method = typeof(DeskLoopController).GetMethod(
                "OpenCityButtonMasksAfterDocumentExit",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("DeskLoopController 缺少公文退出后打开城区按钮遮罩的方法。");
            }

            var sourcePath = "Assets/Scripts/UI/DeskLoopController.cs";
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("缺少 DeskLoopController 脚本。");
            }

            var source = File.ReadAllText(sourcePath);
            if (!source.Contains("!wasDocumentFlowActive && isDocumentFlowActive") ||
                !source.Contains("wasDocumentFlowActive && !isDocumentFlowActive && !HasPendingDocuments()") ||
                !source.Contains("DOAnchorPos(leftOpenPosition, duration)") ||
                !source.Contains("DOAnchorPos(rightOpenPosition, duration)") ||
                !File.ReadAllText("Assets/Scripts/UI/DocumentPopupPanelView.cs").Contains("isFeedbackTypewriterPlaying || waitingForContinue"))
            {
                throw new InvalidOperationException("城区按钮遮罩必须在公文打开和中途选项后保持闭合，只在公文全部结束后用过渡动画拉开。");
            }
        }

        private static void ValidateDocumentExitHintBehavior()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/公文弹窗面板.prefab");
            if (prefab == null)
            {
                throw new FileNotFoundException("缺少公文弹窗面板 Prefab。");
            }

            var popup = prefab.GetComponent<DocumentPopupPanelView>();
            if (popup == null)
            {
                throw new InvalidOperationException("公文弹窗面板缺少 DocumentPopupPanelView。");
            }

            var hint = prefab
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate != null && candidate.name == "提示图片");
            if (hint == null)
            {
                throw new InvalidOperationException("公文弹窗面板下缺少“提示图片”。");
            }

            if (hint.gameObject.activeSelf)
            {
                throw new InvalidOperationException("公文弹窗面板下的提示图片必须默认关闭，只能在所有公文处理完毕后显示。");
            }

            var serializedPopup = new SerializedObject(popup);
            var exitHintProperty = serializedPopup.FindProperty("exitHintImage");
            if (exitHintProperty == null || exitHintProperty.objectReferenceValue != hint.gameObject)
            {
                throw new InvalidOperationException("DocumentPopupPanelView 的 exitHintImage 必须绑定到公文弹窗面板下的“提示图片”。");
            }

            var hintGraphic = hint.GetComponent<UnityEngine.UI.Graphic>();
            if (hintGraphic != null && hintGraphic.raycastTarget)
            {
                throw new InvalidOperationException("提示图片不能拦截拖拽射线，否则玩家拖动公文面板时可能无法触发关闭。");
            }

            var sourcePath = "Assets/Scripts/UI/DocumentPopupPanelView.cs";
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("缺少 DocumentPopupPanelView 脚本。");
            }

            var source = File.ReadAllText(sourcePath);
            var hidesHintWhenDragStarts =
                source.Contains("HideExitHint();\r\n            isDraggingExit = true") ||
                source.Contains("HideExitHint();\n            isDraggingExit = true");
            if (!source.Contains("AutoBindExitHintImage") ||
                !source.Contains("transform.Find(\"提示图片\")") ||
                !hidesHintWhenDragStarts ||
                !source.Contains("ShowExitHint();"))
            {
                throw new InvalidOperationException("提示图片必须能自动绑定，只在全部公文结束等待拖出时显示，并在玩家开始拖动时隐藏。");
            }
        }

        private static void ValidateSuspicionPointerFloatSource()
        {
            var sourcePath = "Assets/Scripts/UI/Faction/SuspicionPanelView.cs";
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("缺少质疑度面板脚本。");
            }

            var source = File.ReadAllText(sourcePath);
            if (source.Contains("DOPunchRotation"))
            {
                throw new InvalidOperationException("质疑度指针不应再使用旋转摆动。");
            }

            if (!source.Contains("DOLocalMoveY"))
            {
                throw new InvalidOperationException("质疑度指针应恢复使用本地 Y 轴移动来对齐质疑行。");
            }

            if (!source.Contains("DOPunchPosition"))
            {
                throw new InvalidOperationException("质疑度指针应使用 Y 轴位置浮动。");
            }
        }

        private static void ValidateSuspicionPointerFloatSpeed()
        {
            ValidateSuspicionPanelPointerFloatSpeed("Assets/Resources/Prefabs/UI/桌面面板.prefab");
            ValidateSuspicionPanelPointerFloatSpeed("Assets/Resources/Prefabs/UI/城区HUD面板.prefab");
        }

        private static void ValidateCityHudSuspicionRows()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/城区HUD面板.prefab");
            if (prefab == null)
            {
                throw new FileNotFoundException("缺少城区 HUD 面板 Prefab。");
            }

            var rows = prefab.GetComponentsInChildren<FactionSuspicionRow>(true);
            if (rows.Length < 4)
            {
                throw new InvalidOperationException($"城区 HUD 面板应包含四个质疑度行，当前为 {rows.Length} 个。");
            }

            var cityOverlayPanel = prefab.GetComponent<CityOverlayPanelView>();
            if (cityOverlayPanel == null)
            {
                throw new InvalidOperationException("城区 HUD 面板根物体必须直接挂 CityOverlayPanelView，用于按四个质疑行同步 Slider 数值。");
            }

            var sourcePath = "Assets/Scripts/UI/City/CityOverlayPanelView.cs";
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("缺少城区 HUD 视图脚本。");
            }

            var source = File.ReadAllText(sourcePath);
            if (!source.Contains("citySuspicionSliders") ||
                !source.Contains("CitySuspicionFactionIds") ||
                !source.Contains("noble质疑行") ||
                !source.Contains("academy质疑行") ||
                !source.Contains("church质疑行") ||
                !source.Contains("civilian质疑行") ||
                !source.Contains("FindChildByName") ||
                !source.Contains("RefreshCitySuspicionSliders") ||
                !source.Contains("RuntimeDataService") ||
                !source.Contains("FactionService") ||
                !source.Contains("FactionsChanged") ||
                !source.Contains("SetValueWithoutNotify") ||
                source.Contains("citySuspicionPanel?.Refresh()") ||
                source.Contains("BindCitySuspicionRowSliders"))
            {
                throw new InvalidOperationException("城区 HUD 的四个质疑 Slider 必须由 CityOverlayPanelView 独立读取阵营运行时数据并实时写入，不能复用桌面质疑行刷新逻辑。");
            }
        }

        private static void ValidateSuspicionPanelPointerFloatSpeed(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new FileNotFoundException($"缺少待检查质疑指针速度的 Prefab：{prefabPath}");
            }

            var panel = prefab.GetComponentInChildren<SuspicionPanelView>(true);
            if (panel == null)
            {
                throw new InvalidOperationException($"{prefabPath} 缺少 SuspicionPanelView。");
            }

            if (Mathf.Abs(panel.PointerSwingStepDuration - 0.2f) > 0.001f)
            {
                throw new InvalidOperationException($"{prefabPath} 的质疑指针浮动速度应为 0.2，当前为 {panel.PointerSwingStepDuration}。");
            }
        }

        private static void ValidateWorkflowButtonHoverScaleApi()
        {
            var method = typeof(DeskLoopController).GetMethod(
                "EnsureWorkflowButtonHoverScaleEffect",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("DeskLoopController 缺少公文按钮和报纸按钮悬停放大效果补挂方法。");
            }

            if (typeof(ButtonAnim) == null)
            {
                throw new InvalidOperationException("缺少现成按钮悬停放大脚本 ButtonAnim。");
            }
        }

        private static void ValidateCityHudGlobalButtonTarget()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/城区HUD面板.prefab");
            if (prefab == null)
            {
                throw new FileNotFoundException("缺少城区 HUD 面板 Prefab。");
            }

            var globalButton = prefab
                .GetComponentsInChildren<CityCameraControlButton>(true)
                .FirstOrDefault(button => button != null && button.DisplayName == "全局");
            if (globalButton == null)
            {
                throw new InvalidOperationException("城区 HUD 面板缺少“全局”摄像机按钮。");
            }

            if (globalButton.TargetViewId != "GlobalViewPoint")
            {
                throw new InvalidOperationException($"城区 HUD 全局按钮应绑定到 GlobalViewPoint，当前为 {globalButton.TargetViewId}。");
            }
        }
    }
}
