using System;
using System.Linq;
using System.Text;
using TwelveMoons.Core.Config;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TwelveMoons.Core.Runtime
{
    public static class GeneratedGameDataSynchronizer
    {
#if UNITY_EDITOR
        private const string ResourcesRoot = "Assets/Resources/GameData";
        private const string DocumentsFolder = "Assets/Resources/GameData/Documents";
        private const string CharactersFolder = "Assets/Resources/GameData/Characters";
        private const string DemoConfigDirectory = "Configs/Demo";
        private static string lastGeneratedSignature = string.Empty;

        [MenuItem("Twelve Moons/GameData/Generate Document And Character Assets")]
        public static void GenerateFromMenu()
        {
            var tempRoot = new GameObject("GeneratedGameDataSynchronizer");
            try
            {
                var configManager = tempRoot.AddComponent<ConfigManager>();
                var relativeDirectoryField = typeof(ConfigManager).GetField(
                    "relativeConfigDirectory",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                relativeDirectoryField?.SetValue(configManager, DemoConfigDirectory);
                configManager.BuildDefaultProviders();
                GenerateDocumentAndCharacterAssets(configManager, true);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tempRoot);
            }
        }

        public static void GenerateDocumentAndCharacterAssets(ConfigManager configManager, bool force = false)
        {
            if (configManager == null)
            {
                return;
            }

            if (!configManager.TryGetTable("DocumentConfig", out var documentTable) ||
                !configManager.TryGetTable("CharacterConfig", out var characterTable))
            {
                return;
            }

            var signature = BuildSignature(documentTable, characterTable);
            if (!force &&
                signature == lastGeneratedSignature &&
                AssetDatabase.IsValidFolder(DocumentsFolder) &&
                AssetDatabase.IsValidFolder(CharactersFolder))
            {
                return;
            }

            EnsureFolder("Assets/Resources", "GameData");
            EnsureFolder(ResourcesRoot, "Documents");
            EnsureFolder(ResourcesRoot, "Characters");

            foreach (var row in characterTable.Rows)
            {
                var definition = new CharacterDefinition(row);
                if (string.IsNullOrWhiteSpace(definition.CharacterId))
                {
                    continue;
                }

                var asset = LoadOrCreateAsset<CharacterDefinitionAsset>($"{CharactersFolder}/{definition.CharacterId}.asset");
                asset.Apply(definition);
                EditorUtility.SetDirty(asset);
            }

            foreach (var row in documentTable.Rows)
            {
                var definition = new DocumentDefinition(row);
                if (string.IsNullOrWhiteSpace(definition.DocumentId))
                {
                    continue;
                }

                var asset = LoadOrCreateAsset<DocumentDefinitionAsset>($"{DocumentsFolder}/{definition.DocumentId}.asset");
                asset.Apply(definition);
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            lastGeneratedSignature = signature;
        }

        private static T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void EnsureFolder(string parentFolder, string childFolderName)
        {
            if (!AssetDatabase.IsValidFolder($"{parentFolder}/{childFolderName}"))
            {
                AssetDatabase.CreateFolder(parentFolder, childFolderName);
            }
        }

        private static string BuildSignature(ConfigTable documentTable, ConfigTable characterTable)
        {
            var builder = new StringBuilder();
            AppendTableSignature(builder, documentTable);
            AppendTableSignature(builder, characterTable);
            return builder.ToString();
        }

        private static void AppendTableSignature(StringBuilder builder, ConfigTable table)
        {
            builder.Append(table.TableName).Append(':').Append(table.Rows.Count).Append('|');
            foreach (var row in table.Rows)
            {
                foreach (var value in row.Values.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    builder.Append(value.Key).Append('=').Append(value.Value).Append(';');
                }

                builder.AppendLine();
            }
        }
#else
        public static void GenerateDocumentAndCharacterAssets(ConfigManager configManager, bool force = false)
        {
        }
#endif
    }
}
