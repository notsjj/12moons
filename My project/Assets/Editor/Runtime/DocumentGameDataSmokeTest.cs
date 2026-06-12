using System.IO;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class DocumentGameDataSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run Document GameData Smoke Test")]
        public static void Run()
        {
            var root = new GameObject("DocumentGameDataSmokeTest");
            try
            {
                var configManager = root.AddComponent<ConfigManager>();
                var relativeDirectoryField = typeof(ConfigManager).GetField(
                    "relativeConfigDirectory",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                relativeDirectoryField?.SetValue(configManager, "Configs/Demo");
                configManager.BuildDefaultProviders();

                GeneratedGameDataSynchronizer.GenerateDocumentAndCharacterAssets(configManager, true);

                var documentAsset = AssetDatabase.LoadAssetAtPath<DocumentDefinitionAsset>(
                    "Assets/Resources/GameData/Documents/document_relief_prepare.asset");
                var characterAsset = AssetDatabase.LoadAssetAtPath<CharacterDefinitionAsset>(
                    "Assets/Resources/GameData/Characters/character_steward.asset");
                var fallbackPortrait = CharacterPlaceholderPortraitProvider.LoadPortrait(string.Empty);

                if (documentAsset == null ||
                    characterAsset == null ||
                    documentAsset.ProposerCharacterId != "character_steward" ||
                    string.IsNullOrEmpty(characterAsset.CharacterName) ||
                    fallbackPortrait == null)
                {
                    throw new InvalidDataException("Document GameData smoke test failed.");
                }

                Debug.Log("Document GameData smoke test passed. SO assets were generated to Resources/GameData and fallback portrait is available.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
