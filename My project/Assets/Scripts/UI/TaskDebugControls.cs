using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class TaskDebugControls : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private TaskService taskService;
        [SerializeField] private RoundService roundService;
        [SerializeField] private RuntimeDataService runtimeDataService;

        [Header("Demo")]
        [SerializeField] private string demoTaskId = "task_demo_relief_01";
        [SerializeField]
        private string[] demoTaskIds =
        {
            "task_demo_relief_01",
            "task_punish_noble_01",
            "task_punish_academy_01",
            "task_punish_church_01",
            "task_punish_civilian_01",
            "task_scroll_test_01",
            "task_scroll_test_02",
            "task_scroll_test_03",
            "task_scroll_test_04",
            "task_scroll_test_05",
            "task_scroll_test_06",
            "task_scroll_test_07",
            "task_scroll_test_08",
            "task_scroll_test_09",
            "task_scroll_test_10"
        };

        [SerializeField] private int scoreStep = 1;
        [SerializeField] private TMP_Text feedbackText;

        private void Awake()
        {
            if (taskService == null)
            {
                taskService = FindFirstObjectByType<TaskService>();
            }

            if (roundService == null)
            {
                roundService = FindFirstObjectByType<RoundService>();
            }

            if (runtimeDataService == null)
            {
                runtimeDataService = FindFirstObjectByType<RuntimeDataService>();
            }
        }

        public void ActivateDemoTask()
        {
            var state = taskService != null ? taskService.ActivateTask(demoTaskId) : null;
            SetFeedback(state != null ? $"Activated {demoTaskId}." : "Task activation failed.");
        }

        public void ActivateDemoTasks()
        {
            if (taskService == null || demoTaskIds == null || demoTaskIds.Length == 0)
            {
                SetFeedback("Multi task activation failed.");
                return;
            }

            var activatedCount = 0;
            foreach (var taskId in demoTaskIds)
            {
                if (string.IsNullOrEmpty(taskId))
                {
                    continue;
                }

                if (taskService.ActivateTask(taskId) != null)
                {
                    activatedCount++;
                }
            }

            SetFeedback($"Activated {activatedCount} demo tasks.");
        }

        public void AddDemoTaskScore()
        {
            var state = taskService != null ? taskService.AddTaskScore(demoTaskId, scoreStep) : null;
            SetFeedback(state != null ? $"{demoTaskId} score = {state.Score}." : "Score change failed.");
        }

        public void NextRound()
        {
            var advanced = roundService != null && roundService.NextRound();
            var round = runtimeDataService != null ? runtimeDataService.Data.CurrentRound : 0;
            SetFeedback(advanced ? $"Advanced to round {round}." : "Cannot advance round.");
        }

        public void RefreshTasks()
        {
            if (taskService != null)
            {
                taskService.Refresh();
            }

            SetFeedback("Task service refreshed.");
        }

        private void SetFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
            }
        }
    }
}
