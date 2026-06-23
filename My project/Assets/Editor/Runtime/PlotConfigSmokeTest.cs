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

            ValidateStoryConfigScheduleFields(storyTable);
            ValidateOpeningRoundStoryTriggers(storyTable, csvProvider.LoadTable("SideEventConfig"));
            ValidateStoryConfigReferences(storyTable, taskTable);
            ValidateSideEventReferences(csvProvider, storyTable, taskTable);
            ValidateOpeningStoryBackground(storyTable);
            ValidateDialogueBackgrounds(dialogueTable);
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
                ?.GetString("\u80cc\u666f\u56fe\u7247");
            if (backgroundId != "\u5b9d\u5e93and\u5360\u661f\u5ba4")
            {
                throw new InvalidDataException("S0001 must configure \u80cc\u666f\u56fe\u7247=\u5b9d\u5e93and\u5360\u661f\u5ba4 in StoryConfig.");
            }
        }

        private static void ValidateStoryConfigScheduleFields(ConfigTable storyTable)
        {
            var openingStory = storyTable.Rows.FirstOrDefault(row => row.GetString("StoryId") == "S0001");
            if (openingStory == null ||
                openingStory.GetString("\u89e6\u53d1\u5355\u4f4did") != "\u516c\u6587\u524d" ||
                openingStory.GetInt("\u56de\u5408\u6570") != 1)
            {
                throw new InvalidDataException("StoryConfig S0001 must configure \u89e6\u53d1\u5355\u4f4did=\u516c\u6587\u524d and \u56de\u5408\u6570=1.");
            }

            var cityIntroStory = storyTable.Rows.FirstOrDefault(row => row.GetString("StoryId") == "S0003");
            if (cityIntroStory == null ||
                cityIntroStory.GetString("\u89e6\u53d1\u5355\u4f4did") != "\u63a2\u7d22\u524d" ||
                cityIntroStory.GetInt("\u56de\u5408\u6570") != 1)
            {
                throw new InvalidDataException("StoryConfig S0003 must configure the round 1 \u63a2\u7d22\u524d trigger.");
            }

            var upperCityPoint = storyTable.Rows.FirstOrDefault(row => row.GetString("StoryId") == "S0034");
            if (upperCityPoint == null || upperCityPoint.GetString("\u89e6\u53d1\u5355\u4f4did") != "P0009|P0011|P0012")
            {
                throw new InvalidDataException("Upper-city scheduled stories must use the P0009|P0011|P0012 random point pool.");
            }
        }

        private static void ValidateOpeningRoundStoryTriggers(ConfigTable storyTable, ConfigTable sideEventTable)
        {
            AssertStoryTrigger(storyTable, "S0001", "公文前", 1);
            AssertStoryTrigger(storyTable, "S0002", "公文1", 1);
            AssertStoryTrigger(storyTable, "S0003", "探索前", 1);
            AssertStoryTrigger(storyTable, "S0004", "P0013", 1);
            AssertStoryTrigger(storyTable, "S0005", "P0003", 1);
            AssertStoryTrigger(storyTable, "S0006", "P0004", 1);

            var firstPointStories = new[] { "S0004", "S0005", "S0006" };
            var sideEventStoryIds = sideEventTable.Rows
                .Where(row => row.GetInt("Round") <= 1 && row.GetInt("ExpireRound", 1) >= 1)
                .Select(row => row.GetString("StoryId"))
                .ToHashSet();
            if (!sideEventStoryIds.Contains("S0004"))
            {
                throw new InvalidDataException("Opening round S0004 must be reachable through SideEventConfig SE0001 at P0013.");
            }

            foreach (var storyId in firstPointStories.Where(storyId => storyId != "S0004"))
            {
                var row = storyTable.Rows.FirstOrDefault(candidate => candidate.GetString("StoryId") == storyId);
                if (row == null || string.IsNullOrEmpty(row.GetString("触发单位id")))
                {
                    throw new InvalidDataException($"Opening round point story {storyId} must keep a StoryConfig point trigger so CitySideEventService can synthesize it.");
                }
            }
        }

        private static void AssertStoryTrigger(ConfigTable storyTable, string storyId, string triggerUnitId, int roundNumber)
        {
            var row = storyTable.Rows.FirstOrDefault(candidate => candidate.GetString("StoryId") == storyId);
            if (row == null ||
                row.GetString("触发单位id") != triggerUnitId ||
                row.GetInt("回合数") != roundNumber)
            {
                throw new InvalidDataException($"StoryConfig {storyId} must configure {triggerUnitId} at round {roundNumber} for the opening six-story flow.");
            }
        }

        private static void ValidateStoryConfigReferences(ConfigTable storyTable, ConfigTable taskTable)
        {
            var taskIds = taskTable.Rows
                .Select(row => row.GetString("TaskId"))
                .Where(IsTaskId)
                .ToHashSet();

            foreach (var row in storyTable.Rows.Where(row => IsStoryId(row.GetString("StoryId"))))
            {
                var story = new StoryDefinition(row);
                if (story.StoryType != StoryType.Dialogue)
                {
                    throw new InvalidDataException($"StoryConfig {story.StoryId} must use StoryType=Dialogue.");
                }

                if (string.IsNullOrEmpty(story.StoryContentAssetId) ||
                    !File.Exists(Path.Combine(PlotConfigDirectory, story.StoryContentAssetId + ".csv")))
                {
                    throw new FileNotFoundException($"StoryConfig {story.StoryId} references missing dialogue CSV: {story.StoryContentAssetId}.");
                }

                if (string.IsNullOrEmpty(story.BackgroundImageId) ||
                    !StoryImageResourceProvider.TryLoadSprite(story.BackgroundImageId, MapRoots, out _))
                {
                    throw new InvalidDataException($"StoryConfig {story.StoryId} references missing background sprite: {story.BackgroundImageId}.");
                }

                if (!string.IsNullOrEmpty(story.TriggerTaskId) && !taskIds.Contains(story.TriggerTaskId))
                {
                    throw new InvalidDataException($"StoryConfig {story.StoryId} TriggerTaskId is missing from TaskConfig: {story.TriggerTaskId}.");
                }
            }
        }

        private static void ValidateSideEventReferences(CsvConfigProvider csvProvider, ConfigTable storyTable, ConfigTable taskTable)
        {
            var sideEventTable = csvProvider.LoadTable("SideEventConfig");
            var pointTable = csvProvider.LoadTable("CityPointConfig");
            var characterTable = csvProvider.LoadTable("CharacterConfig");

            var storyIds = storyTable.Rows.Select(row => row.GetString("StoryId")).Where(IsStoryId).ToHashSet();
            var taskIds = taskTable.Rows.Select(row => row.GetString("TaskId")).Where(IsTaskId).ToHashSet();
            var pointIds = pointTable.Rows.Select(row => row.GetString("PointId")).Where(IsPointId).ToHashSet();
            var characterIds = characterTable.Rows.Select(row => row.GetString("CharacterId")).Where(IsCharacterId).ToHashSet();

            foreach (var row in sideEventTable.Rows.Where(row => IsSideEventId(row.GetString("SideEventId"))))
            {
                var sideEventId = row.GetString("SideEventId");
                var storyId = row.GetString("StoryId");
                var pointId = row.GetString("PointId");
                var characterId = row.GetString("DisplayCharacterId");
                var requiredTaskId = row.GetString("RequiredTaskId");

                if (!storyIds.Contains(storyId))
                {
                    throw new InvalidDataException($"SideEventConfig {sideEventId} references missing StoryId: {storyId}.");
                }

                if (!pointIds.Contains(pointId))
                {
                    throw new InvalidDataException($"SideEventConfig {sideEventId} references missing PointId: {pointId}.");
                }

                if (!characterIds.Contains(characterId))
                {
                    throw new InvalidDataException($"SideEventConfig {sideEventId} references missing DisplayCharacterId: {characterId}.");
                }

                if (!string.IsNullOrEmpty(requiredTaskId) && !taskIds.Contains(requiredTaskId))
                {
                    throw new InvalidDataException($"SideEventConfig {sideEventId} references missing RequiredTaskId: {requiredTaskId}.");
                }
            }
        }

        private static bool IsStoryId(string value)
        {
            return HasPrefixedFourDigitId(value, 'S');
        }

        private static bool IsTaskId(string value)
        {
            return HasPrefixedFourDigitId(value, 'T');
        }

        private static bool IsPointId(string value)
        {
            return HasPrefixedFourDigitId(value, 'P');
        }

        private static bool IsCharacterId(string value)
        {
            return HasPrefixedFourDigitId(value, 'C');
        }

        private static bool IsSideEventId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Length == 6 &&
                   value.StartsWith("SE", System.StringComparison.Ordinal) &&
                   value.Skip(2).All(char.IsDigit);
        }

        private static bool HasPrefixedFourDigitId(string value, char prefix)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Length == 5 &&
                   value[0] == prefix &&
                   value.Skip(1).All(char.IsDigit);
        }
        private static void ValidateDialogueBackgrounds(ConfigTable dialogueTable)
        {
            var missingBackgrounds = dialogueTable.Rows
                .Select(row => row.GetString("\u80cc\u666fID", row.GetString("BackgroundImageId")))
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .Where(id => !StoryImageResourceProvider.TryLoadSprite(id, MapRoots, out _))
                .ToList();
            if (missingBackgrounds.Count > 0)
            {
                throw new InvalidDataException("Plot dialogue backgrounds without map sprite resources: " + string.Join(", ", missingBackgrounds));
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
