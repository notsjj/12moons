using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class EventHandler
{
    #region 对话事件
    /// <summary>
    /// 获取字幕数据
    /// </summary>
    public static event Action<string, string> GetDialogueDataEvent;
    public static void CallGetDialogueDataEvent(string sceneName, string dialogueID)
    {
        GetDialogueDataEvent?.Invoke(sceneName, dialogueID);
    }

    /// <summary>
    /// 播放字幕UI
    /// </summary>
    public static event Action<float, float, string[]> DisplayDialogueUIEvent;
    public static void CallDisplayDialogueUIEvent(float textSpeed, float sentenceSpacing, string[] dialogueText)
    {
        DisplayDialogueUIEvent?.Invoke(textSpeed, sentenceSpacing, dialogueText);
    }
    #endregion

    #region 异步场景加载事件
    public static event Action<string, Vector3> TransitionEvent;
    public static void CallTransitionEvent(string sceneName, Vector3 pos)
    {
        TransitionEvent?.Invoke(sceneName, pos);
    }

    public static event Action BeforeSceneUnloadEvent;
    public static void CallBeforeSceneUnloadEvent()
    {
        BeforeSceneUnloadEvent?.Invoke();
    }

    public static event Action AfterSceneLoadedEvent;
    public static void CallAfterSceneLoadedEvent()
    {
        AfterSceneLoadedEvent?.Invoke();
    }

    public static event Action<Vector3> MoveToPosition;
    public static void CallMoveToPosition(Vector3 targetPosition)
    {
        MoveToPosition?.Invoke(targetPosition);
    }

    #endregion

    public static event Action<ParticleEffectType, Vector3> ParticleEffectEvent;
    public static void CallParticleEffectEvent(ParticleEffectType effectType, Vector3 pos)
    {
        ParticleEffectEvent?.Invoke(effectType, pos);
    }

    public static event Action<GameState> UpdateGameStateEvent;
    public static void CallUpdateGameStateEvent(GameState gameState)
    {
        UpdateGameStateEvent?.Invoke(gameState);
    }

    public static event Action<int> StartNewGameEvent;
    public static void CallStartNewGameEvent(int index)
    {
        StartNewGameEvent?.Invoke(index);
    }

    public static event Action EndGameEvent;
    public static void CallEndGameEvent()
    {
        EndGameEvent?.Invoke();
    }

}
