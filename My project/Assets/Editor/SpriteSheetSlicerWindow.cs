using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools
{
    public sealed class SpriteSheetSlicerWindow : EditorWindow
    {
        private Texture2D texture;
        private int pixelsPerUnit = 100;
        private Vector2 pivot = new Vector2(0.5f, 0.5f);
        private float alphaThreshold = 0.05f;
        private int padding = 0;
        private int minimumSpritePixels = 4;

        [MenuItem("Twelve Moons/Tools/Auto Slice Selected PNG To Sprites")]
        public static void OpenWithSelection()
        {
            var window = GetWindow<SpriteSheetSlicerWindow>("Sprite Slicer");
            window.texture = Selection.activeObject as Texture2D;
            window.Show();
        }

        [MenuItem("Twelve Moons/Tools/Auto Slice Selected PNG To Sprites", true)]
        public static bool CanOpenWithSelection()
        {
            return Selection.activeObject is Texture2D;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Sprite Sheet", EditorStyles.boldLabel);
            texture = (Texture2D)EditorGUILayout.ObjectField("Texture", texture, typeof(Texture2D), false);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Auto Slice", EditorStyles.boldLabel);
            pixelsPerUnit = EditorGUILayout.IntField("Pixels Per Unit", Mathf.Max(1, pixelsPerUnit));
            pivot = EditorGUILayout.Vector2Field("Pivot", pivot);
            alphaThreshold = EditorGUILayout.Slider("Alpha Threshold", alphaThreshold, 0.001f, 1f);
            padding = EditorGUILayout.IntField("Padding", Mathf.Max(0, padding));
            minimumSpritePixels = EditorGUILayout.IntField("Minimum Sprite Pixels", Mathf.Max(1, minimumSpritePixels));

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(texture == null))
            {
                if (GUILayout.Button("Auto Slice And Set Compression None", GUILayout.Height(32f)))
                {
                    SliceSelectedTexture();
                }
            }

            if (texture != null)
            {
                EditorGUILayout.HelpBox(
                    $"Texture size: {texture.width} x {texture.height}. The tool finds separated non-transparent regions and creates one sprite per region.",
                    MessageType.Info);
            }
        }

        private void SliceSelectedTexture()
        {
            if (texture == null)
            {
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog("Sprite Slicer", "Please select a texture asset from the Project window.", "OK");
                return;
            }

            if (!string.Equals(Path.GetExtension(assetPath), ".png", System.StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Sprite Slicer", "Please select a PNG texture.", "OK");
                return;
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                EditorUtility.DisplayDialog("Sprite Slicer", "Selected asset is not imported by TextureImporter.", "OK");
                return;
            }

            var wasReadable = importer.isReadable;
            ApplyImporterSettings(importer, true);
            importer.SaveAndReimport();

            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            var sprites = BuildSpritesFromContent(texture, assetPath);
            if (sprites.Count == 0)
            {
                EditorUtility.DisplayDialog("Sprite Slicer", "No visible PNG regions were found. Lower Alpha Threshold or check the image alpha.", "OK");
                importer.isReadable = wasReadable;
                importer.SaveAndReimport();
                return;
            }

            importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            ApplyImporterSettings(importer, wasReadable);
            importer.spritesheet = sprites.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            Debug.Log($"Auto sliced {assetPath} into {sprites.Count} sprites with compression set to None.", texture);
        }

        private void ApplyImporterSettings(TextureImporter importer, bool isReadable)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = Mathf.Max(1, pixelsPerUnit);
            importer.maxTextureSize = 4096;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = isReadable;

            var defaultSettings = importer.GetDefaultPlatformTextureSettings();
            defaultSettings.maxTextureSize = 4096;
            importer.SetPlatformTextureSettings(defaultSettings);
        }

        private List<SpriteMetaData> BuildSpritesFromContent(Texture2D source, string assetPath)
        {
            var width = source.width;
            var height = source.height;
            var pixels = source.GetPixels32();
            var visited = new bool[pixels.Length];
            var sprites = new List<SpriteMetaData>();
            var baseName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            for (var y = height - 1; y >= 0; y--)
            {
                for (var x = 0; x < width; x++)
                {
                    var startIndex = ToIndex(x, y, width);
                    if (visited[startIndex] || !IsVisible(pixels[startIndex]))
                    {
                        continue;
                    }

                    var bounds = FloodFillBounds(x, y, width, height, pixels, visited);
                    if (bounds.width * bounds.height < minimumSpritePixels)
                    {
                        continue;
                    }

                    sprites.Add(CreateSpriteMetaData(baseName, sprites.Count, ExpandBounds(bounds, width, height)));
                }
            }

            sprites.Sort(CompareSpritesTopLeft);
            if (sprites.Count == 1)
            {
                var sprite = sprites[0];
                sprite.rect = new Rect(0f, 0f, width, height);
                sprites[0] = sprite;
            }

            RenameSprites(baseName, sprites);
            return sprites;
        }

        private RectInt FloodFillBounds(int startX, int startY, int width, int height, Color32[] pixels, bool[] visited)
        {
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startX, startY));
            visited[ToIndex(startX, startY, width)] = true;

            var minX = startX;
            var maxX = startX;
            var minY = startY;
            var maxY = startY;

            while (queue.Count > 0)
            {
                var point = queue.Dequeue();
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);

                TryEnqueue(point.x - 1, point.y, width, height, pixels, visited, queue);
                TryEnqueue(point.x + 1, point.y, width, height, pixels, visited, queue);
                TryEnqueue(point.x, point.y - 1, width, height, pixels, visited, queue);
                TryEnqueue(point.x, point.y + 1, width, height, pixels, visited, queue);
            }

            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private void TryEnqueue(
            int x,
            int y,
            int width,
            int height,
            Color32[] pixels,
            bool[] visited,
            Queue<Vector2Int> queue)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return;
            }

            var index = ToIndex(x, y, width);
            if (visited[index] || !IsVisible(pixels[index]))
            {
                return;
            }

            visited[index] = true;
            queue.Enqueue(new Vector2Int(x, y));
        }

        private RectInt ExpandBounds(RectInt bounds, int width, int height)
        {
            var left = Mathf.Max(0, bounds.xMin - padding);
            var bottom = Mathf.Max(0, bounds.yMin - padding);
            var right = Mathf.Min(width, bounds.xMax + padding);
            var top = Mathf.Min(height, bounds.yMax + padding);
            return new RectInt(left, bottom, right - left, top - bottom);
        }

        private SpriteMetaData CreateSpriteMetaData(string baseName, int index, RectInt bounds)
        {
            return new SpriteMetaData
            {
                name = $"{baseName}_{index + 1:00}",
                rect = new Rect(bounds.x, bounds.y, bounds.width, bounds.height),
                alignment = (int)SpriteAlignment.Custom,
                pivot = pivot
            };
        }

        private static int CompareSpritesTopLeft(SpriteMetaData left, SpriteMetaData right)
        {
            var yComparison = right.rect.yMax.CompareTo(left.rect.yMax);
            return yComparison != 0 ? yComparison : left.rect.xMin.CompareTo(right.rect.xMin);
        }

        private static void RenameSprites(string baseName, List<SpriteMetaData> sprites)
        {
            for (var index = 0; index < sprites.Count; index++)
            {
                var sprite = sprites[index];
                sprite.name = $"{baseName}_{index + 1:00}";
                sprites[index] = sprite;
            }
        }

        private bool IsVisible(Color32 pixel)
        {
            return pixel.a >= alphaThreshold * 255f;
        }

        private static int ToIndex(int x, int y, int width)
        {
            return y * width + x;
        }
    }
}
