using System;
using UnityEngine;

public class Settings
{
    [Header("物体遮挡透明设置")]
    public const float itemFadeDuration = 0.35f;
    public const float targetAlpha = 0.45f;

    [Header("时间相关")]
    public const float secondThreshold = 0.012f; //数值越小时间越快
    public const int secondHold = 59;
    public const int minuteHold = 59;
    public const int hourHold = 23;
    public const int dayHold = 10;
    public const int seasonHold= 3;

    [Header("UI相关")]
    public const float fadeDuration = 1f;

    [Header("灯光")]
    public const float lightChangeDuration = 25f;
    public static TimeSpan morningTime = new TimeSpan(5, 0, 0);
    public static TimeSpan nightTime = new TimeSpan(19, 0, 0);

    [Header("初始坐标")]
    public static Vector3 playerStartPos = new Vector3(-2f, -23f, 0);

    [Header("玩家初始资金")]
    public const int playerStartMoney = 100;
}
