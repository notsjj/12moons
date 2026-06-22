using System.IO;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class RoundSmokeTest
    {
        private const string PlotConfigDirectory = "Assets/StreamingAssets/Configs/Plot";

        [MenuItem("Twelve Moons/Tests/Run Round Smoke Test")]
        public static void Run()
        {
            var providerRoot = Path.GetFullPath(PlotConfigDirectory);
            var csvProvider = new CsvConfigProvider(providerRoot);
            var jsonProvider = new JsonConfigProvider(providerRoot);

            var disasterTable = jsonProvider.LoadTable("DisasterConfig");
            var stageTable = csvProvider.LoadTable("DisasterStageConfig");

            if (!disasterTable.TryFindById("DisasterId", "DI0001", out var disasterRow))
            {
                throw new InvalidDataException("DisasterConfig 缺少 DI0001 行。");
            }

            var data = new GameRuntimeData();
            data.Reset("DI0001", disasterRow.GetInt("TotalRound"));

            var resolver = new DisasterStageResolver(stageTable);
            var firstStage = resolver.Resolve(data.DisasterId, data.CurrentRound);
            data.SetCurrentRound(3);
            var secondStage = resolver.Resolve(data.DisasterId, data.CurrentRound);
            data.SetCurrentRound(data.TotalRound);
            var finalStage = resolver.Resolve(data.DisasterId, data.CurrentRound);
            var blocked = data.TryAdvanceRound();

            if (data.TotalRound != 18 ||
                firstStage == null ||
                firstStage.StageId != "DS0001" ||
                firstStage.StageName != "晴天" ||
                secondStage == null ||
                secondStage.StageId != "DS0002" ||
                secondStage.StageName != "阴" ||
                finalStage == null ||
                finalStage.StageName != "大雨" ||
                blocked)
            {
                throw new InvalidDataException("Round smoke test failed：DI0001 的回合必须按 DisasterStageConfig 解析阶段名称。");
            }

            Debug.Log("Round smoke test passed. DI0001 resolves configured DisasterStageConfig stage names by current round.");
        }
    }
}
