using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools
{
    public static class PlotCharacterAssetGenerator
    {
        private const string PlotConfigDirectory = "Assets/StreamingAssets/Configs/Plot";
        private const string CharactersFolder = "Assets/Resources/GameData/Characters";

        [MenuItem("Twelve Moons/GameData/Generate Plot Character Assets")]
        public static void GenerateFromMenu()
        {
            GeneratePlotCharacterAssets(true);
        }

        public static int GeneratePlotCharacterAssets(bool saveAssets)
        {
            EnsureFolder("Assets/Resources", "GameData");
            EnsureFolder("Assets/Resources/GameData", "Characters");

            var providerRoot = Path.GetFullPath(PlotConfigDirectory);
            var csvProvider = new CsvConfigProvider(providerRoot);
            var dialogueTable = csvProvider.LoadTable("DialogueConfig");
            var groups = dialogueTable.Rows
                .Select(row => row.GetString("SpeakerCharacterId"))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .GroupBy(CharacterDisplayNameUtility.GetDisplayName, StringComparer.Ordinal)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key))
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToList();

            foreach (var group in groups)
            {
                var expressions = group
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToArray();
                var defaultPortraitId = ChooseDefaultPortrait(group.Key, expressions);
                var assetPath = $"{CharactersFolder}/plot_character_{SanitizeFileName(group.Key)}.asset";
                var asset = LoadOrCreateAsset<CharacterDefinitionAsset>(assetPath);
                asset.ApplyPlotCharacter(
                    $"plot_character_{group.Key}",
                    group.Key,
                    defaultPortraitId,
                    expressions,
                    $"? Plot DialogueConfig ?????????????????????????????{expressions.Length}?");
                EditorUtility.SetDirty(asset);
            }

            if (saveAssets)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return groups.Count;
        }

        private static string ChooseDefaultPortrait(string displayName, IReadOnlyList<string> expressions)
        {
            var normalName = displayName + "\u6b63\u5e38";
            var normal = expressions.FirstOrDefault(id => id == normalName);
            if (!string.IsNullOrEmpty(normal))
            {
                return normal;
            }

            return expressions.Count > 0 ? expressions[0] : displayName;
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
            var folderPath = $"{parentFolder}/{childFolderName}";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parentFolder, childFolderName);
            }
        }

        private static string SanitizeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var chars = value.Select(character => invalidChars.Contains(character) ? '_' : character).ToArray();
            return new string(chars);
        }
    }
}
