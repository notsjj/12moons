using TwelveMoons.City;
using TwelveMoons.Core;
using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.UI
{
    public sealed class BaseSceneUIContext : MonoBehaviour
    {
        [Header("核心入口：场景总入口")]
        [Tooltip("场景总入口；用于桌面与城区等已有场景根节点切换。为空时启动时自动查找。")]
        [SerializeField] private GameEntry gameEntry;

        [Header("运行时数据：全局状态")]
        [Tooltip("运行时数据服务；用于读取当前回合、任务、信件、公文队列、后续公文等状态。为空时启动时自动查找。")]
        [SerializeField] private RuntimeDataService runtimeDataService;

        [Header("背包服务：资源和道具")]
        [Tooltip("背包服务；用于物品栏、公文提交和道具数量刷新。为空时启动时自动查找。")]
        [SerializeField] private InventoryService inventoryService;

        [Header("阵营服务：质疑度")]
        [Tooltip("阵营服务；用于质疑度栏刷新、公文选项影响和阵营反馈。为空时启动时自动查找。")]
        [SerializeField] private FactionService factionService;

        [Header("回合服务：回合推进")]
        [Tooltip("回合服务；用于回合栏刷新、灾难阶段显示和进入下一回合。为空时启动时自动查找。")]
        [SerializeField] private RoundService roundService;

        [Header("任务服务：任务与阶段")]
        [Tooltip("任务服务；用于任务栏刷新、任务阶段和任务分数变化。为空时启动时自动查找。")]
        [SerializeField] private TaskService taskService;

        [Header("剧情服务：剧情播放")]
        [Tooltip("剧情服务；用于剧情 Overlay 播放、队列推进和剧情结束触发。为空时启动时自动查找。")]
        [SerializeField] private StoryService storyService;

        [Header("信件服务：信件区域")]
        [Tooltip("信件服务；用于信件列表、信件阅读器和新信件接收。为空时启动时自动查找。")]
        [SerializeField] private LetterService letterService;

        [Header("公文服务：公文流程")]
        [Tooltip("公文服务；用于公文队列生成、公文选项结算和后续公文记录。为空时启动时自动查找。")]
        [SerializeField] private DocumentService documentService;

        [Header("城区摄像机：观察位置")]
        [Tooltip("城区摄像机控制器；用于城区按钮切换观察位置，不刷新城区数据。为空时启动时自动查找。")]
        [SerializeField] private CityCameraController cityCameraController;

        public GameEntry GameEntry => gameEntry;
        public RuntimeDataService RuntimeDataService => runtimeDataService;
        public InventoryService InventoryService => inventoryService;
        public FactionService FactionService => factionService;
        public RoundService RoundService => roundService;
        public TaskService TaskService => taskService;
        public StoryService StoryService => storyService;
        public LetterService LetterService => letterService;
        public DocumentService DocumentService => documentService;
        public CityCameraController CityCameraController => cityCameraController;

        public bool ResolveMissingReferences()
        {
            gameEntry = Resolve(gameEntry, "场景总入口 GameEntry");
            runtimeDataService = Resolve(runtimeDataService, "运行时数据服务 RuntimeDataService");
            inventoryService = Resolve(inventoryService, "背包服务 InventoryService");
            factionService = Resolve(factionService, "阵营服务 FactionService");
            roundService = Resolve(roundService, "回合服务 RoundService");
            taskService = Resolve(taskService, "任务服务 TaskService");
            storyService = Resolve(storyService, "剧情服务 StoryService");
            letterService = Resolve(letterService, "信件服务 LetterService");
            documentService = Resolve(documentService, "公文服务 DocumentService");
            cityCameraController = Resolve(cityCameraController, "城区摄像机控制器 CityCameraController");

            return gameEntry != null
                && runtimeDataService != null
                && inventoryService != null
                && factionService != null
                && roundService != null
                && taskService != null
                && storyService != null
                && letterService != null
                && documentService != null
                && cityCameraController != null;
        }

        private static T Resolve<T>(T currentValue, string displayName) where T : Object
        {
            if (currentValue != null)
            {
                return currentValue;
            }

            var resolved = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (resolved == null)
            {
                Debug.LogError($"BaseScene UI 上下文缺少必要引用：{displayName}");
            }

            return resolved;
        }
    }
}
