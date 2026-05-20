using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class DeskPanelView : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private TaskPanelView taskPanel;
        [SerializeField] private SuspicionPanelView suspicionPanel;
        [SerializeField] private LetterAreaView letterArea;
        [SerializeField] private InventoryPanelView inventoryPanel;
        [SerializeField] private SharedActorSlotView sharedActorSlot;
        [SerializeField] private DocumentPopupPanelView documentPopupPanel;

        public TaskPanelView TaskPanel => taskPanel;
        public SuspicionPanelView SuspicionPanel => suspicionPanel;
        public LetterAreaView LetterArea => letterArea;
        public InventoryPanelView InventoryPanel => inventoryPanel;
        public SharedActorSlotView SharedActorSlot => sharedActorSlot;
        public DocumentPopupPanelView DocumentPopupPanel => documentPopupPanel;

        private void Awake()
        {
            ResolveMissingReferences();
        }

        private void OnEnable()
        {
            RefreshAll();
        }

        public void RefreshAll()
        {
            taskPanel?.Refresh();
            suspicionPanel?.Refresh();
            letterArea?.Refresh();
            inventoryPanel?.Refresh();
        }

        public void ClearActorSlot()
        {
            sharedActorSlot?.Hide();
        }

        public void HideDocumentPopup()
        {
            documentPopupPanel?.Hide();
        }

        private void ResolveMissingReferences()
        {
            if (taskPanel == null)
            {
                taskPanel = GetComponentInChildren<TaskPanelView>(true);
            }

            if (suspicionPanel == null)
            {
                suspicionPanel = GetComponentInChildren<SuspicionPanelView>(true);
            }

            if (letterArea == null)
            {
                letterArea = GetComponentInChildren<LetterAreaView>(true);
            }

            if (inventoryPanel == null)
            {
                inventoryPanel = GetComponentInChildren<InventoryPanelView>(true);
            }

            if (sharedActorSlot == null)
            {
                sharedActorSlot = GetComponentInChildren<SharedActorSlotView>(true);
            }

            if (documentPopupPanel == null)
            {
                documentPopupPanel = GetComponentInChildren<DocumentPopupPanelView>(true);
            }
        }
    }
}
