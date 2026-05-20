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
        private const string DemoTaskId = "task_demo_relief_01";

        [MenuItem("Twelve Moons/Tests/Run Task Smoke Test")]
        public static void Run()
        {
            var providerRoot = Path.GetFullPath(DemoConfigDirectory);
            var csvProvider = new CsvConfigProvider(providerRoot);
            var jsonProvider = new JsonConfigProvider(providerRoot);

            var disasterTable = jsonProvider.LoadTable("DisasterConfig");
            var taskTable = csvProvider.LoadTable("TaskConfig");
            var taskStageTable = csvProvider.LoadTable("TaskStageConfig");

            if (!disasterTable.TryFindById("DisasterId", "disaster_flood_01", out var disasterRow))
            {
                throw new InvalidDataException("DisasterConfig missing disaster_flood_01 row.");
            }

            if (!taskTable.TryFindById("TaskId", DemoTaskId, out var taskRow))
            {
                throw new InvalidDataException("TaskConfig missing phase 6 demo task row.");
            }

            var data = new GameRuntimeData();
            data.Reset("disaster_flood_01", disasterRow.GetInt("TotalRound"));
            var task = data.GetOrCreateTask(taskRow.GetString("TaskId"));
            task.Activate(data.CurrentRound);

            var stageCount = 0;
            foreach (var row in taskStageTable.Rows)
            {
                if (row.GetString("TaskId") == DemoTaskId)
                {
                    stageCount++;
                    var stage = new TaskStageDefinition(row);
                    if (stage.StartOffsetRound == 0)
                    {
                        data.QueueStory(stage.StartStoryId, task.TaskId, stage.TaskStageId, RuntimeStoryQueueTiming.StageStart);
                        data.ReceivePhaseTestLetter(stage.StartLetterId);
                        foreach (var documentId in stage.LinkedDocumentIds)
                        {
                            data.QueueDocument(documentId, task.TaskId, stage.TaskStageId, stage.BeforeDocumentCharacterId);
                        }
                    }
                }
            }

            task.AddScore(2);
            task.Complete(data.CurrentRound);

            if (stageCount != 2 ||
                data.StoryQueue.Count != 1 ||
                data.Letters.Count != 1 ||
                data.DocumentQueue.Count != 1 ||
                task.Status != TaskRuntimeStatus.Completed ||
                task.Score != 2)
            {
                throw new InvalidDataException("Task smoke test failed.");
            }

            Debug.Log("Task smoke test passed. Demo task activates, stage config parses, one start story queues, one start letter is received, one task document queues, and task score can complete the task.");
        }

        private static RuntimeLetterState ReceivePhaseTestLetter(this GameRuntimeData data, string letterId)
        {
            return string.IsNullOrEmpty(letterId) ? null : data.AddLetter(letterId);
        }
    }
}
