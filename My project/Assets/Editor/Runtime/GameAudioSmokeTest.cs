using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class GameAudioSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run Game Audio Smoke Test")]
        public static void Run()
        {
            ValidateAudioResources();
            ValidateEnvironmentRainMapping();
            ValidateAudioTriggerPlacement();
            Debug.Log("游戏音频冒烟测试通过：Resources 音频资源可加载，灾难阶段雨声映射正确。");
        }

        private static void ValidateAudioResources()
        {
            var bgmClips = Resources.LoadAll<AudioClip>("Audio/BGM").Select(clip => clip.name).ToArray();
            var sfxClips = Resources.LoadAll<AudioClip>("Audio/SFX").Select(clip => clip.name).ToArray();

            RequireClip(bgmClips, "小雨", "Audio/BGM");
            RequireClip(bgmClips, "大雨", "Audio/BGM");
            RequireClip(sfxClips, "云转场散开", "Audio/SFX");
            RequireClip(sfxClips, "云转场聚拢", "Audio/SFX");
            RequireClip(sfxClips, "公文开启关闭", "Audio/SFX");
            RequireClip(sfxClips, "切换城区", "Audio/SFX");
            RequireClip(sfxClips, "处理公文反馈", "Audio/SFX");
            RequireClip(sfxClips, "开始界面光声", "Audio/SFX");
            RequireClip(sfxClips, "按键声", "Audio/SFX");
            RequireClip(sfxClips, "键盘打字机声", "Audio/SFX");
        }

        private static void ValidateEnvironmentRainMapping()
        {
            if (typeof(GameAudioBinder).GetMethod(nameof(GameAudioBinder.ResolveEnvironmentClipName)) == null)
            {
                throw new MissingMethodException(nameof(GameAudioBinder), nameof(GameAudioBinder.ResolveEnvironmentClipName));
            }

            if (GameAudioBinder.ResolveEnvironmentClipName("小雨") != "小雨")
            {
                throw new InvalidDataException("灾难阶段“小雨”必须播放环境音“小雨”。");
            }

            if (GameAudioBinder.ResolveEnvironmentClipName("大雨") != "大雨" ||
                GameAudioBinder.ResolveEnvironmentClipName("大暴雨") != "大雨")
            {
                throw new InvalidDataException("灾难阶段“大雨”和“大暴雨”必须播放环境音“大雨”。");
            }

            if (!string.IsNullOrEmpty(GameAudioBinder.ResolveEnvironmentClipName("晴天")) ||
                !string.IsNullOrEmpty(GameAudioBinder.ResolveEnvironmentClipName("阴")))
            {
                throw new InvalidDataException("非雨灾难阶段不应播放雨声环境音。");
            }
        }

        private static void ValidateAudioTriggerPlacement()
        {
            var startPanelSource = File.ReadAllText("Assets/Scripts/UI/StartPanelView.cs");
            var startPanelOnEnable = SliceMethod(startPanelSource, "private void OnEnable()");
            var startGameClicked = SliceMethod(startPanelSource, "public void OnStartGameClicked()");
            if (startPanelOnEnable.Contains("PlayStartPanelGlow") ||
                !startGameClicked.Contains("GameAudioBinder.PlayStartPanelGlow();"))
            {
                throw new InvalidDataException("“开始界面光声”只能在玩家点击“开始游戏”后触发一次，不能在开始面板显示时触发。");
            }

            var binderSource = File.ReadAllText("Assets/Scripts/Audio/GameAudioBinder.cs");
            if (!binderSource.Contains("typewriterLoopSource") ||
                !binderSource.Contains("EnsureTypewriterLoopSource") ||
                !binderSource.Contains("typewriterLoopSource.loop = true") ||
                !binderSource.Contains("typewriterLoopSource.Play()") ||
                !binderSource.Contains("typewriterLoopSource.Stop()") ||
                binderSource.Contains("return PlaySfx(TypewriterClipName, true);"))
            {
                throw new InvalidDataException("“键盘打字机声”必须使用循环 AudioSource，从打字机效果开始播放，最后一个字输出或跳过时停止。");
            }

            var loadingSource = File.ReadAllText("Assets/Scripts/UI/LoadingPanelTransitionView.cs");
            if (!loadingSource.Contains("GameAudioBinder.PlayCloudGather();\n            playingSequence.Append(BuildEnterSequence()") ||
                !loadingSource.Contains("playingSequence.AppendCallback(() => GameAudioBinder.PlayCloudScatter());\n            playingSequence.Append(BuildExitSequence()") ||
                !loadingSource.Contains("GameAudioBinder.PlayCloudGather();\n                onStarted?.Invoke();") ||
                !loadingSource.Contains("playingSequence.AppendCallback(() => GameAudioBinder.PlayCloudScatter());\n            playingSequence.Append(BuildExitSequence(phaseDuration))"))
            {
                throw new InvalidDataException("云转场聚拢/散开音效必须分别在加载过场面板聚拢动效和散开动效开始时触发。");
            }
        }

        private static string SliceMethod(string source, string signature)
        {
            var start = source.IndexOf(signature, StringComparison.Ordinal);
            if (start < 0)
            {
                throw new MissingMethodException(signature);
            }

            var nextMethod = source.IndexOf("\n        private ", start + signature.Length, StringComparison.Ordinal);
            if (nextMethod < 0)
            {
                nextMethod = source.IndexOf("\n        public ", start + signature.Length, StringComparison.Ordinal);
            }

            return nextMethod < 0 ? source.Substring(start) : source.Substring(start, nextMethod - start);
        }

        private static void RequireClip(string[] clipNames, string clipName, string resourcesPath)
        {
            if (!clipNames.Contains(clipName))
            {
                throw new FileNotFoundException($"缺少音频资源：Resources/{resourcesPath}/{clipName}");
            }
        }
    }
}
