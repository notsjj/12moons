using System.IO;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class TaskSmokeTest
    {
        private const string DemoConfigDirectory = "Assets/StreamingAssets/Configs/Demo";
        private const string OpeningTaskId = "T0001";
        private const string OpeningStageId = "TS0001";

        [MenuItem("Twelve Moons/Tests/Run Task Smoke Test")]
        public static void Run()
        {
            var providerRoot = Path.GetFullPath(DemoConfigDirectory);
            var csvProvider = new CsvConfigProvider(providerRoot);
            var jsonProvider = new JsonConfigProvider(providerRoot);

            var disasterTable = jsonProvider.LoadTable("DisasterConfig");
            var taskTable = csvProvider.LoadTable("TaskConfig");
            var taskStageTable = csvProvider.LoadTable("TaskStageConfig");

            if (!disasterTable.TryFindById("DisasterId", "DI0001", out var disasterRow))
            {
                throw new InvalidDataException("DisasterConfig missing DI0001 row.");
            }

            if (!taskTable.TryFindById("TaskId", OpeningTaskId, out var taskRow) ||
                taskRow.GetInt("StartRound") != 1)
            {
                throw new InvalidDataException("TaskConfig must activate T0001 on round 1.");
            }

            if (!taskStageTable.TryFindById("TaskStageId", OpeningStageId, out var stageRow))
            {
                throw new InvalidDataException("TaskStageConfig missing TS0001 row.");
            }

            var data = new GameRuntimeData();
            data.Reset("DI0001", disasterRow.GetInt("TotalRound"));
            var task = data.GetOrCreateTask(taskRow.GetString("TaskId"));
            task.Activate(data.CurrentRound);

            var stage = new TaskStageDefinition(stageRow);
            data.QueueStory(stage.StartStoryId, task.TaskId, stage.TaskStageId, RuntimeStoryQueueTiming.StageStart);
            data.QueueStory(stage.BeforeDocumentStoryId, task.TaskId, stage.TaskStageId, RuntimeStoryQueueTiming.BeforeDocument);

            task.AddScore(2);
            task.Complete(data.CurrentRound);

            if (stage.TaskId != OpeningTaskId ||
                stage.StartStoryId != "S0001" ||
                stage.BeforeDocumentStoryId != "S0002" ||
                stage.BeforeDocumentCharacterId != "C0028" ||
                data.StoryQueue.Count != 2 ||
                data.StoryQueue[0].StoryId != "S0001" ||
                data.StoryQueue[0].Timing != RuntimeStoryQueueTiming.StageStart ||
                data.StoryQueue[1].StoryId != "S0002" ||
                data.StoryQueue[1].Timing != RuntimeStoryQueueTiming.BeforeDocument ||
                task.Status != TaskRuntimeStatus.Completed ||
                task.Score != 2)
            {
                throw new InvalidDataException("Task stage smoke test failed to queue S0001 and S0002 from TaskStageConfig.");
            }

            Debug.Log("Task smoke test passed. T0001 activates on round 1 and TS0001 queues S0001 as stage start plus S0002 as before-document story.");
        }
    }
}
