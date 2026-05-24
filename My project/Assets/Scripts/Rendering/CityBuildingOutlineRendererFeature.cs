using System.Collections.Generic;
using TwelveMoons.City;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

#pragma warning disable CS0618, CS0672
namespace TwelveMoons.Rendering
{
    public sealed class CityBuildingOutlineRendererFeature : ScriptableRendererFeature
    {
        private const string MaskShaderName = "TwelveMoons/CityHoverOutlineMask";
        private const string OutlineShaderName = "TwelveMoons/CityHoverOutline";

        [System.Serializable]
        public sealed class Settings
        {
            [Header("城区建筑轮廓：屏幕空间外描边")]
            [Tooltip("渲染时机；默认在透明物体之后叠加橙色外轮廓。")]
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

            [Tooltip("没有建筑悬停时是否跳过渲染；建议保持开启，避免不必要的全屏 Pass。")]
            public bool skipWhenNoHoveredBuilding = true;
        }

        [SerializeField] private Settings settings = new Settings();

        private CityBuildingOutlinePass outlinePass;
        private Material maskMaterial;
        private Material outlineMaterial;

        public override void Create()
        {
            maskMaterial = CreateMaterial(MaskShaderName, "RuntimeCityHoverOutlineMask");
            outlineMaterial = CreateMaterial(OutlineShaderName, "RuntimeCityHoverOutline");

            outlinePass = new CityBuildingOutlinePass(maskMaterial, outlineMaterial)
            {
                renderPassEvent = settings.renderPassEvent
            };
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (outlinePass == null)
            {
                return;
            }

            outlinePass.Setup(renderer.cameraColorTargetHandle);
            outlinePass.renderPassEvent = settings.renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.skipWhenNoHoveredBuilding && !CityBuildingOutlineRuntime.HasActiveOutline)
            {
                return;
            }

            if (outlinePass == null || maskMaterial == null || outlineMaterial == null)
            {
                return;
            }

            renderer.EnqueuePass(outlinePass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(maskMaterial);
            CoreUtils.Destroy(outlineMaterial);
        }

        private static Material CreateMaterial(string shaderName, string materialName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"找不到 Shader：{shaderName}。城区建筑轮廓高亮将不会显示。");
                return null;
            }

            return CoreUtils.CreateEngineMaterial(shader)
                .WithName(materialName);
        }

        private sealed class CityBuildingOutlinePass : ScriptableRenderPass
        {
            private static readonly int MaskTextureId = Shader.PropertyToID("_CityBuildingOutlineMaskTex");
            private static readonly int TempColorId = Shader.PropertyToID("_CityBuildingOutlineTempColor");
            private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
            private static readonly int OutlinePixelWidthId = Shader.PropertyToID("_OutlinePixelWidth");
            private static readonly Vector4 FullscreenScaleBias = new Vector4(1f, 1f, 0f, 0f);

            private readonly List<Renderer> renderers = new List<Renderer>();
            private readonly Material maskMaterial;
            private readonly Material outlineMaterial;
            private RTHandle colorTarget;

            public CityBuildingOutlinePass(Material maskMaterial, Material outlineMaterial)
            {
                this.maskMaterial = maskMaterial;
                this.outlineMaterial = outlineMaterial;
                requiresIntermediateTexture = true;
            }

            public void Setup(RTHandle cameraColorTarget)
            {
                colorTarget = cameraColorTarget;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (maskMaterial == null || outlineMaterial == null)
                {
                    return;
                }

                CityBuildingOutlineRuntime.CollectActiveRenderers(renderers);
                if (renderers.Count == 0)
                {
                    return;
                }

                var resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                {
                    return;
                }

                var hoveredRenderers = renderers.ToArray();
                var outlineColor = CityBuildingOutlineRuntime.CurrentColor;
                var outlinePixelWidth = CityBuildingOutlineRuntime.CurrentPixelWidth;

                var maskDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
                maskDesc.name = "_CityBuildingOutlineMaskTex";
                maskDesc.clearBuffer = true;
                maskDesc.clearColor = Color.clear;
                maskDesc.depthBufferBits = DepthBits.None;
                maskDesc.msaaSamples = MSAASamples.None;
                var maskTexture = renderGraph.CreateTexture(maskDesc);

                using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>("城区建筑轮廓蒙版", out var passData))
                {
                    passData.renderers = hoveredRenderers;
                    passData.maskMaterial = maskMaterial;

                    builder.SetRenderAttachment(maskTexture, 0, AccessFlags.Write);
                    if (resourceData.activeDepthTexture.IsValid())
                    {
                        builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                    }

                    builder.SetRenderFunc((MaskPassData data, RasterGraphContext context) => ExecuteMaskPass(data, context));
                }

                using (var builder = renderGraph.AddRasterRenderPass<GlobalTexturePassData>("设置城区建筑轮廓蒙版", out var passData))
                {
                    passData.texture = maskTexture;
                    builder.UseTexture(passData.texture, AccessFlags.Read);
                    builder.AllowGlobalStateModification(true);
                    builder.SetGlobalTextureAfterPass(passData.texture, MaskTextureId);
                    builder.SetRenderFunc((GlobalTexturePassData data, RasterGraphContext context) =>
                    {
                    });
                }

                var tempDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
                tempDesc.name = "_CityBuildingOutlineTempColor";
                tempDesc.clearBuffer = false;
                tempDesc.depthBufferBits = DepthBits.None;
                tempDesc.msaaSamples = MSAASamples.None;
                var tempColor = renderGraph.CreateTexture(tempDesc);

                using (var builder = renderGraph.AddRasterRenderPass<OutlinePassData>("城区建筑轮廓叠加", out var passData))
                {
                    passData.source = resourceData.activeColorTexture;
                    passData.maskTexture = maskTexture;
                    passData.outlineMaterial = outlineMaterial;
                    passData.outlineColor = outlineColor;
                    passData.outlinePixelWidth = outlinePixelWidth;

                    builder.UseTexture(passData.source, AccessFlags.Read);
                    builder.UseTexture(passData.maskTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(tempColor, 0, AccessFlags.Write);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc((OutlinePassData data, RasterGraphContext context) => ExecuteOutlinePass(data, context));
                }

                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("城区建筑轮廓回写颜色", out var passData))
                {
                    passData.source = tempColor;

                    builder.UseTexture(passData.source, AccessFlags.Read);
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, data.source, FullscreenScaleBias, 0, false);
                    });
                }
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (colorTarget == null || maskMaterial == null || outlineMaterial == null)
                {
                    return;
                }

                CityBuildingOutlineRuntime.CollectActiveRenderers(renderers);
                if (renderers.Count == 0)
                {
                    return;
                }

                var cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                cameraDescriptor.depthBufferBits = 0;
                cameraDescriptor.msaaSamples = 1;
                cameraDescriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;

                var cmd = CommandBufferPool.Get("城区建筑屏幕空间轮廓");
                cmd.GetTemporaryRT(MaskTextureId, cameraDescriptor, FilterMode.Point);
                cmd.GetTemporaryRT(TempColorId, cameraDescriptor, FilterMode.Bilinear);

                cmd.SetRenderTarget(MaskTextureId);
                cmd.ClearRenderTarget(false, true, Color.clear);
                DrawMaskRenderers(cmd);

                outlineMaterial.SetColor(OutlineColorId, CityBuildingOutlineRuntime.CurrentColor);
                outlineMaterial.SetFloat(OutlinePixelWidthId, CityBuildingOutlineRuntime.CurrentPixelWidth);
                cmd.SetGlobalTexture(MaskTextureId, new RenderTargetIdentifier(MaskTextureId));

                cmd.SetRenderTarget(TempColorId);
                Blitter.BlitTexture(cmd, colorTarget, FullscreenScaleBias, outlineMaterial, 0);
                cmd.Blit(TempColorId, colorTarget);

                cmd.ReleaseTemporaryRT(MaskTextureId);
                cmd.ReleaseTemporaryRT(TempColorId);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            private static void ExecuteMaskPass(MaskPassData data, RasterGraphContext context)
            {
                foreach (var renderer in data.renderers)
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    var materialCount = Mathf.Max(1, renderer.sharedMaterials.Length);
                    for (var submeshIndex = 0; submeshIndex < materialCount; submeshIndex++)
                    {
                        context.cmd.DrawRenderer(renderer, data.maskMaterial, submeshIndex);
                    }
                }
            }

            private static void ExecuteOutlinePass(OutlinePassData data, RasterGraphContext context)
            {
                data.outlineMaterial.SetColor(OutlineColorId, data.outlineColor);
                data.outlineMaterial.SetFloat(OutlinePixelWidthId, data.outlinePixelWidth);
                Blitter.BlitTexture(context.cmd, data.source, FullscreenScaleBias, data.outlineMaterial, 0);
            }

            private void DrawMaskRenderers(CommandBuffer cmd)
            {
                foreach (var renderer in renderers)
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    var materialCount = Mathf.Max(1, renderer.sharedMaterials.Length);
                    for (var submeshIndex = 0; submeshIndex < materialCount; submeshIndex++)
                    {
                        cmd.DrawRenderer(renderer, maskMaterial, submeshIndex);
                    }
                }
            }

            private sealed class MaskPassData
            {
                public Renderer[] renderers;
                public Material maskMaterial;
            }

            private sealed class OutlinePassData
            {
                public TextureHandle source;
                public TextureHandle maskTexture;
                public Material outlineMaterial;
                public Color outlineColor;
                public float outlinePixelWidth;
            }

            private sealed class GlobalTexturePassData
            {
                public TextureHandle texture;
            }

            private sealed class CopyPassData
            {
                public TextureHandle source;
            }
        }
    }

    internal static class MaterialNameExtensions
    {
        public static Material WithName(this Material material, string name)
        {
            if (material != null)
            {
                material.name = name;
            }

            return material;
        }
    }
}
#pragma warning restore CS0618, CS0672
