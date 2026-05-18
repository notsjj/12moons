using System.IO;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class RuntimeDataSmokeTest
    {
        private const string DemoConfigDirectory = "Assets/StreamingAssets/Configs/Demo";

        [MenuItem("Twelve Moons/Tests/Run Runtime Data Smoke Test")]
        public static void Run()
        {
            var providerRoot = Path.GetFullPath(DemoConfigDirectory);
            var csvProvider = new CsvConfigProvider(providerRoot);
            var jsonProvider = new JsonConfigProvider(providerRoot);

            var itemTable = csvProvider.LoadTable("ItemConfig");
            var disasterTable = jsonProvider.LoadTable("DisasterConfig");

            if (!disasterTable.TryFindById("DisasterId", "disaster_flood_01", out var disasterRow))
            {
                throw new InvalidDataException("DisasterConfig missing disaster_flood_01 row.");
            }

            var data = new GameRuntimeData();
            data.Reset("disaster_flood_01", disasterRow.GetInt("TotalRound"));

            foreach (var row in itemTable.Rows)
            {
                data.GetOrCreateItem(row.GetString("ItemId"));
            }

            data.GetOrCreateItem("item_money").AddCount(25);
            data.GetOrCreateTask("task_demo_drainage").Activate(data.CurrentRound);
            data.GetOrCreateBuilding("building_demo_pump").Unlock();
            data.AddLetter("letter_demo_lower_city").MarkRead();

            if (data.TotalRound != 18 ||
                data.Items.Count != 5 ||
                data.GetOrCreateItem("item_money").Count != 25 ||
                data.Tasks[0].Status != TaskRuntimeStatus.Active ||
                !data.Buildings[0].IsUnlocked ||
                !data.Letters[0].IsRead)
            {
                throw new InvalidDataException("Runtime data smoke test failed.");
            }

            Debug.Log(
                $"Runtime data smoke test passed. Disaster={data.DisasterId}, Round={data.CurrentRound}/{data.TotalRound}, Items={data.Items.Count}, Tasks={data.Tasks.Count}, Buildings={data.Buildings.Count}, Letters={data.Letters.Count}.");
        }
    }
}
