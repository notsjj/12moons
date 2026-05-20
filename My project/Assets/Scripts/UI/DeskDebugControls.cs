using TMPro;
using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class DeskDebugControls : MonoBehaviour
    {
        [Header("Services")]
        [SerializeField] private InventoryService inventoryService;
        [SerializeField] private TaskService taskService;
        [SerializeField] private RoundService roundService;
        [SerializeField] private RuntimeDataService runtimeDataService;
        [SerializeField] private FactionService factionService;
        [SerializeField] private LetterService letterService;

        [Header("Views")]
        [SerializeField] private DeskPanelView deskPanelView;
        [SerializeField] private SharedActorSlotView sharedActorSlot;
        [SerializeField] private DocumentPopupPanelView documentPopupPanel;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Demo Values")]
        [SerializeField] private string demoTaskId = "task_demo_relief_01";
        [SerializeField] private string lowTestFactionId = "civilian";
        [SerializeField] private string highTestFactionId = "noble";
        [SerializeField] private string demoLetterIdA = "letter_relief_start";
        [SerializeField] private string demoLetterIdB = "letter_relief_prepare_end";
        [SerializeField] private string demoLetterIdC = "letter_relief_deliver_start";
        [SerializeField] private int moneyDelta = 10;
        [SerializeField] private int materialDelta = 5;
        [SerializeField] private int foodDelta = 3;
        [SerializeField] private int taskScoreStep = 1;
        [SerializeField] private int lowSuspicionDelta = -35;
        [SerializeField] private int highSuspicionDelta = 45;

        private void Awake()
        {
            ResolveMissingReferences();
        }

        public void AddMoney()
        {
            inventoryService?.AddMoney(moneyDelta);
            SetFeedback($"Money +{moneyDelta}");
        }

        public void RemoveMoney()
        {
            inventoryService?.RemoveMoney(moneyDelta);
            SetFeedback($"Money -{moneyDelta}");
        }

        public void AddMaterial()
        {
            inventoryService?.AddMaterial(materialDelta);
            SetFeedback($"Material +{materialDelta}");
        }

        public void RemoveMaterial()
        {
            inventoryService?.RemoveMaterial(materialDelta);
            SetFeedback($"Material -{materialDelta}");
        }

        public void AddFood()
        {
            inventoryService?.AddFood(foodDelta);
            SetFeedback($"Food +{foodDelta}");
        }

        public void RemoveFood()
        {
            inventoryService?.RemoveFood(foodDelta);
            SetFeedback($"Food -{foodDelta}");
        }

        public void ActivateDemoTask()
        {
            var state = taskService != null ? taskService.ActivateTask(demoTaskId) : null;
            SetFeedback(state != null ? $"Activated {demoTaskId}" : "Task activation failed.");
        }

        public void AddDemoTaskScore()
        {
            var state = taskService != null ? taskService.AddTaskScore(demoTaskId, taskScoreStep) : null;
            SetFeedback(state != null ? $"{demoTaskId} score = {state.Score}" : "Task score failed.");
        }

        public void NextRound()
        {
            var advanced = roundService != null && roundService.NextRound();
            var round = runtimeDataService != null ? runtimeDataService.Data.CurrentRound : 0;
            SetFeedback(advanced ? $"Round {round}" : "Cannot advance round.");
        }

        public void LowerSuspicion()
        {
            var state = factionService != null ? factionService.ChangeSuspicion(lowTestFactionId, lowSuspicionDelta) : null;
            SetFeedback(state != null ? $"{lowTestFactionId}: {state.Suspicion}" : "Lower suspicion failed.");
        }

        public void RaiseSuspicion()
        {
            var state = factionService != null ? factionService.ChangeSuspicion(highTestFactionId, highSuspicionDelta) : null;
            SetFeedback(state != null ? $"{highTestFactionId}: {state.Suspicion}" : "Raise suspicion failed.");
        }

        public void ReceiveLetterA()
        {
            ReceiveLetter(demoLetterIdA);
        }

        public void ReceiveLetterB()
        {
            ReceiveLetter(demoLetterIdB);
        }

        public void ReceiveLetterC()
        {
            ReceiveLetter(demoLetterIdC);
        }

        public void ShowTestActor()
        {
            sharedActorSlot?.ShowTestActor();
            SetFeedback("SharedActorSlot shown.");
        }

        public void HideActor()
        {
            sharedActorSlot?.Hide();
            SetFeedback("SharedActorSlot hidden.");
        }

        public void ShowDocumentPreview()
        {
            documentPopupPanel?.ShowPreview();
            SetFeedback("DocumentPopupPanel shown.");
        }

        public void HideDocumentPreview()
        {
            documentPopupPanel?.Hide();
            SetFeedback("DocumentPopupPanel hidden.");
        }

        public void RefreshDesk()
        {
            inventoryService?.Refresh();
            taskService?.Refresh();
            factionService?.Refresh();
            letterService?.Refresh();
            roundService?.Refresh();
            deskPanelView?.RefreshAll();
            SetFeedback("Desk refreshed.");
        }

        private void ReceiveLetter(string letterId)
        {
            var state = letterService != null ? letterService.ReceiveLetter(letterId) : null;
            SetFeedback(state != null ? $"Received {letterId}" : $"Receive failed: {letterId}");
        }

        private void ResolveMissingReferences()
        {
            if (inventoryService == null)
            {
                inventoryService = FindFirstObjectByType<InventoryService>();
            }

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

            if (factionService == null)
            {
                factionService = FindFirstObjectByType<FactionService>();
            }

            if (letterService == null)
            {
                letterService = FindFirstObjectByType<LetterService>();
            }

            if (deskPanelView == null)
            {
                deskPanelView = FindFirstObjectByType<DeskPanelView>();
            }

            if (sharedActorSlot == null)
            {
                sharedActorSlot = FindFirstObjectByType<SharedActorSlotView>(FindObjectsInactive.Include);
            }

            if (documentPopupPanel == null)
            {
                documentPopupPanel = FindFirstObjectByType<DocumentPopupPanelView>(FindObjectsInactive.Include);
            }
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
