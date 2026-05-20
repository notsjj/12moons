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
