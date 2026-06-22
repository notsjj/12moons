using System.IO;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class FactionSmokeTest
    {
        [MenuItem("Twelve Moons/Tests/Run Faction Smoke Test")]
        public static void Run()
        {
            var root = new GameObject("FactionSmokeTestRoot");
            try
            {
                var configManager = root.AddComponent<ConfigManager>();
                var runtimeDataService = root.AddComponent<RuntimeDataService>();
                var factionService = root.AddComponent<FactionService>();

                ConfigureConfigManager(configManager);
                ConfigureRuntimeDataService(runtimeDataService, configManager);
                ConfigureFactionService(factionService, configManager, runtimeDataService);

                configManager.BuildDefaultProviders();
                runtimeDataService.CreateNewGame("DI0001");
                factionService.Refresh();

                if (runtimeDataService.Data.Factions.Count != 4)
                {
                    throw new InvalidDataException("FactionConfig should initialize four runtime factions.");
                }

                var lowEvents = 0;
                var highEvents = 0;
                factionService.ThresholdTriggered += result =>
                {
                    if (result.GrantedLowSuspicionLetter)
                    {
                        lowEvents++;
                    }

                    if (result.ActivatedPunishTask)
                    {
                        highEvents++;
                    }
                };

                factionService.ChangeSuspicion("civilian", -35);
                factionService.ChangeSuspicion("noble", 45);
                factionService.ChangeSuspicion("noble", 30);

                var civilian = runtimeDataService.Data.GetOrCreateFaction("civilian", 0);
                var noble = runtimeDataService.Data.GetOrCreateFaction("noble", 0);

                if (civilian.Suspicion != 15 ||
                    noble.Suspicion != 65 ||
                    runtimeDataService.Data.Letters.Count != 1 ||
                    runtimeDataService.Data.Letters[0].LetterId != "letter_civilian_low_01" ||
                    runtimeDataService.Data.Tasks.Count != 1 ||
                    runtimeDataService.Data.Tasks[0].TaskId != "task_punish_noble_01" ||
                    runtimeDataService.Data.Tasks[0].Status != TaskRuntimeStatus.Active ||
                    lowEvents != 1 ||
                    highEvents != 2)
                {
                    throw new InvalidDataException("Faction smoke test failed.");
                }

                Debug.Log("Faction smoke test passed. Four factions loaded; low suspicion granted a letter; high suspicion repeatedly triggered punishment and reduced suspicion.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureConfigManager(ConfigManager configManager)
        {
            var serializedObject = new SerializedObject(configManager);
            serializedObject.FindProperty("relativeConfigDirectory").stringValue = "Configs/Demo";
            serializedObject.FindProperty("loadOnAwake").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRuntimeDataService(RuntimeDataService runtimeDataService, ConfigManager configManager)
        {
            var serializedObject = new SerializedObject(runtimeDataService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("createNewGameOnAwake").boolValue = false;
            serializedObject.FindProperty("initialDisasterId").stringValue = "DI0001";
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureFactionService(
            FactionService factionService,
            ConfigManager configManager,
            RuntimeDataService runtimeDataService)
        {
            var serializedObject = new SerializedObject(factionService);
            serializedObject.FindProperty("configManager").objectReferenceValue = configManager;
            serializedObject.FindProperty("runtimeDataService").objectReferenceValue = runtimeDataService;
            serializedObject.FindProperty("highSuspicionReduceValue").intValue = 30;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
