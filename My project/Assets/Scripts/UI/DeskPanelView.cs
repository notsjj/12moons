using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class DeskPanelView : MonoBehaviour
    {
        [Header("桌面面板引用：共享或桌面专用 UI")]
        [Tooltip("共享任务栏；优先使用手动绑定，缺失时会查找场景中的 TaskPanel。")]
        [SerializeField] private TaskPanelView taskPanel;

        [Tooltip("桌面质疑栏；用于显示阵营质疑度和桌面流程反馈。")]
        [SerializeField] private SuspicionPanelView suspicionPanel;

        [Tooltip("信件区域；用于显示当前可阅读信件。")]
        [SerializeField] private LetterAreaView letterArea;

        [Tooltip("物品栏；用于显示金币、建材、食物和其它道具。")]
        [SerializeField] private InventoryPanelView inventoryPanel;

        [Tooltip("共享角色槽；用于公文前角色和新公文提出者滑入。")]
        [SerializeField] private SharedActorSlotView sharedActorSlot;

        [Tooltip("公文弹窗；用于显示当前公文和两个选项。")]
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

            if (taskPanel == null)
            {
                taskPanel = FindScenePanel<TaskPanelView>("TaskPanel");
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

        private static T FindScenePanel<T>(string objectName) where T : Component
        {
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in transforms)
            {
                if (candidate == null || candidate.name != objectName)
                {
                    continue;
                }

                var panel = candidate.GetComponent<T>();
                if (panel != null)
                {
                    return panel;
                }
            }

            return null;
        }
    }
}
