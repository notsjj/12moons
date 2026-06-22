using System;
using TwelveMoons.Core.Config;
using UnityEngine;

namespace TwelveMoons.Core.Runtime
{
    public sealed class RoundService : MonoBehaviour
    {
        [Header("依赖服务：配置和运行时")]
        [Tooltip("配置管理器；用于读取 DisasterStageConfig。")]
        [SerializeField] private ConfigManager configManager;
        [Tooltip("运行时数据服务；用于读取和推进当前回合。")]
        [SerializeField] private RuntimeDataService runtimeDataService;

        private DisasterStageResolver disasterStageResolver;

        public event Action RoundChanged;

        public int CurrentRound => runtimeDataService != null ? runtimeDataService.Data.CurrentRound : 0;

        public int TotalRound => runtimeDataService != null ? runtimeDataService.Data.TotalRound : 0;

        public DisasterStageDefinition CurrentDisasterStage =>
            runtimeDataService != null
                ? ResolveDisasterStage(runtimeDataService.Data.CurrentRound)
                : null;

        private void Awake()
        {
            if (configManager == null)
            {
                configManager = FindFirstObjectByType<ConfigManager>();
            }

            if (runtimeDataService == null)
            {
                runtimeDataService = FindFirstObjectByType<RuntimeDataService>();
            }

            LoadDisasterStageConfig();
        }

        private void Start()
        {
            EnsureInitialRuntimeData();
            RoundChanged?.Invoke();
        }

        public void Refresh()
        {
            EnsureInitialRuntimeData();
            LoadDisasterStageConfig();
            RoundChanged?.Invoke();
        }

        public bool NextRound()
        {
            if (runtimeDataService == null)
            {
                Debug.LogWarning("RoundService missing RuntimeDataService.", this);
                return false;
            }

            var advanced = runtimeDataService.Data.TryAdvanceRound();
            RoundChanged?.Invoke();
            return advanced;
        }

        public void RestartInitialDisaster()
        {
            if (runtimeDataService == null)
            {
                Debug.LogWarning("RoundService missing RuntimeDataService.", this);
                return;
            }

            runtimeDataService.CreateNewGameWithInitialDisaster();
            RoundChanged?.Invoke();
        }

        
        public void EnsureInitialRuntimeData()
        {
            if (runtimeDataService == null || runtimeDataService.Data.CurrentRound > 0)
            {
                return;
            }

            runtimeDataService.CreateNewGameWithInitialDisaster();
        }

        public DisasterStageDefinition ResolveDisasterStage(int round)
        {
            if (runtimeDataService == null)
            {
                return null;
            }

            EnsureInitialRuntimeData();

            if (disasterStageResolver == null)
            {
                LoadDisasterStageConfig();
            }

            return disasterStageResolver?.Resolve(runtimeDataService.Data.DisasterId, round);
        }

        private void LoadDisasterStageConfig()
        {
            if (configManager != null && configManager.TryGetTable("DisasterStageConfig", out var table))
            {
                disasterStageResolver = new DisasterStageResolver(table);
                return;
            }

            disasterStageResolver = new DisasterStageResolver(null);
            Debug.LogWarning("RoundService cannot load DisasterStageConfig.", this);
        }
    }
}
