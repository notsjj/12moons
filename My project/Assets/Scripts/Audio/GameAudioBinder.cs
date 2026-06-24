using System.Collections.Generic;
using TwelveMoons.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameAudioBinder : MonoBehaviour
{
    public const string LightRainClipName = "小雨";
    public const string HeavyRainClipName = "大雨";
    public const string CloudScatterClipName = "云转场散开";
    public const string CloudGatherClipName = "云转场聚拢";
    public const string DocumentOpenCloseClipName = "公文开启关闭";
    public const string CitySwitchClipName = "切换城区";
    public const string DocumentResultClipName = "处理公文反馈";
    public const string StartPanelGlowClipName = "开始界面光声";
    public const string ButtonClickClipName = "按键声";
    public const string TypewriterClipName = "键盘打字机声";

    [Header("依赖服务：音频管理器和回合阶段")]
    [Tooltip("项目已有 AudioManager；用于注册 Resources/Audio 下的音乐和音效。为空时运行时自动查找。")]
    [SerializeField] private AudioManager audioManager;
    [Tooltip("回合服务；用于读取当前灾难阶段，并在回合变化时切换小雨/大雨环境音。为空时运行时自动查找。")]
    [SerializeField] private RoundService roundService;

    [Header("自动注册：Resources 音频目录")]
    [Tooltip("启用后，进入游戏时自动注册 Resources/Audio/SFX 下的所有音效。")]
    [SerializeField] private bool registerResourcesSfxOnStart = true;
    [Tooltip("启用后，进入游戏时自动注册 Resources/Audio/BGM 下的所有音乐和环境音。")]
    [SerializeField] private bool registerResourcesBgmOnStart = true;

    [Header("环境音：灾难阶段雨声")]
    [Tooltip("启用后，根据当前灾难阶段播放环境雨声：小雨播放“小雨”，大雨和大暴雨播放“大雨”。")]
    [SerializeField] private bool playRainByDisasterStage = true;
    [Tooltip("运行时快照：当前正在播放的环境雨声音频名；非雨阶段为空。")]
    [SerializeField] private string activeEnvironmentClipName;

    [Header("通用按钮音效")]
    [Tooltip("启用后，运行时自动给当前场景中的 Button 追加“按键声”监听，不需要逐个改 Prefab。")]
    [SerializeField] private bool bindButtonClickSfx = true;
    [Tooltip("自动扫描新按钮的间隔秒数；用于处理运行时动态创建的 UI。")]
    [SerializeField, Min(0.05f)] private float buttonScanInterval = 0.5f;
    [Tooltip("运行时快照：已经绑定“按键声”的按钮数量。")]
    [SerializeField] private int boundButtonCountSnapshot;

    [Header("打字机循环音效")]
    [Tooltip("键盘打字机声的循环声源；为空时运行时自动创建，用于从打字机效果开始持续播放到最后一个字输出。")]
    [SerializeField] private AudioSource typewriterLoopSource;
    [Tooltip("键盘打字机声循环播放时的基础音量，会再乘以 AudioManager 当前音效音量。")]
    [SerializeField, Range(0f, 1f)] private float typewriterLoopVolume = 0.75f;
    [Tooltip("运行时快照：键盘打字机声是否正在循环播放。")]
    [SerializeField] private bool isTypewriterLoopPlayingSnapshot;

    private static GameAudioBinder activeInstance;
    private readonly HashSet<Button> boundButtons = new HashSet<Button>();
    private float nextButtonScanTime;

    public bool IsTypewriterLoopPlaying => isTypewriterLoopPlayingSnapshot;

    private void Awake()
    {
        activeInstance = this;
        ResolveReferences();
    }

    private void OnEnable()
    {
        activeInstance = this;
        ResolveReferences();
        if (roundService != null)
        {
            roundService.RoundChanged -= RefreshEnvironmentAudio;
            roundService.RoundChanged += RefreshEnvironmentAudio;
        }
    }

    private void Start()
    {
        RegisterResourcesAudio();
        RefreshEnvironmentAudio();
        ScanAndBindButtons();
    }

    private void Update()
    {
        if (!bindButtonClickSfx || Time.unscaledTime < nextButtonScanTime)
        {
            return;
        }

        nextButtonScanTime = Time.unscaledTime + Mathf.Max(0.05f, buttonScanInterval);
        ScanAndBindButtons();
    }

    private void OnDisable()
    {
        StopTypewriterLoop();
        if (roundService != null)
        {
            roundService.RoundChanged -= RefreshEnvironmentAudio;
        }
    }

    private void OnDestroy()
    {
        StopTypewriterLoop();
        if (activeInstance == this)
        {
            activeInstance = null;
        }
    }

    public void RegisterResourcesAudio()
    {
        ResolveReferences();
        if (audioManager == null)
        {
            Debug.LogWarning("GameAudioBinder 缺少 AudioManager，无法注册 Resources/Audio 音频。", this);
            return;
        }

        if (registerResourcesSfxOnStart)
        {
            audioManager.RegisterResourcesSfx();
        }

        if (registerResourcesBgmOnStart)
        {
            audioManager.RegisterResourcesBgm();
        }
    }

    public void RefreshEnvironmentAudio()
    {
        if (!playRainByDisasterStage)
        {
            StopEnvironmentRain();
            return;
        }

        ResolveReferences();
        var stageName = roundService != null && roundService.CurrentDisasterStage != null
            ? roundService.CurrentDisasterStage.StageName
            : string.Empty;
        ApplyEnvironmentClip(ResolveEnvironmentClipName(stageName));
    }

    public static string ResolveEnvironmentClipName(string disasterStageName)
    {
        if (disasterStageName == LightRainClipName)
        {
            return LightRainClipName;
        }

        if (disasterStageName == HeavyRainClipName || disasterStageName == "大暴雨")
        {
            return HeavyRainClipName;
        }

        return string.Empty;
    }

    public static bool PlayButtonClick()
    {
        return PlaySfx(ButtonClickClipName, true);
    }

    public static bool PlayStartPanelGlow()
    {
        return PlaySfx(StartPanelGlowClipName, true);
    }

    public static bool PlayCloudGather()
    {
        return PlaySfx(CloudGatherClipName, true);
    }

    public static bool PlayCloudScatter()
    {
        return PlaySfx(CloudScatterClipName, true);
    }

    public static bool PlayDocumentOpenClose()
    {
        return PlaySfx(DocumentOpenCloseClipName, true);
    }

    public static bool PlayDocumentResult()
    {
        return PlaySfx(DocumentResultClipName, false);
    }

    public static bool PlayCitySwitch()
    {
        return PlaySfx(CitySwitchClipName, true);
    }

    public static bool PlayTypewriter()
    {
        if (activeInstance != null)
        {
            return activeInstance.PlayTypewriterLoop();
        }

        return PlaySfx(TypewriterClipName, false);
    }

    public static void StopTypewriter()
    {
        if (activeInstance != null)
        {
            activeInstance.StopTypewriterLoop();
            return;
        }

        StopSfx(TypewriterClipName);
    }

    private static bool PlaySfx(string clipName, bool isWait)
    {
        return !string.IsNullOrEmpty(clipName) && AudioManager.PlaySfx(clipName, isWait);
    }

    private static void StopSfx(string clipName)
    {
        if (AudioManager.Instance == null ||
            string.IsNullOrEmpty(clipName) ||
            !AudioManager.Instance.IsAudioRegistered(clipName))
        {
            return;
        }

        AudioManager.Instance.StopAudio(clipName);
    }

    private void ApplyEnvironmentClip(string clipName)
    {
        if (activeEnvironmentClipName == clipName)
        {
            return;
        }

        StopEnvironmentRain();
        activeEnvironmentClipName = clipName ?? string.Empty;
        if (string.IsNullOrEmpty(activeEnvironmentClipName) || audioManager == null)
        {
            return;
        }

        audioManager.TryPlayBgm(activeEnvironmentClipName, true);
    }

    private void StopEnvironmentRain()
    {
        StopRegisteredAudio(LightRainClipName);
        StopRegisteredAudio(HeavyRainClipName);
        activeEnvironmentClipName = string.Empty;
    }

    private void StopRegisteredAudio(string clipName)
    {
        if (audioManager == null || string.IsNullOrEmpty(clipName) || !audioManager.IsAudioRegistered(clipName))
        {
            return;
        }

        audioManager.StopAudio(clipName);
    }

    private bool PlayTypewriterLoop()
    {
        if (!EnsureTypewriterLoopSource())
        {
            return false;
        }

        ApplyTypewriterLoopVolume();
        if (!typewriterLoopSource.isPlaying)
        {
            typewriterLoopSource.Play();
        }

        isTypewriterLoopPlayingSnapshot = true;
        return true;
    }

    private void StopTypewriterLoop()
    {
        if (typewriterLoopSource != null && typewriterLoopSource.isPlaying)
        {
            typewriterLoopSource.Stop();
        }

        isTypewriterLoopPlayingSnapshot = false;
    }

    private bool EnsureTypewriterLoopSource()
    {
        if (typewriterLoopSource == null)
        {
            var sourceObject = new GameObject(TypewriterClipName + "循环");
            sourceObject.transform.SetParent(transform, false);
            typewriterLoopSource = sourceObject.AddComponent<AudioSource>();
        }

        if (typewriterLoopSource.clip == null)
        {
            typewriterLoopSource.clip = Resources.Load<AudioClip>("Audio/SFX/" + TypewriterClipName);
        }

        if (typewriterLoopSource.clip == null)
        {
            Debug.LogWarning("GameAudioBinder 缺少键盘打字机声音频资源，无法循环播放打字机声。", this);
            return false;
        }

        typewriterLoopSource.playOnAwake = false;
        typewriterLoopSource.loop = true;
        ApplyTypewriterLoopVolume();
        return true;
    }

    private void ApplyTypewriterLoopVolume()
    {
        if (typewriterLoopSource == null)
        {
            return;
        }

        var sfxVolume = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;
        typewriterLoopSource.volume = Mathf.Clamp01(typewriterLoopVolume) * sfxVolume;
    }

    private void ScanAndBindButtons()
    {
        if (!bindButtonClickSfx)
        {
            return;
        }

        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var button in buttons)
        {
            if (button == null || boundButtons.Contains(button))
            {
                continue;
            }

            button.onClick.AddListener(() => PlayButtonClick());
            boundButtons.Add(button);
        }

        boundButtons.RemoveWhere(button => button == null);
        boundButtonCountSnapshot = boundButtons.Count;
    }

    private void ResolveReferences()
    {
        if (audioManager == null)
        {
            audioManager = AudioManager.Instance != null
                ? AudioManager.Instance
                : FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
        }

        if (roundService == null)
        {
            roundService = FindFirstObjectByType<RoundService>(FindObjectsInactive.Include);
        }
    }
}
