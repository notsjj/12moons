using System;
using System.Collections.Generic;
using TwelveMoons.Core.Config;
using UnityEngine;

namespace TwelveMoons.Core.Runtime
{
    public sealed class TaskService : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private ConfigManager configManager;
        [SerializeField] private RuntimeDataService runtimeDataService;
        [SerializeField] private RoundService roundService;

        private readonly List<TaskDefinition> definitions = new List<TaskDefinition>();
        private readonly Dictionary<string, TaskDefinition> definitionsById =
            new Dictionary<string, TaskDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<TaskStageDefinition>> stagesByTaskId =
            new Dictionary<string, List<TaskStageDefinition>>(StringComparer.Ordinal);

        public event Action TasksChanged;

        public IReadOnlyList<TaskDefinition> Definitions => definitions;

        public IReadOnlyDictionary<string, List<TaskStageDefinition>> StagesByTaskId => stagesByTaskId;

        private void Awake()
        {
            ResolveDependencies();
            LoadTaskConfig();
        }

        private void OnEnable()
        {
            if (roundService != null)
            {
                roundService.RoundChanged += ProcessCurrentRound;
            }
        }

        private void OnDisable()
        {
            if (roundService != null)
            {
                roundService.RoundChanged -= ProcessCurrentRound;
            }
        }

        private void Start()
        {
            ProcessCurrentRound();
        }

        public void Refresh()
        {
            LoadTaskConfig();
            ProcessCurrentRound();
        }

        public bool TryGetDefinition(string taskId, out TaskDefinition definition)
        {
            return definitionsById.TryGetValue(taskId, out definition);
        }

        public RuntimeTaskState ActivateTask(string taskId)
        {
            if (!TryGetUsableTask(taskId, out _))
            {
                return null;
            }

            var state = runtimeDataService.ActivateTask(taskId);
            ProcessTaskStages(state);
            NotifyTasksChanged();
            return state;
        }

        public RuntimeTaskState AddTaskScore(string taskId, int delta)
        {
            if (!TryGetUsableTask(taskId, out _))
            {
                return null;
            }

            var state = runtimeDataService.Data.GetOrCreateTask(taskId);
            state.AddScore(delta);
            EvaluateTaskResult(state);
            NotifyTasksChanged();
            return state;
        }

        public void ProcessCurrentRound()
        {
            if (runtimeDataService == null)
            {
                Debug.LogWarning("TaskService missing RuntimeDataService.", this);
                return;
            }

            ActivateConfiguredTasksForCurrentRound();

            foreach (var state in runtimeDataService.Data.Tasks)
            {
                if (state.Status != TaskRuntimeStatus.Active)
                {
                    continue;
                }

                ProcessTaskStages(state);
                EvaluateTaskResult(state);
            }

            NotifyTasksChanged();
        }

        public TaskStageDefinition GetCurrentStage(RuntimeTaskState state)
        {
            if (state == null ||
                state.Status != TaskRuntimeStatus.Active ||
                !stagesByTaskId.TryGetValue(state.TaskId, out var stages))
            {
                return null;
            }

            var relativeRound = runtimeDataService.Data.CurrentRound - state.ActivatedRound;
            TaskStageDefinition current = null;
            foreach (var stage in stages)
            {
                if (relativeRound >= stage.StartOffsetRound && relativeRound <= stage.EndOffsetRound)
                {
                    current = stage;
                }
            }

            return current;
        }

        public IReadOnlyList<TaskStageDefinition> GetStages(string taskId)
        {
            return stagesByTaskId.TryGetValue(taskId, out var stages)
                ? stages
                : Array.Empty<TaskStageDefinition>();
        }

        private void ResolveDependencies()
        {
            if (configManager == null)
            {
                configManager = FindFirstObjectByType<ConfigManager>();
            }

            if (runtimeDataService == null)
            {
                runtimeDataService = FindFirstObjectByType<RuntimeDataService>();
            }

            if (roundService == null)
            {
                roundService = FindFirstObjectByType<RoundService>();
            }
        }

        private void LoadTaskConfig()
        {
            definitions.Clear();
            definitionsById.Clear();
            stagesByTaskId.Clear();

            if (configManager == null)
            {
                Debug.LogWarning("TaskService missing ConfigManager.", this);
                return;
            }

            LoadTaskDefinitions();
            LoadTaskStageDefinitions();
        }

        private void LoadTaskDefinitions()
        {
            if (!configManager.TryGetTable("TaskConfig", out var table))
            {
                Debug.LogWarning("TaskService cannot load TaskConfig.", this);
                return;
            }

            foreach (var row in table.Rows)
            {
                var definition = new TaskDefinition(row);
                if (string.IsNullOrEmpty(definition.TaskId))
                {
                    continue;
                }

                definitions.Add(definition);
                definitionsById[definition.TaskId] = definition;
            }
        }

        private void LoadTaskStageDefinitions()
        {
            if (!configManager.TryGetTable("TaskStageConfig", out var table))
            {
                Debug.LogWarning("TaskService cannot load TaskStageConfig.", this);
                return;
            }

            foreach (var row in table.Rows)
            {
                var stage = new TaskStageDefinition(row);
                if (string.IsNullOrEmpty(stage.TaskStageId) || string.IsNullOrEmpty(stage.TaskId))
                {
                    continue;
                }

                if (!stagesByTaskId.TryGetValue(stage.TaskId, out var stages))
                {
                    stages = new List<TaskStageDefinition>();
                    stagesByTaskId[stage.TaskId] = stages;
                }

                stages.Add(stage);
            }

            foreach (var stages in stagesByTaskId.Values)
            {
                stages.Sort((left, right) => left.StageIndex.CompareTo(right.StageIndex));
            }
        }

        private void ActivateConfiguredTasksForCurrentRound()
        {
            var currentRound = runtimeDataService.Data.CurrentRound;
            foreach (var definition in definitions)
            {
                if (definition.StartRound <= 0 || definition.StartRound > currentRound)
                {
                    continue;
                }

                var state = runtimeDataService.Data.GetOrCreateTask(definition.TaskId);
                state.Activate(currentRound);
            }
        }

        private void ProcessTaskStages(RuntimeTaskState state)
        {
            if (!stagesByTaskId.TryGetValue(state.TaskId, out var stages))
            {
                return;
            }

            var relativeRound = runtimeDataService.Data.CurrentRound - state.ActivatedRound;
            foreach (var stage in stages)
            {
                if (relativeRound == stage.StartOffsetRound && !state.HasProcessedStageStart(stage.TaskStageId))
                {
                    ProcessStageStart(state, stage);
                }

                if (relativeRound == stage.EndOffsetRound && !state.HasProcessedStageEnd(stage.TaskStageId))
                {
                    ProcessStageEnd(state, stage);
                }
            }
        }

        private void ProcessStageStart(RuntimeTaskState state, TaskStageDefinition stage)
        {
            QueueStory(stage.StartStoryId, state.TaskId, stage.TaskStageId, RuntimeStoryQueueTiming.StageStart);
            QueueStory(stage.BeforeDocumentStoryId, state.TaskId, stage.TaskStageId, RuntimeStoryQueueTiming.BeforeDocument);
            GrantLetter(stage.StartLetterId);
            QueueLinkedDocuments(state, stage);
            state.MarkStageStartProcessed(stage.TaskStageId);
        }

        private void ProcessStageEnd(RuntimeTaskState state, TaskStageDefinition stage)
        {
            QueueStory(stage.EndStoryId, state.TaskId, stage.TaskStageId, RuntimeStoryQueueTiming.StageEnd);
            GrantLetter(stage.EndLetterId);
            state.MarkStageEndProcessed(stage.TaskStageId);
        }

        private void QueueStory(
            string storyId,
            string taskId,
            string taskStageId,
            RuntimeStoryQueueTiming timing)
        {
            if (!string.IsNullOrEmpty(storyId))
            {
                runtimeDataService.Data.QueueStory(storyId, taskId, taskStageId, timing);
            }
        }

        private void GrantLetter(string letterId)
        {
            if (!string.IsNullOrEmpty(letterId))
            {
                runtimeDataService.ReceiveLetter(letterId);
            }
        }

        private void QueueLinkedDocuments(RuntimeTaskState state, TaskStageDefinition stage)
        {
            foreach (var documentId in stage.LinkedDocumentIds)
            {
                runtimeDataService.Data.QueueDocument(
                    documentId,
                    state.TaskId,
                    stage.TaskStageId,
                    stage.BeforeDocumentCharacterId);
            }
        }

        private void EvaluateTaskResult(RuntimeTaskState state)
        {
            if (!TryGetDefinition(state.TaskId, out var definition) || state.Status != TaskRuntimeStatus.Active)
            {
                return;
            }

            var currentRound = runtimeDataService.Data.CurrentRound;
            if (definition.SuccessScore > 0 && state.Score >= definition.SuccessScore)
            {
                state.Complete(currentRound);
                return;
            }

            if (definition.EndRound > 0 && currentRound >= definition.EndRound)
            {
                if (definition.FailScore != 0 && state.Score <= definition.FailScore)
                {
                    state.Fail(currentRound);
                    return;
                }

                if (definition.SuccessScore > 0)
                {
                    state.Fail(currentRound);
                }
            }
        }

        private bool TryGetUsableTask(string taskId, out TaskDefinition definition)
        {
            definition = null;
            if (runtimeDataService == null)
            {
                Debug.LogWarning("TaskService missing RuntimeDataService.", this);
                return false;
            }

            if (TryGetDefinition(taskId, out definition))
            {
                return true;
            }

            Debug.LogWarning($"TaskId {taskId} is not configured in TaskConfig.", this);
            return false;
        }

        private void NotifyTasksChanged()
        {
            TasksChanged?.Invoke();
        }
    }
}
