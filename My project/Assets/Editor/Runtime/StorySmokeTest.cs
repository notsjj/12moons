using System.IO;
using System.Linq;
using System.Reflection;
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
            var providerRoot = Path.GetFullPath(DemoConfigDirectory);
            var csvProvider = new CsvConfigProvider(providerRoot);

            var storyTable = csvProvider.LoadTable("StoryConfig");
            var dialogueTable = csvProvider.LoadTable("DialogueConfig");
            var itemTable = csvProvider.LoadTable("ItemConfig");
            var taskTable = csvProvider.LoadTable("TaskConfig");

            if (!storyTable.TryFindById("StoryId", "story_demo_choice", out var choiceStoryRow))
            {
                throw new InvalidDataException("StoryConfig missing story_demo_choice row.");
            }

            var choiceStory = new StoryDefinition(choiceStoryRow);
            if (choiceStory.StoryType != StoryType.Dialogue ||
                !choiceStory.TriggerTaskOnEnd ||
                choiceStory.TriggerTaskId != "task_punish_civilian_01" ||
                choiceStory.AddItemId != "item_money" ||
                choiceStory.AddItemCount != 2)
            {
                throw new InvalidDataException("StoryConfig did not parse story end effects correctly.");
            }

            if (!storyTable.TryFindById("StoryId", "story_demo_text", out var textStoryRow) ||
                new StoryDefinition(textStoryRow).StoryType != StoryType.Text ||
                new StoryDefinition(textStoryRow).TextSegments.Count != 3)
            {
                throw new InvalidDataException("StoryConfig did not parse Text story type or multi-paragraph text.");
            }

            if (!storyTable.TryFindById("StoryId", "story_demo_image", out var imageStoryRow))
            {
                throw new InvalidDataException("StoryConfig missing story_demo_image row.");
            }

            var pageImageStory = new StoryDefinition(imageStoryRow);
            if (pageImageStory.StoryType != StoryType.Image ||
                pageImageStory.ImageDisplayMode != StoryImageDisplayMode.PageSequence ||
                pageImageStory.ImageIds.Count != 3 ||
                pageImageStory.ImageCaptions.Count != 3)
            {
                throw new InvalidDataException("StoryConfig did not parse page image story fields.");
            }

            if (!storyTable.TryFindById("StoryId", "story_demo_comic_image", out var comicStoryRow))
            {
                throw new InvalidDataException("StoryConfig missing story_demo_comic_image row.");
            }

            var comicStory = new StoryDefinition(comicStoryRow);
            if (comicStory.ImageDisplayMode != StoryImageDisplayMode.ComicPanels ||
                comicStory.ImageIds.Count != 4)
            {
                throw new InvalidDataException("StoryConfig did not parse comic panel image fields.");
            }

            var choiceLineRow = dialogueTable.Rows.First(row => row.GetString("LineId") == "line_choice_001");
            var choiceLine = new DialogueLineDefinition(choiceLineRow);
            if (!choiceLine.IsChoice ||
                choiceLine.GetChoiceText(0) != "交出排水图" ||
                choiceLine.GetNextLineId(0) != "line_choice_approve" ||
                choiceLine.GetRequiredItemId(0) != "item_drainage_map" ||
                choiceLine.GetRequiredItemCount(0) != 1 ||
                !choiceLine.ShouldConsumeItem(0) ||
                choiceLine.GetAddItemId(0) != "item_archivist_badge" ||
                choiceLine.GetAddItemCount(0) != 1)
            {
                throw new InvalidDataException("DialogueConfig did not parse choice jump or item effects correctly.");
            }

            var retainSpeakerLine = new DialogueLineDefinition(dialogueTable.Rows.First(row => row.GetString("LineId") == "line_relief_start_003"));
            if (!string.IsNullOrEmpty(retainSpeakerLine.SpeakerCharacterId) ||
                retainSpeakerLine.Position != 1 ||
                retainSpeakerLine.GetNextLineId(0) != "END")
            {
                throw new InvalidDataException("DialogueConfig did not preserve the empty-speaker retention test row.");
            }

            var endLine = new DialogueLineDefinition(dialogueTable.Rows.First(row => row.GetString("LineId") == "line_choice_approve"));
            if (endLine.GetNextLineId(0) != "END")
            {
                throw new InvalidDataException("DialogueConfig did not parse END as the configured choice result terminator.");
            }

            var submitLine = new DialogueLineDefinition(dialogueTable.Rows.First(row => row.GetString("LineId") == "line_submit_002"));
            if (!submitLine.IsItemSubmissionLine() ||
                submitLine.GetRequiredItemId(0) != "item_drainage_map" ||
                submitLine.GetRequiredItemCount(0) != 1 ||
                !submitLine.ShouldConsumeItem(0) ||
                submitLine.GetNextLineId(0) != "line_submit_003")
            {
                throw new InvalidDataException("DialogueConfig did not parse item submission line correctly.");
            }

            if (!itemTable.TryFindById("ItemId", choiceLine.GetRequiredItemId(0), out _) ||
                !itemTable.TryFindById("ItemId", choiceLine.GetAddItemId(0), out _) ||
                !taskTable.TryFindById("TaskId", choiceStory.TriggerTaskId, out _))
            {
                throw new InvalidDataException("Story demo references missing configured item or task ids.");
            }

            var data = new GameRuntimeData();
            data.Reset("disaster_flood_01", 18);
            var queueEntry = data.QueueStory("story_demo_choice", "task_demo_relief_01", "task_stage_relief_prepare", RuntimeStoryQueueTiming.StageStart);
            if (data.StoryQueue.Count != 1 || !data.RemoveStoryQueueEntry(queueEntry) || data.StoryQueue.Count != 0)
            {
                throw new InvalidDataException("Runtime story queue did not enqueue and remove correctly.");
            }

            ValidateSubmissionPlayback();
            ValidatePortraitBrightnessSpeakerState();

            Debug.Log("Story smoke test passed. StoryConfig parses Dialogue/Image/Text with multi-image and multi-paragraph fields, DialogueConfig parses choices and item submission, runtime story progress restores submission waits, and story queue removes played entries.");
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
                runtimeDataService.CreateNewGame("disaster_flood_01");

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
    }
}
