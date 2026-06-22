using System.IO;
using System.Linq;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.Editor.Runtime
{
    public static class PlotConfigSmokeTest
    {
        private const string PlotConfigDirectory = "Assets/StreamingAssets/Configs/Plot";

        private static readonly string[] CharacterRoots =
        {
            string.Empty,
            "Art/Art/Character"
        };

        private static readonly string[] MapRoots =
        {
            string.Empty,
            "Art/Art/Map"
        };

        [MenuItem("Twelve Moons/Tests/Run Plot Config Smoke Test")]
        public static void Run()
        {
            var providerRoot = Path.Combine(Application.dataPath, "StreamingAssets", "Configs", "Plot");
            var csvProvider = new CsvConfigProvider(providerRoot);

            var storyTable = csvProvider.LoadTable("StoryConfig");
            var dialogueTable = csvProvider.LoadTable("DialogueConfig");
            var taskTable = csvProvider.LoadTable("TaskConfig");
            var taskStageTable = csvProvider.LoadTable("TaskStageConfig");
            var openingStoryName = "\u6fc0\u6d3b\u9ab7\u9ac5";

            if (!storyTable.TryFindById("StoryId", "S0001", out var openingStory) ||
                openingStory.GetString("StoryName") != openingStoryName ||
                openingStory.GetString("StoryType") != "Dialogue")
            {
                throw new InvalidDataException("Plot StoryConfig must contain S0001 as the opening dialogue story.");
            }

            var openingLines = dialogueTable.Rows
                .Where(row => row.GetString("StoryId") == "S0001")
                .Select(row => new DialogueLineDefinition(row))
                .ToList();
            if (openingLines.Count == 0 || openingLines[0].LineId != "S0001_001")
            {
                throw new InvalidDataException("Plot DialogueConfig must contain normalized dialogue lines for S0001.");
            }

            if (!File.Exists(Path.Combine(PlotConfigDirectory, openingStoryName + ".csv")))
            {
                throw new FileNotFoundException("Plot directory must keep an individual CSV named after the opening story.");
            }

            if (!storyTable.TryFindById("StoryId", "S0002", out var firstDocumentStory) ||
                firstDocumentStory.GetString("StoryName") != "[主线]最初的工作" ||
                firstDocumentStory.GetString("StoryType") != "Dialogue" ||
                firstDocumentStory.GetString("StoryContentAssetId") != "最初的工作")
            {
                throw new InvalidDataException("Plot StoryConfig must contain S0002 and map it to the 最初的工作 dialogue table.");
            }

            if (!File.Exists(Path.Combine(PlotConfigDirectory, "最初的工作.csv")))
            {
                throw new FileNotFoundException("Plot directory must keep an individual CSV named after S0002: 最初的工作.");
            }

            var firstRoundTask = taskTable.Rows
                .FirstOrDefault(row => row.GetInt("StartRound") == 1);
            if (firstRoundTask == null)
            {
                throw new InvalidDataException("Plot TaskConfig must activate at least one task on round 1.");
            }

            var firstRoundStages = taskStageTable.Rows
                .Where(row => row.GetString("TaskId") == firstRoundTask.GetString("TaskId"))
                .ToList();
            if (!firstRoundStages.Any(row => row.GetString("StartStoryId") == "S0001" ||
                                            row.GetString("BeforeDocumentStoryId") == "S0001"))
            {
                throw new InvalidDataException("The first round plot task must queue S0001 so the game opens with the opening story.");
            }

            ValidateDialoguePortraits(dialogueTable);
            ValidateOpeningSkeletonPresentationCues(dialogueTable);
            ValidateMapSprites();
            ValidateSpeakerDisplayNames();
            ValidatePlotCharacterAssets();

            Debug.Log("Plot config smoke test passed: Plot tables load, S0001 has dialogue lines, round 1 queues it, dialogue portraits/maps resolve, and Plot character assets exist.");
        }

        private static void ValidateOpeningSkeletonPresentationCues(ConfigTable dialogueTable)
        {
            var openingStartCue = dialogueTable.Rows
                .FirstOrDefault(row => row.GetString("LineId") == "S0001_001")
                ?.GetString("演出");
            if (string.IsNullOrEmpty(openingStartCue) || !openingStartCue.Contains("演出点位起始"))
            {
                throw new InvalidDataException("S0001_001 must configure the skeleton start-point presentation cue in the 演出 column.");
            }

            var activationCue = dialogueTable.Rows
                .FirstOrDefault(row => row.GetString("LineId") == "S0001_005")
                ?.GetString("演出");
            if (string.IsNullOrEmpty(activationCue) || !activationCue.Contains("上升300回初始位"))
            {
                throw new InvalidDataException("S0001_005 must configure the skeleton rise-and-return cue in the 演出 column.");
            }
        }

        private static void ValidateOpeningStoryBackground(ConfigTable storyTable)
        {
            var backgroundId = storyTable.Rows
                .FirstOrDefault(row => row.GetString("StoryId") == "S0001")
                ?.GetString("背景图片");
            if (backgroundId != "宝库and占星室")
            {
                throw new InvalidDataException("S0001 must configure 背景图片=宝库and占星室 in StoryConfig.");
            }
        }
        private static void ValidateDialoguePortraits(ConfigTable dialogueTable)
        {
            var missingCharacters = dialogueTable.Rows
                .Select(row => row.GetString("SpeakerCharacterId"))
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .Where(id => !StoryImageResourceProvider.TryLoadSprite(id, CharacterRoots, out _))
                .ToList();
            if (missingCharacters.Count > 0)
            {
                throw new InvalidDataException("Plot dialogue speakers without portrait resources: " + string.Join(", ", missingCharacters));
            }
        }

        private static void ValidateMapSprites()
        {
            var requiredMapNames = new[]
            {
                "\u4e0a\u57ce\u533a",
                "\u6559\u533a",
                "\u5b66\u9662",
                "\u4e0b\u57ce\u533a",
                "\u4e0b\u6c34\u9053",
                "\u9ed1\u5e02",
                "\u5de5\u4f1a",
                "\u738b\u57ce",
                "\u8425\u5730",
                "\u8001\u94c1\u7684\u94c1\u5320\u94fa"
            };

            var missingMaps = requiredMapNames
                .Where(id => !StoryImageResourceProvider.TryLoadSprite(id, MapRoots, out _))
                .ToList();
            if (missingMaps.Count > 0)
            {
                throw new InvalidDataException("Map names without sprite resources: " + string.Join(", ", missingMaps));
            }
        }

        private static void ValidateSpeakerDisplayNames()
        {
            if (CharacterDisplayNameUtility.GetDisplayName("\u738b\u6b63\u5e38") != "\u738b" ||
                CharacterDisplayNameUtility.GetDisplayName("\u8fd1\u4f8d\u62ac\u624b") != "\u8fd1\u4f8d" ||
                CharacterDisplayNameUtility.GetDisplayName("\u9ab7\u9ac5\u7591\u60d1") != "\u9ab7\u9ac5")
            {
                throw new InvalidDataException("Speaker display names must remove expression suffixes.");
            }
        }

        private static void ValidatePlotCharacterAssets()
        {
            var requiredAssets = new[]
            {
                "Assets/Resources/GameData/Characters/plot_character_\u738b.asset",
                "Assets/Resources/GameData/Characters/plot_character_\u8fd1\u4f8d.asset",
                "Assets/Resources/GameData/Characters/plot_character_\u9ab7\u9ac5.asset"
            };

            foreach (var assetPath in requiredAssets)
            {
                var asset = AssetDatabase.LoadAssetAtPath<CharacterDefinitionAsset>(assetPath);
                if (asset == null || string.IsNullOrEmpty(asset.CharacterName) || asset.ExpressionPortraitIds.Count == 0)
                {
                    throw new InvalidDataException("Missing or incomplete Plot character asset: " + assetPath);
                }
            }
        }

    }
}

