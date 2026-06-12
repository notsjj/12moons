using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.UI
{
    [DisallowMultipleComponent]
    public sealed class DeskPanelAtmosphereView : MonoBehaviour
    {
        private const string ShaderName = "TwelveMoons/UI/DeskPanelAtmosphere";

        [Header("桌面暗角")]
        [Tooltip("是否显示屏幕内侧暗角。暗角只影响视觉，不会阻挡按钮点击。")]
        [SerializeField] private bool enableVignette = true;

        [Tooltip("暗角颜色与最大透明度。")]
        [SerializeField] private Color vignetteColor = new Color(0f, 0f, 0f, 0.86f);

        [Tooltip("中心清晰区域半径；值越大，清晰区域越大。")]
        [SerializeField, Range(0f, 1f)] private float vignetteInnerRadius = 0.34f;

        [Tooltip("暗角由透明到黑色的渐变宽度。")]
        [SerializeField, Range(0.01f, 1f)] private float vignetteSoftness = 0.5f;

        [Header("整体画面压暗")]
        [Tooltip("全屏始终保留的黑色透明度，用于降低桌面整体亮度；受光区域会从这层暗幕中挖出清晰区域。")]
        [SerializeField, Range(0f, 0.8f)] private float overallDimAlpha = 0.28f;

        [Header("受光清晰区域")]
        [Tooltip("会被蜡烛照亮并保持底图清晰的 RectTransform。可拖入蜡烛、桌面纸张、屏幕或其它需要变清楚的局部。")]
        [SerializeField] private RectTransform[] lightTargets = System.Array.Empty<RectTransform>();

        [Tooltip("受光区域从暗幕中恢复清晰的强度。值越大，指定区域越接近原图亮度。")]
        [SerializeField, Range(0f, 1f)] private float lightClearAmount = 0.68f;

        [Tooltip("受光区域边缘的柔和过渡宽度，避免出现硬边。")]
        [SerializeField, Range(0.001f, 0.8f)] private float lightEdgeSoftness = 0.46f;

        [Tooltip("受光区域相对自身 RectTransform 额外向外扩展的比例。值越大，照亮范围越宽。")]
        [SerializeField, Range(0f, 2f)] private float lightTargetPadding = 1.15f;

        [Header("运行时引用与调试快照")]
        [Tooltip("全屏暗角与整体压暗图片，必须在 DeskPanel Prefab 中预先创建并拖入；该图片不会阻挡 UI 点击。")]
        [SerializeField] private Image vignetteImage;

        [Tooltip("现有蜡烛图片的 RectTransform，用于确认蜡烛本体也在受光清晰区域中。")]
        [SerializeField] private RectTransform candleRect;

        [Tooltip("运行时计算出的受光区域数量，只读快照，用来在 Inspector 中确认绑定是否生效。")]
        [SerializeField] private int activeLightTargetCountSnapshot;

        [Tooltip("运行时传给 Shader 的受光区域坐标快照，格式为中心 X、中心 Y、宽度、高度，均为 DeskPanel 内归一化坐标。")]
        [SerializeField] private Vector4[] lightTargetRectSnapshot = System.Array.Empty<Vector4>();

        private const int MaxLightTargets = 8;
        private Material vignetteMaterial;
        private readonly Vector4[] lightTargetRects = new Vector4[MaxLightTargets];

        public Image VignetteImage => vignetteImage;
        public RectTransform CandleRect => candleRect;
        public float OverallDimAlpha => overallDimAlpha;
        public int LightTargetCount => activeLightTargetCountSnapshot;
        public float LightEdgeSoftness => lightEdgeSoftness;
        public bool UsesRectangularVignette => true;

        private void Awake()
        {
            EnsureSetup();
        }

        private void OnEnable()
        {
            EnsureSetup();
            ApplyVisualSettings();
        }

        private void OnDestroy()
        {
            DestroyMaterial(vignetteMaterial);
        }

        private void OnValidate()
        {
            ApplyVisualSettings();
        }

        public void EnsureSetup()
        {
            if (vignetteImage == null || candleRect == null)
            {
                Debug.LogError("DeskPanelAtmosphereView 缺少 Prefab 引用：请在 DeskPanel Prefab 中绑定桌面暗角、蜡烛和受光清晰区域。", this);
            }
        }

        public void ApplyVisualSettings()
        {
            var shader = Resources.Load<Shader>("Shaders/DeskPanelAtmosphere");
            if (shader == null)
            {
                shader = Shader.Find(ShaderName);
            }
            if (shader == null)
            {
                return;
            }

            if (vignetteImage != null)
            {
                vignetteMaterial = EnsureMaterial(vignetteMaterial, shader, "DeskPanel 暗角运行时材质");
                ConfigureMaterial(vignetteMaterial, vignetteColor, vignetteInnerRadius, vignetteSoftness, overallDimAlpha);
                ConfigureLightTargets(vignetteMaterial);
                vignetteImage.material = vignetteMaterial;
                vignetteImage.enabled = enableVignette;
                vignetteImage.raycastTarget = false;
            }
        }

        public bool ContainsLightTarget(RectTransform target)
        {
            if (target == null || lightTargets == null)
            {
                return false;
            }

            for (var index = 0; index < lightTargets.Length; index++)
            {
                if (lightTargets[index] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private void ConfigureLightTargets(Material material)
        {
            var rootRect = transform as RectTransform;
            var root = rootRect == null ? Rect.zero : rootRect.rect;
            var count = 0;

            if (root.width > 0f && root.height > 0f && lightTargets != null)
            {
                for (var index = 0; index < lightTargets.Length && count < MaxLightTargets; index++)
                {
                    var target = lightTargets[index];
                    if (target == null)
                    {
                        continue;
                    }

                    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rootRect, target);
                    var size = bounds.size;
                    var paddedWidth = size.x * (1f + lightTargetPadding);
                    var paddedHeight = size.y * (1f + lightTargetPadding);
                    lightTargetRects[count] = new Vector4(
                        (bounds.center.x - root.xMin) / root.width,
                        (bounds.center.y - root.yMin) / root.height,
                        Mathf.Clamp01(paddedWidth / root.width),
                        Mathf.Clamp01(paddedHeight / root.height));
                    count++;
                }
            }

            for (var index = count; index < MaxLightTargets; index++)
            {
                lightTargetRects[index] = Vector4.zero;
            }

            activeLightTargetCountSnapshot = count;
            lightTargetRectSnapshot = new Vector4[count];
            for (var index = 0; index < count; index++)
            {
                lightTargetRectSnapshot[index] = lightTargetRects[index];
            }

            material.SetInt("_LightRectCount", count);
            material.SetVectorArray("_LightRects", lightTargetRects);
            material.SetFloat("_LightClearAmount", lightClearAmount);
            material.SetFloat("_LightEdgeSoftness", lightEdgeSoftness);
        }

        private static Material EnsureMaterial(Material material, Shader shader, string materialName)
        {
            if (material != null && material.shader == shader)
            {
                return material;
            }

            DestroyMaterial(material);
            return new Material(shader)
            {
                name = materialName,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static void ConfigureMaterial(
            Material material,
            Color effectColor,
            float innerRadius,
            float softness,
            float baseDimAlpha)
        {
            material.SetColor("_EffectColor", effectColor);
            material.SetFloat("_InnerRadius", innerRadius);
            material.SetFloat("_Softness", softness);
            material.SetFloat("_BaseDimAlpha", baseDimAlpha);
        }

        private static void DestroyMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }
        }
    }
}
