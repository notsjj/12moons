using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TwelveMoons.City
{
    public sealed class CityBuildingOutlineEffect : MonoBehaviour
    {
        private const string OutlineShaderName = "TwelveMoons/CityHoverOutline";
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

        [Header("轮廓高亮：鼠标悬停时显示建筑边缘")]
        [Tooltip("轮廓材质；留空时运行时会使用 TwelveMoons/CityHoverOutline Shader 自动创建。")]
        [SerializeField] private Material outlineMaterial;
        [Tooltip("轮廓颜色；用于显示建筑朝向当前摄像机时外圈加粗的高亮边缘。")]
        [SerializeField] private Color outlineColor = new Color(1f, 0.78f, 0.18f, 1f);
        [Tooltip("轮廓宽度；数值越大，建筑外圈高亮越粗。")]
        [SerializeField] private float outlineWidth = 0.045f;

        [Header("运行时只读快照：轮廓副本状态")]
        [Tooltip("当前参与轮廓高亮的 Renderer 数量。")]
        [SerializeField] private int inspectorRendererCount;
        [Tooltip("当前已经创建的轮廓 Renderer 副本数量。")]
        [SerializeField] private int inspectorOutlineRendererCount;

        private readonly List<Renderer> sourceRenderers = new List<Renderer>();
        private readonly List<Renderer> outlineRenderers = new List<Renderer>();
        private Transform outlineRoot;
        private bool isVisible;

        public void Configure(Renderer[] renderers, Color color, float width)
        {
            outlineColor = color;
            outlineWidth = Mathf.Max(0f, width);
            SetTargets(renderers);
        }

        public void SetTargets(Renderer[] renderers)
        {
            sourceRenderers.Clear();
            if (renderers != null)
            {
                foreach (var sourceRenderer in renderers)
                {
                    if (sourceRenderer != null && !sourceRenderers.Contains(sourceRenderer))
                    {
                        sourceRenderers.Add(sourceRenderer);
                    }
                }
            }

            RebuildOutlines();
            SetVisible(isVisible);
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
            EnsureMaterial();

            foreach (var outlineRenderer in outlineRenderers)
            {
                if (outlineRenderer == null)
                {
                    continue;
                }

                var source = outlineRenderer.transform.parent != null
                    ? outlineRenderer.transform.parent.GetComponent<Renderer>()
                    : null;
                outlineRenderer.enabled = visible && source != null && source.enabled && source.gameObject.activeInHierarchy;
            }
        }

        private void RebuildOutlines()
        {
            EnsureOutlineRoot();
            ClearOutlineChildren();
            EnsureMaterial();

            foreach (var sourceRenderer in sourceRenderers)
            {
                CreateOutlineForRenderer(sourceRenderer);
            }

            inspectorRendererCount = sourceRenderers.Count;
            inspectorOutlineRendererCount = outlineRenderers.Count;
        }

        private void CreateOutlineForRenderer(Renderer sourceRenderer)
        {
            if (sourceRenderer == null)
            {
                return;
            }

            var meshFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return;
            }

            var outlineObject = new GameObject($"{sourceRenderer.name}_Outline");
            outlineObject.transform.SetParent(sourceRenderer.transform, false);
            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one;
            outlineObject.layer = sourceRenderer.gameObject.layer;

            var outlineMeshFilter = outlineObject.AddComponent<MeshFilter>();
            outlineMeshFilter.sharedMesh = meshFilter.sharedMesh;

            var outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
            outlineRenderer.sharedMaterials = CreateMaterialArray(meshFilter.sharedMesh.subMeshCount);
            outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
            outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            outlineRenderer.enabled = false;
            outlineRenderers.Add(outlineRenderer);
        }

        private Material[] CreateMaterialArray(int count)
        {
            var materialCount = Mathf.Max(1, count);
            var materials = new Material[materialCount];
            for (var index = 0; index < materials.Length; index++)
            {
                materials[index] = outlineMaterial;
            }

            return materials;
        }

        private void EnsureOutlineRoot()
        {
            if (outlineRoot != null)
            {
                return;
            }

            var rootObject = new GameObject("HoverOutlineRuntime");
            rootObject.transform.SetParent(transform, false);
            outlineRoot = rootObject.transform;
            outlineRoot.gameObject.SetActive(false);
        }

        private void ClearOutlineChildren()
        {
            foreach (var outlineRenderer in outlineRenderers)
            {
                if (outlineRenderer != null)
                {
                    Destroy(outlineRenderer.gameObject);
                }
            }

            outlineRenderers.Clear();
        }

        private void EnsureMaterial()
        {
            if (outlineMaterial == null)
            {
                var shader = Shader.Find(OutlineShaderName);
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                }

                outlineMaterial = new Material(shader)
                {
                    name = "RuntimeCityHoverOutline"
                };
            }

            outlineMaterial.SetColor(OutlineColorId, outlineColor);
            outlineMaterial.SetFloat(OutlineWidthId, Mathf.Max(0f, outlineWidth));
        }
    }
}
