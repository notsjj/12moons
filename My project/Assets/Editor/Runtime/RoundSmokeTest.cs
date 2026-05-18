using System.IO;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class RoundSmokeTest
    {
        private const string DemoConfigDirectory = "Assets/StreamingAssets/Configs/Demo";

        [MenuItem("Twelve Moons/Tests/Run Round Smoke Test")]
        public static void Run()
        {
            var providerRoot = Path.GetFullPath(DemoConfigDirectory);
            var csvProvider = new CsvConfigProvider(providerRoot);
            var jsonProvider = new JsonConfigProvider(providerRoot);

            var disasterTable = jsonProvider.LoadTable("DisasterConfig");
            var stageTable = csvProvider.LoadTable("DisasterStageConfig");

            if (!disasterTable.TryFindById("DisasterId", "disaster_flood_01", out var disasterRow))
            {
                throw new InvalidDataException("DisasterConfig missing disaster_flood_01 row.");
            }

            var data = new GameRuntimeData();
            data.Reset("disaster_flood_01", disasterRow.GetInt("TotalRound"));

            var resolver = new DisasterStageResolver(stageTable);
            var firstStage = resolver.Resolve(data.DisasterId, data.CurrentRound);
            data.SetCurrentRound(7);
            var middleStage = resolver.Resolve(data.DisasterId, data.CurrentRound);
            data.SetCurrentRound(data.TotalRound);
            var finalStage = resolver.Resolve(data.DisasterId, data.CurrentRound);
            var blocked = data.TryAdvanceRound();

            if (data.TotalRound != 18 ||
                firstStage == null ||
                firstStage.StageId != "stage_warning" ||
                middleStage == null ||
                middleStage.StageId != "stage_peak" ||
                finalStage == null ||
                finalStage.StageId != "stage_aftermath" ||
                blocked)
            {
                throw new InvalidDataException("Round smoke test failed.");
            }

            Debug.Log("Round smoke test passed. TotalRound=18; rounds 1, 7, and 18 resolve to configured disaster stages; advancing after final round is blocked.");
        }
    }
}
