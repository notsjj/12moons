using System;
using UnityEngine;

namespace TwelveMoons.City
{
    [Serializable]
    public sealed class CityCameraViewPoint
    {
        [Header("点位标识：按钮和调试信息使用")]
        [Tooltip("摄像机观察点 ID；只用于阶段13的摄像机移动，不触发城区数据刷新。")]
        [SerializeField] private string viewId;

        [Tooltip("摄像机观察点中文名；用于 Inspector 调试和按钮显示。")]
        [SerializeField] private string displayName;

        [Header("点位 Transform：摄像机会移动到这里")]
        [Tooltip("空物体点位；摄像机移动到该 Transform 的位置，并可选同步旋转。")]
        [SerializeField] private Transform target;

        public string ViewId => viewId;

        public string DisplayName => displayName;

        public Transform Target => target;

        public CityCameraViewPoint(string viewId, string displayName, Transform target)
        {
            this.viewId = viewId;
            this.displayName = displayName;
            this.target = target;
        }
    }
}
