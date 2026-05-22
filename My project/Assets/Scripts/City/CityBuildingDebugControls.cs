using System.Linq;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using UnityEngine;

namespace TwelveMoons.City
{
    public sealed class CityBuildingDebugControls : MonoBehaviour
    {
        [Header("调试目标：阶段15建筑解锁链路")]
        [Tooltip("用于调试的公文 ID；默认指向阶段15赈济仓修建令。")]
        [SerializeField] private string debugDocumentId = "document_market_notice";
        [Tooltip("用于调试的建筑 ID；默认指向赈济仓建筑。")]
        [SerializeField] private string debugBuildingId = "building_relief_depot";

        [Header("依赖引用：留空时自动查找")]
        [Tooltip("配置管理器；用于检查 DocumentConfig 和 CityBuildingConfig 当前是否读到正确字段。")]
        [SerializeField] private ConfigManager configManager;
        [Tooltip("运行时数据服务；用于检查公文结算后 RuntimeBuildingState 是否已解锁。")]
        [SerializeField] private RuntimeDataService runtimeDataService;
        [Tooltip("公文服务；用于排队和直接结算测试公文。")]
        [SerializeField] private DocumentService documentService;
        [Tooltip("建筑服务；用于刷新建筑配置、解锁状态和点击状态。")]
        [SerializeField] private CityBuildingService buildingService;
        [Tooltip("建筑注册器；用于刷新场景中 CityBuildingView 的显示状态。")]
        [SerializeField] private CityBuildingRegistry buildingRegistry;

        [Header("只读快照：配置、公文、运行时、模型显示")]
        [Tooltip("DocumentConfig 中测试公文 A 选项的解锁字段和资源门槛。")]
        [SerializeField] private string inspectorDocumentConfigSnapshot;
        [Tooltip("DocumentService 当前内存定义是否已经读到测试公文。")]
        [SerializeField] private string inspectorDocumentServiceSnapshot;
        [Tooltip("当前回合公文队列里是否存在测试公文。")]
        [SerializeField] private string inspectorDocumentQueueSnapshot;
        [Tooltip("RuntimeDataService 中目标建筑的解锁状态。")]
        [SerializeField] private string inspectorRuntimeBuildingSnapshot;
        [Tooltip("CityBuildingView 匹配、Renderer 和 Collider 状态。")]
        [SerializeField] private string inspectorSceneViewSnapshot;
        [Tooltip("最近一次调试命令的结果。")]
        [SerializeField] private string inspectorLastDebugResult;

        [ContextMenu("阶段15/刷新建筑解锁调试快照")]
        public void RefreshDebugSnapshot()
        {
            ResolveDependencies();
            RefreshDocumentConfigSnapshot();
            RefreshDocumentServiceSnapshot();
            RefreshDocumentQueueSnapshot();
            RefreshRuntimeBuildingSnapshot();
            RefreshSceneViewSnapshot();
        }

        [ContextMenu("阶段15/强制重载配置并刷新服务")]
        public void ForceReloadConfigsAndRefreshServices()
        {
            ResolveDependencies();
            if (configManager == null)
            {
                SetLastResult("缺少 ConfigManager，无法强制重载配置。");
                return;
            }

            var loaded = 0;
            loaded += TryLoadTable("DocumentConfig") ? 1 : 0;
            loaded += TryLoadTable("CityBuildingConfig") ? 1 : 0;
            documentService?.Refresh();
            buildingService?.Refresh();
            buildingRegistry?.RefreshAndBind();
            SetLastResult($"已强制重载配置并刷新服务。成功重载表数量={loaded}/2。");
        }

        [ContextMenu("阶段15/排队测试公文")]
        public void QueueDebugDocument()
        {
            ResolveDependencies();
            ForceReloadConfigsWithoutResult();
            if (documentService == null)
            {
                SetLastResult("缺少 DocumentService，无法排队测试公文。");
                return;
            }

            var entry = documentService.QueueDocument(debugDocumentId);
            SetLastResult(entry != null
                ? $"已排队测试公文：{debugDocumentId}。"
                : $"排队测试公文失败：{debugDocumentId}。");
        }

        [ContextMenu("阶段15/直接结算测试公文A选项")]
        public void ResolveDebugDocumentOptionA()
        {
            ResolveDependencies();
            ForceReloadConfigsWithoutResult();
            if (documentService == null || runtimeDataService == null)
            {
                SetLastResult("缺少 DocumentService 或 RuntimeDataService，无法直接结算公文。");
                return;
            }

            var entry = runtimeDataService.Data.DocumentQueue
                .FirstOrDefault(candidate => candidate.DocumentId == debugDocumentId);
            if (entry == null)
            {
                entry = documentService.QueueDocument(debugDocumentId);
            }

            var result = documentService.ResolveDocument(entry, DocumentOptionType.A);
            buildingService?.Refresh();
            buildingRegistry?.RefreshBoundViewStates();
            SetLastResult(result.Success
                ? $"直接结算 A 成功：{debugDocumentId}，应解锁 {debugBuildingId}。"
                : $"直接结算 A 失败：{result.Message}");
        }

        [ContextMenu("阶段15/强制解锁目标建筑")]
        public void ForceUnlockDebugBuilding()
        {
            ResolveDependencies();
            if (runtimeDataService == null)
            {
                SetLastResult("缺少 RuntimeDataService，无法强制解锁建筑。");
                return;
            }

            runtimeDataService.UnlockBuilding(debugBuildingId);
            buildingService?.Refresh();
            buildingRegistry?.RefreshBoundViewStates();
            SetLastResult($"已强制解锁建筑：{debugBuildingId}。如果模型仍不显示，问题在 CityBuildingView/Renderer/模型层级。");
        }

        [ContextMenu("阶段15/刷新建筑显示")]
        public void RefreshBuildingViews()
        {
            ResolveDependencies();
            buildingService?.Refresh();
            buildingRegistry?.RefreshAndBind();
            SetLastResult("已刷新 CityBuildingService 与 CityBuildingRegistry。");
        }

        private void ResolveDependencies()
        {
            if (configManager == null)
            {
                configManager = FindFirstObjectByType<ConfigManager>(FindObjectsInactive.Include);
            }

            if (runtimeDataService == null)
            {
                runtimeDataService = FindFirstObjectByType<RuntimeDataService>(FindObjectsInactive.Include);
            }

            if (documentService == null)
            {
                documentService = FindFirstObjectByType<DocumentService>(FindObjectsInactive.Include);
            }

            if (buildingService == null)
            {
                buildingService = FindFirstObjectByType<CityBuildingService>(FindObjectsInactive.Include);
            }

            if (buildingRegistry == null)
            {
                buildingRegistry = FindFirstObjectByType<CityBuildingRegistry>(FindObjectsInactive.Include);
            }
        }

        private void RefreshDocumentConfigSnapshot()
        {
            if (configManager == null ||
                !configManager.TryFindRow("DocumentConfig", "DocumentId", debugDocumentId, out var row))
            {
                inspectorDocumentConfigSnapshot = $"ConfigManager 未读取到 DocumentConfig.{debugDocumentId}";
                return;
            }

            inspectorDocumentConfigSnapshot =
                $"公文={row.GetString("Title")}, A={row.GetString("OptionA_Text")}, " +
                $"A解锁={row.GetString("OptionA_UnlockBuildingId")}, " +
                $"金币={row.GetInt("OptionA_MoneyChange")}, 建材={row.GetInt("OptionA_MaterialChange")}, " +
                $"食物={row.GetInt("OptionA_FoodChange")}, 需求道具={row.GetString("OptionA_RequiredItemId")}";
        }

        private void RefreshDocumentServiceSnapshot()
        {
            if (documentService == null)
            {
                inspectorDocumentServiceSnapshot = "缺少 DocumentService";
                return;
            }

            if (!documentService.TryGetDefinition(debugDocumentId, out var definition))
            {
                inspectorDocumentServiceSnapshot =
                    $"DocumentService 内存中没有 {debugDocumentId}；请执行“阶段15/强制重载配置并刷新服务”。";
                return;
            }

            inspectorDocumentServiceSnapshot =
                $"DocumentService 已读取：标题={definition.Title}, A={definition.OptionA.Text}, A解锁={definition.OptionA.UnlockBuildingId}";
        }

        private bool TryLoadTable(string tableName)
        {
            try
            {
                configManager.LoadTable(tableName);
                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"阶段15调试重载配置失败：{tableName}，原因：{exception.Message}", this);
                return false;
            }
        }

        private void ForceReloadConfigsWithoutResult()
        {
            if (configManager == null)
            {
                return;
            }

            TryLoadTable("DocumentConfig");
            TryLoadTable("CityBuildingConfig");
            documentService?.Refresh();
            buildingService?.Refresh();
            buildingRegistry?.RefreshAndBind();
        }

        private void RefreshDocumentQueueSnapshot()
        {
            if (runtimeDataService == null)
            {
                inspectorDocumentQueueSnapshot = "缺少 RuntimeDataService";
                return;
            }

            var entries = runtimeDataService.Data.DocumentQueue
                .Where(candidate => candidate.DocumentId == debugDocumentId)
                .Select(candidate => $"QueuedRound={candidate.QueuedRound}, Task={candidate.TaskId}, Stage={candidate.TaskStageId}");
            inspectorDocumentQueueSnapshot = string.Join(" | ", entries);
            if (string.IsNullOrEmpty(inspectorDocumentQueueSnapshot))
            {
                inspectorDocumentQueueSnapshot = "当前公文队列中没有测试公文。";
            }
        }

        private void RefreshRuntimeBuildingSnapshot()
        {
            if (runtimeDataService == null)
            {
                inspectorRuntimeBuildingSnapshot = "缺少 RuntimeDataService";
                return;
            }

            var state = runtimeDataService.Data.Buildings.FirstOrDefault(candidate => candidate.BuildingId == debugBuildingId);
            inspectorRuntimeBuildingSnapshot = state == null
                ? $"运行时尚未创建建筑状态：{debugBuildingId}"
                : $"建筑={state.BuildingId}, 已解锁={state.IsUnlocked}, 上次领取回合={state.LastCollectedRound}";
        }

        private void RefreshSceneViewSnapshot()
        {
            var views = FindObjectsByType<CityBuildingView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(view => view.BuildingId == debugBuildingId)
                .ToList();
            if (views.Count == 0)
            {
                inspectorSceneViewSnapshot = $"场景中找不到 CityBuildingView：{debugBuildingId}";
                return;
            }

            inspectorSceneViewSnapshot = string.Join(
                " | ",
                views.Select(view =>
                {
                    var renderers = view.GetComponentsInChildren<Renderer>(true);
                    var enabledRenderers = renderers.Count(renderer => renderer != null && renderer.enabled);
                    var colliders = view.GetComponentsInChildren<Collider>(true);
                    var enabledColliders = colliders.Count(collider => collider != null && collider.enabled);
                    return $"{view.name}: Active={view.gameObject.activeInHierarchy}, 匹配={view.IsMatched}, Renderer={enabledRenderers}/{renderers.Length}, Collider={enabledColliders}/{colliders.Length}";
                }));
        }

        private void SetLastResult(string message)
        {
            inspectorLastDebugResult = message;
            Debug.Log(message, this);
            RefreshDebugSnapshot();
        }
    }
}
