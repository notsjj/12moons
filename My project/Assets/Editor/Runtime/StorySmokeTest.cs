using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using TwelveMoons.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class StorySmokeTest
    {
        private const string DemoConfigDirectory = "Assets/StreamingAssets/Configs/Demo";

        [MenuItem("Twelve Moons/Tests/Run Story Smoke Test")]
        public static void Run()
        {
            ValidateStoryNameLoadsMatchingDialogueTable();
            ValidateChineseStoryBackgroundField();
            ValidateDialogueLineBackgroundField();
            ValidateStoryScheduleFields();
            ValidateStoryQueueDedupesSameStoryTiming();
            ValidateFloodEndingResolver();
            ValidateStoryPanelUsesDialoguePanelImageForBackground();
            ValidateStoryCanStartAtSpecificLine();
            ValidateDialogueCharacterMoveClearsPreviousSlot();
            ValidatePortraitBrightnessSpeakerState();

            Debug.Log("Story smoke test passed. StoryService resolves named dialogue CSVs for S0001, S0002, and S0004, and keeps core story panel portrait behavior wired.");
        }

        private static void ValidateDialogueCharacterMoveClearsPreviousSlot()
        {
            var testRoot = new GameObject("StoryDialogueCharacterMoveSmokeTest");
            try
            {
                var panel = testRoot.AddComponent<StoryPanelView>();
                InvokePrivate(
                    panel,
                    "UpdateDialogueCharacter",
                    new object[] { CreateDialogueLine("line_right", "\u8fd1\u4f8d\u6b63\u5e38", 0) });
                InvokePrivate(
                    panel,
                    "UpdateDialogueCharacter",
                    new object[] { CreateDialogueLine("line_left", "\u8fd1\u4f8d\u4e25\u8083", 1) });

                var leftCharacterId = GetPrivateStringField(panel, "leftCharacterId");
                var rightCharacterId = GetPrivateStringField(panel, "rightCharacterId");
                if (leftCharacterId != "\u8fd1\u4f8d\u4e25\u8083" || !string.IsNullOrEmpty(rightCharacterId))
                {
                    throw new InvalidDataException("StoryPanel \u540c\u4e00\u89d2\u8272\u5207\u6362\u5de6\u53f3\u4f4d\u7f6e\u65f6\u5fc5\u987b\u6e05\u7a7a\u65e7\u4f4d\u7f6e\uff0c\u907f\u514d\u5de6\u53f3\u7acb\u7ed8\u663e\u793a\u540c\u4e00\u4e2a\u4eba\u7269\u3002");
                }
            }
            finally
            {
                Object.DestroyImmediate(testRoot);
            }
        }

        private static void ValidateChineseStoryBackgroundField()
        {
            var story = new StoryDefinition(new ConfigRow(new Dictionary<string, string>
            {
                { "StoryId", "story_background_field_test" },
                { "StoryName", "\u80cc\u666f\u5b57\u6bb5\u6d4b\u8bd5" },
                { "StoryType", "Dialogue" },
                { "\u80cc\u666f\u56fe\u7247", "\u5b9d\u5e93and\u5360\u661f\u5ba4" }
            }));

            if (story.BackgroundImageId != "\u5b9d\u5e93and\u5360\u661f\u5ba4")
            {
                throw new InvalidDataException("StoryDefinition must read StoryConfig.\u80cc\u666f\u56fe\u7247 so dialogue backgrounds can use per-story map sprites.");
            }
        }

        private static void ValidateDialogueLineBackgroundField()
        {
            var line = new DialogueLineDefinition(new ConfigRow(new Dictionary<string, string>
            {
                { "LineId", "story_dialogue_background_test_001" },
                { "StoryId", "story_dialogue_background_test" },
                { "NextLineId", "END" },
                { "Content", "\u6d4b\u8bd5" },
                { "\u80cc\u666fID", "\u6559\u533a\u80cc\u666f" }
            }));

            if (line.BackgroundImageId != "\u6559\u533a\u80cc\u666f")
            {
                throw new InvalidDataException("DialogueLineDefinition must read \u80cc\u666fID so each dialogue row can override the story panel background.");
            }
        }

        private static void ValidateStoryScheduleFields()
        {
            var story = new StoryDefinition(new ConfigRow(new Dictionary<string, string>
            {
                { "StoryId", "story_schedule_field_test" },
                { "StoryName", "schedule" },
                { "StoryType", "Dialogue" },
                { "\u89e6\u53d1\u5355\u4f4did", "P0013" },
                { "\u56de\u5408\u6570", "7" }
            }));

            if (story.TriggerUnitId != "P0013" || story.RoundNumber != 7)
            {
                throw new InvalidDataException("StoryDefinition must read StoryConfig.\u89e6\u53d1\u5355\u4f4did and StoryConfig.\u56de\u5408\u6570 for round/point scheduling.");
            }
        }

        private static void ValidateStoryQueueDedupesSameStoryTiming()
        {
            var data = new GameRuntimeData();
            data.Reset("DI0001", 30);
            data.QueueStory("S0002", "T0001", "TS0001", RuntimeStoryQueueTiming.BeforeDocument);
            data.QueueStory("S0002", string.Empty, string.Empty, 1, RuntimeStoryQueueTiming.BeforeDocument);

            if (data.StoryQueue.Count != 1 ||
                data.StoryQueue[0].TaskId != "T0001" ||
                data.StoryQueue[0].TaskStageId != "TS0001")
            {
                throw new InvalidDataException("Runtime story queue must dedupe the same StoryId/round/timing so StoryConfig scheduling cannot duplicate or replace TaskStage before-document actors.");
            }
        }

        private static void ValidateFloodEndingResolver()
        {
            var data = new GameRuntimeData();
            data.Reset("DI0001", 30);

            if (FloodEndingStoryResolver.ResolveStoryId(data) != "S0032")
            {
                throw new InvalidDataException("Flood ending resolver should fall back to S0032/\u84c4\u6c34\u6c60 when no stronger runtime condition exists.");
            }

            data.GetOrCreateTask("T0007").Activate(21);
            data.GetOrCreateTask("T0007").AddScore(2);
            data.GetOrCreateTask("T0007").Complete(23);
            if (FloodEndingStoryResolver.ResolveStoryId(data) != "S0029")
            {
                throw new InvalidDataException("Completed relief task T0007 should resolve to S0029/\u536b\u961f\u957f flood ending.");
            }
        }

        private static void ValidateStoryPanelUsesDialoguePanelImageForBackground()
        {
            var testRoot = new GameObject("StoryDialogueBackgroundImageSmokeTest", typeof(Image));
            try
            {
                var rootImage = testRoot.GetComponent<Image>();
                rootImage.color = Color.red;

                var dialogueObject = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
                dialogueObject.transform.SetParent(testRoot.transform, false);
                var dialogueImage = dialogueObject.GetComponent<Image>();
                dialogueImage.color = Color.white;

                var panel = testRoot.AddComponent<StoryPanelView>();
                SetPrivateField(panel, "dialoguePanel", dialogueObject);
                InvokePrivate(panel, "Awake");
                InvokePrivate(panel, "ApplyBackgroundImage", new object[] { string.Empty });

                if (dialogueImage.color != Color.black || rootImage.color != Color.red)
                {
                    throw new InvalidDataException("StoryPanel must apply dialogue \u80cc\u666fID sprites to the \u5bf9\u8bdd\u9762\u677f Image before falling back to the root image.");
                }
            }
            finally
            {
                Object.DestroyImmediate(testRoot);
            }
        }

        private static void ValidateStoryCanStartAtSpecificLine()
        {
            var testRoot = new GameObject("StoryStartAtLineSmokeTest");
            try
            {
                var configManager = testRoot.AddComponent<ConfigManager>();
                SetPrivateField(configManager, "relativeConfigDirectory", "Configs/Plot");
                SetPrivateField(configManager, "loadOnAwake", false);
                configManager.BuildDefaultProviders();

                var runtimeDataService = testRoot.AddComponent<RuntimeDataService>();
                SetPrivateField(runtimeDataService, "configManager", configManager);
                runtimeDataService.CreateNewGame("DI0001");

                var storyService = testRoot.AddComponent<StoryService>();
                SetPrivateField(storyService, "configManager", configManager);
                SetPrivateField(storyService, "runtimeDataService", runtimeDataService);
                InvokePrivate(storyService, "Awake");

                if (!storyService.StartStoryAtLine("S0002", "S0002_020") ||
                    storyService.CurrentPlayback == null ||
                    storyService.CurrentPlayback.CurrentLine == null ||
                    storyService.CurrentPlayback.CurrentLine.LineId != "S0002_020")
                {
                    throw new InvalidDataException("StoryService ????????????????????????? S0002_020?");
                }
            }
            finally
            {
                Object.DestroyImmediate(testRoot);
            }
        }


        private static void ValidateStoryNameLoadsMatchingDialogueTable()
        {
            var testRoot = new GameObject("StoryNameDialogueTableSmokeTest");
            try
            {
                var configManager = testRoot.AddComponent<ConfigManager>();
                SetPrivateField(configManager, "relativeConfigDirectory", "Configs/Plot");
                SetPrivateField(configManager, "loadOnAwake", false);
                configManager.BuildDefaultProviders();

                var runtimeDataService = testRoot.AddComponent<RuntimeDataService>();
                SetPrivateField(runtimeDataService, "configManager", configManager);
                runtimeDataService.CreateNewGame("DI0001");

                var storyService = testRoot.AddComponent<StoryService>();
                SetPrivateField(storyService, "configManager", configManager);
                SetPrivateField(storyService, "runtimeDataService", runtimeDataService);
                InvokePrivate(storyService, "Awake");

                if (!storyService.StartStory("S0001") ||
                    storyService.CurrentPlayback == null ||
                    storyService.CurrentPlayback.Story.StoryName != "[\u4e3b\u7ebf]\u6fc0\u6d3b\u9ab7\u9ac5" ||
                    storyService.CurrentPlayback.Story.StoryType != StoryType.Dialogue ||
                    storyService.CurrentPlayback.CurrentLine == null ||
                    storyService.CurrentPlayback.CurrentLine.LineId != "S0001_001" ||
                    storyService.CurrentPlayback.CurrentLine.PresentationCue != "\u9ab7\u9ac5_\u6f14\u51fa\u70b9\u4f4d\u8d77\u59cb")
                {
                    throw new InvalidDataException("StoryService must resolve S0001 through StoryConfig.StoryName=[\u4e3b\u7ebf]\u6fc0\u6d3b\u9ab7\u9ac5 and load the matching \u6fc0\u6d3b\u9ab7\u9ac5 dialogue table.");
                }

                if (!storyService.StartStory("S0004") ||
                    storyService.CurrentPlayback == null ||
                    storyService.CurrentPlayback.Story.StoryName != "[\u4e3b\u7ebf]\u521d\u6b21\u5de1\u903b-\u6559\u533a" ||
                    storyService.CurrentPlayback.CurrentLine == null ||
                    storyService.CurrentPlayback.CurrentLine.LineId != "S0004_001")
                {
                    throw new InvalidDataException("StoryService must resolve S0004 through StoryConfig.StoryName=[\u4e3b\u7ebf]\u521d\u6b21\u5de1\u903b-\u6559\u533a and load the matching \u521d\u6b21\u5de1\u903b\u00b7\u6559\u533a dialogue table.");
                }
            }
            finally
            {
                Object.DestroyImmediate(testRoot);
            }
        }


        private static void ValidatePortraitBrightnessSpeakerState()
        {
            var testRoot = new GameObject("StoryPortraitBrightnessSmokeTest");
            try
            {
                var panel = testRoot.AddComponent<StoryPanelView>();
                var leftPortrait = CreatePortrait(testRoot.transform, "LeftPortrait");
                var rightPortrait = CreatePortrait(testRoot.transform, "RightPortrait");

                InvokePrivate(
                    panel,
                    "SetPortrait",
                    new object[] { leftPortrait, "left_character", "right_character" });
                InvokePrivate(
                    panel,
                    "SetPortrait",
                    new object[] { rightPortrait, "right_character", "right_character" });

                if (leftPortrait.transform.localScale != Vector3.one || rightPortrait.transform.localScale != Vector3.one)
                {
                    throw new InvalidDataException("StoryPanel speaker state must not enlarge portraits; both portrait scales should stay at Vector3.one.");
                }

                if (rightPortrait.color != panel.ActiveSpeakerPortraitColor ||
                    leftPortrait.color != panel.InactiveSpeakerPortraitColor)
                {
                    throw new InvalidDataException("StoryPanel speaker state must use brightness colors: active speaker bright, inactive speaker dim.");
                }
            }
            finally
            {
                Object.DestroyImmediate(testRoot);
            }
        }

        private static Image CreatePortrait(Transform parent, string objectName)
        {
            var portraitObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            portraitObject.transform.SetParent(parent, false);
            return portraitObject.GetComponent<Image>();
        }

        private static DialogueLineDefinition CreateDialogueLine(string lineId, string speakerCharacterId, int position)
        {
            return new DialogueLineDefinition(new ConfigRow(new Dictionary<string, string>
            {
                { "LineId", lineId },
                { "StoryId", "story_dialogue_position_test" },
                { "NextLineId", "END" },
                { "SpeakerCharacterId", speakerCharacterId },
                { "Content", "\u6d4b\u8bd5" },
                { "Position", position.ToString() },
                { "IsChoice", "false" }
            }));
        }

        private static void ValidateSubmissionPlayback()
        {
            var testRoot = new GameObject("StorySmokeTestRoot");
            try
            {
                var configManager = testRoot.AddComponent<ConfigManager>();
                SetPrivateField(configManager, "relativeConfigDirectory", "Configs/Demo");
                SetPrivateField(configManager, "loadOnAwake", false);
                configManager.BuildDefaultProviders();

                var runtimeDataService = testRoot.AddComponent<RuntimeDataService>();
                SetPrivateField(runtimeDataService, "configManager", configManager);
                runtimeDataService.CreateNewGame("DI0001");

                var inventoryService = testRoot.AddComponent<InventoryService>();
                SetPrivateField(inventoryService, "configManager", configManager);
                SetPrivateField(inventoryService, "runtimeDataService", runtimeDataService);
                InvokePrivate(inventoryService, "Awake");
                InvokePrivate(inventoryService, "Start");

                var roundService = testRoot.AddComponent<RoundService>();
                SetPrivateField(roundService, "configManager", configManager);
                SetPrivateField(roundService, "runtimeDataService", runtimeDataService);

                var taskService = testRoot.AddComponent<TaskService>();
                SetPrivateField(taskService, "configManager", configManager);
                SetPrivateField(taskService, "runtimeDataService", runtimeDataService);
                SetPrivateField(taskService, "roundService", roundService);
                InvokePrivate(taskService, "Awake");

                var storyService = testRoot.AddComponent<StoryService>();
                SetPrivateField(storyService, "configManager", configManager);
                SetPrivateField(storyService, "runtimeDataService", runtimeDataService);
                SetPrivateField(storyService, "inventoryService", inventoryService);
                SetPrivateField(storyService, "taskService", taskService);
                InvokePrivate(storyService, "Awake");

                if (!storyService.StartStory("story_demo_submission"))
                {
                    throw new InvalidDataException("StoryService could not start submission demo.");
                }

                storyService.Continue();
                if (storyService.CurrentPlayback == null ||
                    storyService.CurrentPlayback.CurrentLine == null ||
                    storyService.CurrentPlayback.CurrentLine.LineId != "line_submit_002" ||
                    !storyService.CurrentPlayback.IsWaitingForSubmission)
                {
                    throw new InvalidDataException("StoryService did not enter item submission wait.");
                }

                storyService.ExitItemSubmission();
                if (!runtimeDataService.Data.TryGetStoryProgress("story_demo_submission", out var progress) ||
                    progress.LineId != "line_submit_002" ||
                    !progress.WaitingForSubmission)
                {
                    throw new InvalidDataException("StoryService did not save submission progress on exit.");
                }

                if (!storyService.StartStory("story_demo_submission") ||
                    storyService.CurrentPlayback.CurrentLine.LineId != "line_submit_002" ||
                    !storyService.CurrentPlayback.IsWaitingForSubmission)
                {
                    throw new InvalidDataException("StoryService did not restore saved submission progress.");
                }

                inventoryService.AddItem("item_drainage_map", 1);
                storyService.SubmitCurrentItems();
                if (inventoryService.GetCount("item_drainage_map") != 0 ||
                    storyService.CurrentPlayback.CurrentLine.LineId != "line_submit_003")
                {
                    throw new InvalidDataException("StoryService did not consume submitted item and continue.");
                }

                storyService.Continue();
                if (storyService.CurrentPlayback != null ||
                    inventoryService.GetCount("item_archivist_badge") != 1)
                {
                    throw new InvalidDataException("StoryService did not complete submission story and grant StoryConfig reward.");
                }
            }
            finally
            {
                Object.DestroyImmediate(testRoot);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(target, null);
        }

        private static void InvokePrivate(object target, string methodName, object[] parameters)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(target, parameters);
        }

        private static string GetPrivateStringField(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(target) as string;
        }
    }
}
