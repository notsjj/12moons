using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TwelveMoons.UI
{
    public sealed class StartPanelView : MonoBehaviour
    {
        [Header("开始视频")]
        [Tooltip("开始界面使用的视频资源；留空时会从 Resources/Art/Video/开始界面 加载。")]
        [SerializeField] private VideoClip startVideoClip;

        [Tooltip("视频在 Resources 下的默认路径；不包含扩展名，用于自动加载开始界面视频。")]
        [SerializeField] private string defaultVideoResourcePath = "Art/Video/开始界面";

        [Tooltip("开始面板上的视频播放器；留空时会自动查找或补挂。")]
        [SerializeField] private VideoPlayer videoPlayer;

        [Tooltip("显示视频画面的 RawImage；留空时会在开始面板下自动创建并放到最底层。")]
        [SerializeField] private RawImage videoImage;

        [Tooltip("视频渲染纹理宽度；默认匹配当前游戏屏幕 2560x1440，播放时会优先改为视频原生宽度。")]
        [SerializeField, Min(16)] private int renderTextureWidth = 2560;

        [Tooltip("视频渲染纹理高度；默认匹配当前游戏屏幕 2560x1440，播放时会优先改为视频原生高度。")]
        [SerializeField, Min(16)] private int renderTextureHeight = 1440;

        [Header("视频铺屏")]
        [Tooltip("开启后视频会保持原始比例并覆盖整个开始面板，避免被压扁或露出白边。")]
        [SerializeField] private bool coverFullScreenWithoutStretching = true;

        [Header("按钮自动绑定")]
        [Tooltip("名称包含该文本的按钮会绑定为开始游戏：点击后播放完整视频，结束后进入剧情界面。")]
        [SerializeField] private string startButtonName = "开始游戏";

        [Tooltip("名称包含该文本的按钮会绑定为设置按钮；当前阶段只输出提示，不打开新面板。")]
        [SerializeField] private string settingsButtonName = "设置";

        [Tooltip("名称包含该文本的按钮会绑定为退出游戏。")]
        [SerializeField] private string exitButtonName = "退出游戏";

        [Header("运行时只读快照")]
        [Tooltip("显示开始面板当前播放状态，便于在 Inspector 中确认首帧、播放和结束回调是否正常。")]
        [SerializeField] private string inspectorPlaybackSnapshot;
        [Header("视频加载调试：运行时只读")]
        [Tooltip("只读：开始视频是否已经 Prepare 完成并显示首帧。未完成时开始按钮保持不可点击，避免点击后卡顿。")]
        [SerializeField] private bool isVideoPreparedSnapshot;

        private RenderTexture runtimeRenderTexture;
        private Button startButton;
        private Button settingsButton;
        private Button exitButton;
        private Action startCompleted;
        private bool isStarting;
        private bool pendingPlayAfterPrepare;

        public bool IsVideoPrepared => isVideoPreparedSnapshot;

        private void Awake()
        {
            EnsureVideoOutput();
            BindButtonsByName();
        }

        private void OnEnable()
        {
            PrepareFirstFrame();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyVideoImageLayout();
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= HandleFirstFramePrepared;
                videoPlayer.loopPointReached -= HandleVideoFinished;
            }

            if (runtimeRenderTexture != null)
            {
                runtimeRenderTexture.Release();
                Destroy(runtimeRenderTexture);
                runtimeRenderTexture = null;
            }
        }

        public void Initialize(Action onStartCompleted)
        {
            startCompleted = onStartCompleted;
            EnsureVideoOutput();
            BindButtonsByName();
            PrepareFirstFrame();
        }

        public void OnStartGameClicked()
        {
            if (isStarting)
            {
                return;
            }

            isStarting = true;
            GameAudioBinder.PlayStartPanelGlow();
            EnsureVideoOutput();
            if (videoPlayer == null || videoPlayer.clip == null)
            {
                inspectorPlaybackSnapshot = "缺少开始视频，直接进入剧情界面。";
                startCompleted?.Invoke();
                return;
            }

            SetButtonsInteractable(false);
            videoPlayer.loopPointReached -= HandleVideoFinished;
            videoPlayer.loopPointReached += HandleVideoFinished;

            if (!videoPlayer.isPrepared)
            {
                pendingPlayAfterPrepare = true;
                videoPlayer.prepareCompleted -= HandleFirstFramePrepared;
                videoPlayer.prepareCompleted += HandleFirstFramePrepared;
                videoPlayer.Prepare();
                inspectorPlaybackSnapshot = $"开始视频仍在加载，准备完成后自动播放：{videoPlayer.clip.name}";
                return;
            }

            PlayPreparedStartVideo();
        }

        private void PlayPreparedStartVideo()
        {
            if (videoPlayer == null || videoPlayer.clip == null)
            {
                startCompleted?.Invoke();
                return;
            }

            if (videoImage != null)
            {
                videoImage.enabled = true;
            }

            videoPlayer.frame = 0;
            videoPlayer.time = 0d;
            videoPlayer.Play();
            inspectorPlaybackSnapshot = $"开始播放视频：{videoPlayer.clip.name}";
        }

        public void OnSettingsClicked()
        {
            inspectorPlaybackSnapshot = "点击了设置按钮；当前阶段未配置设置面板。";
            Debug.Log("开始面板设置按钮已点击；当前阶段未配置设置面板。", this);
        }

        public void OnExitGameClicked()
        {
            inspectorPlaybackSnapshot = "点击了退出游戏按钮。";
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void EnsureVideoOutput()
        {
            EnsureRootFillsParent();

            if (videoPlayer == null)
            {
                videoPlayer = GetComponentInChildren<VideoPlayer>(true);
            }

            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }

            if (startVideoClip == null && !string.IsNullOrEmpty(defaultVideoResourcePath))
            {
                startVideoClip = Resources.Load<VideoClip>(defaultVideoResourcePath);
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = startVideoClip;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

            ApplyVideoNativeResolution();
            EnsureRenderTexture();
            EnsureVideoImage();
            videoPlayer.targetTexture = runtimeRenderTexture;
            if (videoImage != null)
            {
                videoImage.texture = runtimeRenderTexture;
            }
        }

        private void EnsureRootFillsParent()
        {
            var rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private void ApplyVideoNativeResolution()
        {
            if (startVideoClip == null)
            {
                return;
            }

            if (startVideoClip.width > 0)
            {
                renderTextureWidth = (int)startVideoClip.width;
            }

            if (startVideoClip.height > 0)
            {
                renderTextureHeight = (int)startVideoClip.height;
            }
        }

        private void EnsureRenderTexture()
        {
            var width = Mathf.Max(16, renderTextureWidth);
            var height = Mathf.Max(16, renderTextureHeight);
            if (runtimeRenderTexture != null &&
                runtimeRenderTexture.width == width &&
                runtimeRenderTexture.height == height)
            {
                return;
            }

            if (runtimeRenderTexture != null)
            {
                runtimeRenderTexture.Release();
                Destroy(runtimeRenderTexture);
            }

            runtimeRenderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = $"{name}_开始视频渲染纹理"
            };
            runtimeRenderTexture.Create();
        }

        private void EnsureVideoImage()
        {
            if (videoImage == null)
            {
                var existingImageTransform = transform.Find("开始视频画面");
                if (existingImageTransform != null)
                {
                    videoImage = existingImageTransform.GetComponent<RawImage>();
                }
            }

            if (videoImage == null)
            {
                var imageObject = new GameObject("开始视频画面", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                imageObject.transform.SetParent(transform, false);
                imageObject.transform.SetAsFirstSibling();
                videoImage = imageObject.GetComponent<RawImage>();
            }

            videoImage.raycastTarget = false;
            videoImage.color = Color.white;
            videoImage.enabled = videoPlayer != null && videoPlayer.isPrepared;
            videoImage.transform.SetAsFirstSibling();
            ApplyVideoImageLayout();
        }

        private void ApplyVideoImageLayout()
        {
            if (videoImage == null)
            {
                return;
            }

            var rectTransform = videoImage.transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;

            var aspectFitter = videoImage.GetComponent<AspectRatioFitter>();
            if (coverFullScreenWithoutStretching)
            {
                if (aspectFitter == null)
                {
                    aspectFitter = videoImage.gameObject.AddComponent<AspectRatioFitter>();
                }

                aspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                aspectFitter.aspectRatio = GetVideoAspectRatio();
            }
            else if (aspectFitter != null)
            {
                Destroy(aspectFitter);
            }
        }

        private float GetVideoAspectRatio()
        {
            var width = Mathf.Max(1, renderTextureWidth);
            var height = Mathf.Max(1, renderTextureHeight);
            return width / (float)height;
        }

        private void PrepareFirstFrame()
        {
            EnsureVideoOutput();
            isStarting = false;
            pendingPlayAfterPrepare = false;
            isVideoPreparedSnapshot = false;
            SetButtonsInteractable(false);
            if (videoImage != null)
            {
                videoImage.enabled = false;
            }

            if (videoPlayer == null || videoPlayer.clip == null)
            {
                inspectorPlaybackSnapshot = "未找到开始视频，无法定格首帧。";
                SetButtonsInteractable(true);
                return;
            }

            videoPlayer.prepareCompleted -= HandleFirstFramePrepared;
            videoPlayer.prepareCompleted += HandleFirstFramePrepared;
            videoPlayer.Stop();
            videoPlayer.frame = 0;
            videoPlayer.time = 0d;
            videoPlayer.Prepare();
            inspectorPlaybackSnapshot = $"开始视频准备中，目标首帧：{videoPlayer.clip.name}";
        }

        private void HandleFirstFramePrepared(VideoPlayer source)
        {
            if (source == null || (isStarting && !pendingPlayAfterPrepare))
            {
                return;
            }

            source.frame = 0;
            source.time = 0d;
            source.Play();
            source.Pause();
            isVideoPreparedSnapshot = true;
            if (videoImage != null)
            {
                videoImage.enabled = true;
            }

            if (pendingPlayAfterPrepare)
            {
                pendingPlayAfterPrepare = false;
                PlayPreparedStartVideo();
                return;
            }

            SetButtonsInteractable(true);
            inspectorPlaybackSnapshot = $"开始视频已准备，定格首帧：{source.clip.name}";
        }

        private void BindButtonsByName()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button == null)
                {
                    continue;
                }

                var objectName = button.gameObject.name;
                if (NameMatches(objectName, startButtonName))
                {
                    startButton = BindButton(button, OnStartGameClicked);
                }
                else if (NameMatches(objectName, settingsButtonName))
                {
                    settingsButton = BindButton(button, OnSettingsClicked);
                }
                else if (NameMatches(objectName, exitButtonName))
                {
                    exitButton = BindButton(button, OnExitGameClicked);
                }
            }
        }

        private static Button BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
            return button;
        }

        private static bool NameMatches(string objectName, string keyword)
        {
            return !string.IsNullOrEmpty(objectName) &&
                !string.IsNullOrEmpty(keyword) &&
                objectName.Contains(keyword);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (startButton != null)
            {
                startButton.interactable = interactable;
            }

            if (settingsButton != null)
            {
                settingsButton.interactable = interactable;
            }

            if (exitButton != null)
            {
                exitButton.interactable = interactable;
            }
        }

        private void HandleVideoFinished(VideoPlayer source)
        {
            if (!isStarting)
            {
                return;
            }

            isStarting = false;
            inspectorPlaybackSnapshot = "开始视频播放完成，进入剧情界面。";
            startCompleted?.Invoke();
        }
    }
}
