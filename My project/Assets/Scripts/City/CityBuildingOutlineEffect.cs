using UnityEngine;

namespace TwelveMoons.City
{
    public sealed class CityBuildingOutlineEffect : MonoBehaviour
    {
        [Header("轮廓高亮：鼠标悬停时显示建筑外轮廓")]
        [Tooltip("轮廓颜色；用于显示类似 Unity Scene 视图选中物体的橙色外圈描边。")]
        [SerializeField] private Color outlineColor = new Color(1f, 0.62f, 0.12f, 1f);
        [Tooltip("轮廓像素宽度；数值越大，屏幕空间外轮廓越粗。")]
        [SerializeField] private int outlinePixelWidth = 3;

        [Header("运行时只读快照：当前悬停状态")]
        [Tooltip("当前建筑是否正在向全局屏幕空间轮廓系统注册。")]
        [SerializeField] private bool inspectorIsHovered;
        [Tooltip("当前参与整体外轮廓高亮的 Renderer 数量。")]
        [SerializeField] private int inspectorRendererCount;

        private Renderer[] registeredRenderers = System.Array.Empty<Renderer>();

        public void Configure(Renderer[] renderers, Color color, float width)
        {
            outlineColor = color;
            outlinePixelWidth = Mathf.Max(1, Mathf.RoundToInt(width));
            registeredRenderers = renderers ?? System.Array.Empty<Renderer>();
            inspectorRendererCount = registeredRenderers.Length;

            if (inspectorIsHovered)
            {
                CityBuildingOutlineRuntime.Register(this, registeredRenderers, outlineColor, outlinePixelWidth);
            }
        }

        public void SetVisible(bool visible)
        {
            inspectorIsHovered = visible;
            if (visible)
            {
                CityBuildingOutlineRuntime.Register(this, registeredRenderers, outlineColor, outlinePixelWidth);
                return;
            }

            CityBuildingOutlineRuntime.Unregister(this);
        }

        private void OnDisable()
        {
            inspectorIsHovered = false;
            CityBuildingOutlineRuntime.Unregister(this);
        }
    }
}
