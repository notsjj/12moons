using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public static class AudioVolumeSettings
{
    public const float DefaultMusicVolume = 1f;
    public const float DefaultSfxVolume = 1f;

    public static float Normalize(float volume)
    {
        return Mathf.Clamp01(volume);
    }
}

public class AudioManager : Singleton<AudioManager>
{
    private const string DefaultResourcesSfxPath = "Audio/SFX";
    private const string DefaultResourcesBgmPath = "Audio/BGM";
    private const string DefaultBgmClipName = "cultivation_bgm_fast_02";
    private const string MusicVolumePlayerPrefsKey = "Audio.MusicVolume";
    private const string SfxVolumePlayerPrefsKey = "Audio.SfxVolume";

    [System.Serializable]
    public class Sound
    {
        [Header("音频剪辑")]
        public AudioClip clip;
        [Header("音频分组")]
        public AudioMixerGroup outputGroup;
        [Header("音频音量")]
        [Range(0, 1)]
        public float volume;
        [Header("开局播放")]
        public bool playOnAwake;
        [Header("循环")]
        public bool loop;
    }

    //存储所有的音频信息
    public List<Sound> sounds;

    [Header("Resources音效目录")]
    [SerializeField] private string resourcesSfxPath = DefaultResourcesSfxPath;
    [Header("自动加载Resources音效")]
    [SerializeField] private bool loadResourcesSfxOnStart = false;
    [Header("Resources音效默认音量")]
    [Range(0, 1)]
    [SerializeField] private float resourcesSfxVolume = 0.75f;

    [Header("Resources BGM Path")]
    [SerializeField] private string resourcesBgmPath = DefaultResourcesBgmPath;
    [Header("Load Resources BGM On Start")]
    [SerializeField] private bool loadResourcesBgmOnStart = false;
    [Header("Play Default BGM On Start")]
    [SerializeField] private bool playDefaultBgmOnStart = true;
    [Header("Default BGM Name")]
    [SerializeField] private string defaultBgmName = DefaultBgmClipName;
    [Header("Resources BGM Volume")]
    [Range(0, 1)]
    [SerializeField] private float resourcesBgmVolume = 0.35f;

    //每一个音频剪辑的名称对应一个音频组件
    public Dictionary<string, AudioSource> audiosDic;
    private readonly Dictionary<string, float> baseVolumes = new Dictionary<string, float>();
    private float musicVolume = AudioVolumeSettings.DefaultMusicVolume;
    private float sfxVolume = AudioVolumeSettings.DefaultSfxVolume;

    public float MusicStartSecond => Random.Range(5f, 15f);
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    protected override void Awake()
    {
        base.Awake();

        audiosDic = new Dictionary<string, AudioSource>();
        musicVolume = AudioVolumeSettings.Normalize(PlayerPrefs.GetFloat(MusicVolumePlayerPrefsKey, AudioVolumeSettings.DefaultMusicVolume));
        sfxVolume = AudioVolumeSettings.Normalize(PlayerPrefs.GetFloat(SfxVolumePlayerPrefsKey, AudioVolumeSettings.DefaultSfxVolume));
    }

    private void Start()
    {
        if (sounds != null)
        {
            foreach (var sound in sounds)
                RegisterSound(sound);
        }

        if (loadResourcesSfxOnStart)
            RegisterResourcesSfx();

        if (loadResourcesBgmOnStart)
            RegisterResourcesBgm();

        if (playDefaultBgmOnStart)
            TryPlayBgm(defaultBgmName);
    }

    public void RegisterResourcesSfx()
    {
        RegisterResourcesSfx(resourcesSfxPath);
    }

    public void RegisterResourcesSfx(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        AudioClip[] clips = Resources.LoadAll<AudioClip>(path);
        for (int i = 0; i < clips.Length; i++)
            RegisterClip(clips[i], resourcesSfxVolume);
    }

    public void RegisterResourcesBgm()
    {
        RegisterResourcesBgm(resourcesBgmPath);
    }

    public void RegisterResourcesBgm(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        AudioClip[] clips = Resources.LoadAll<AudioClip>(path);
        for (int i = 0; i < clips.Length; i++)
            RegisterClip(clips[i], resourcesBgmVolume, true);
    }

    public void RegisterClip(AudioClip clip, float volume = 1f, bool loop = false, AudioMixerGroup outputGroup = null, bool playOnAwake = false)
    {
        if (!clip)
            return;

        Sound sound = new Sound
        {
            clip = clip,
            volume = Mathf.Clamp01(volume),
            loop = loop,
            outputGroup = outputGroup,
            playOnAwake = playOnAwake
        };
        RegisterSound(sound);
    }

    public bool IsAudioRegistered(string name)
    {
        return audiosDic != null && audiosDic.ContainsKey(name);
    }

    public bool TryPlayAudio(string name, bool isWait = false)
    {
        if (!IsAudioRegistered(name))
            RegisterResourceClip(name, resourcesSfxPath, resourcesSfxVolume, false);

        return TryPlayRegisteredAudio(name, isWait);
    }

    private bool TryPlayRegisteredAudio(string name, bool isWait = false)
    {
        if (!IsAudioRegistered(name))
            return false;

        AudioSource source = audiosDic[name];
        if (!source || !source.clip)
            return false;

        if (isWait && source.isPlaying)
            return true;

        if (source.loop)
            source.Play();
        else
            source.PlayOneShot(source.clip, 1f);

        return true;
    }

    public bool TryPlayBgm(string name, bool isWait = true)
    {
        if (!IsAudioRegistered(name))
            RegisterResourceClip(name, resourcesBgmPath, resourcesBgmVolume, true);

        return TryPlayRegisteredAudio(name, isWait);
    }

    public static bool PlaySfx(string name, bool isWait = false)
    {
        return Instance && Instance.TryPlayAudio(name, isWait);
    }

    public static bool PlayBgm(string name, bool isWait = true)
    {
        return Instance && Instance.TryPlayBgm(name, isWait);
    }

    private void RegisterSound(Sound sound)
    {
        if (sound == null || !sound.clip)
            return;

        if (audiosDic == null)
            audiosDic = new Dictionary<string, AudioSource>();

        string clipName = sound.clip.name;
        if (audiosDic.TryGetValue(clipName, out AudioSource existing) && existing)
        {
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
            audiosDic.Remove(clipName);
        }

        GameObject obj = new GameObject(clipName);
        obj.transform.SetParent(transform);

        AudioSource source = obj.AddComponent<AudioSource>();
        source.clip = sound.clip;
        source.playOnAwake = sound.playOnAwake;
        source.loop = sound.loop;
        baseVolumes[clipName] = AudioVolumeSettings.Normalize(sound.volume);
        ApplySourceVolume(clipName, source);
        source.outputAudioMixerGroup = sound.outputGroup;

        if (source.playOnAwake)
            source.Play();

        audiosDic.Add(clipName, source);
    }

    private bool RegisterResourceClip(string clipName, string path, float volume, bool loop)
    {
        if (string.IsNullOrEmpty(clipName) || string.IsNullOrEmpty(path))
            return false;

        AudioClip clip = Resources.Load<AudioClip>($"{path}/{clipName}");
        if (!clip)
            return false;

        RegisterClip(clip, volume, loop);
        return IsAudioRegistered(clipName);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = AudioVolumeSettings.Normalize(volume);
        PlayerPrefs.SetFloat(MusicVolumePlayerPrefsKey, musicVolume);
        RefreshRegisteredVolumes();
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = AudioVolumeSettings.Normalize(volume);
        PlayerPrefs.SetFloat(SfxVolumePlayerPrefsKey, sfxVolume);
        RefreshRegisteredVolumes();
    }

    public void RestoreDefaultVolumes()
    {
        SetMusicVolume(AudioVolumeSettings.DefaultMusicVolume);
        SetSfxVolume(AudioVolumeSettings.DefaultSfxVolume);
        SaveVolumeSettings();
    }

    public void SaveVolumeSettings()
    {
        PlayerPrefs.Save();
    }

    private void RefreshRegisteredVolumes()
    {
        if (audiosDic == null)
            return;

        foreach (KeyValuePair<string, AudioSource> pair in audiosDic)
            ApplySourceVolume(pair.Key, pair.Value);
    }

    private void ApplySourceVolume(string clipName, AudioSource source)
    {
        if (!source)
            return;

        float baseVolume = baseVolumes.TryGetValue(clipName, out float value) ? value : 1f;
        source.volume = baseVolume * (source.loop ? musicVolume : sfxVolume);
    }

    private void OnApplicationQuit()
    {
        SaveVolumeSettings();
    }

    /// <summary>
    /// 播放某一个音频
    /// </summary>
    /// <param name="name"></param>
    /// <param name="isWait"></param>
    public void PlayAudio(string name, bool isWait = false)
    {
        if (!TryPlayAudio(name, isWait))
        {
            Debug.LogWarning($"名为{name}不存在");
            return;
        }
    }

    /// <summary>
    /// 停止某一音频的播放
    /// </summary>
    /// <param name="name"></param>
    public void StopAudio(string name)
    {
        if (!audiosDic.ContainsKey(name))
        {
            Debug.LogWarning($"名为{name}不存在");
            return;
        }

        audiosDic[name].Stop();
    }
}
