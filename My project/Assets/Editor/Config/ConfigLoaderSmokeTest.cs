using System.IO;
using TwelveMoons.Core.Config;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Config
{
    public static class ConfigLoaderSmokeTest
    {
        private const string DemoConfigDirectory = "Assets/StreamingAssets/Configs/Demo";

        [MenuItem("Twelve Moons/Tests/Run Config Loader Smoke Test")]
        public static void Run()
        {
            var providerRoot = Path.GetFullPath(DemoConfigDirectory);
            var csvProvider = new CsvConfigProvider(providerRoot);
            var jsonProvider = new JsonConfigProvider(providerRoot);

            var itemTable = csvProvider.LoadTable("ItemConfig");
            var disasterTable = jsonProvider.LoadTable("DisasterConfig");

            if (!itemTable.TryFindById("ItemId", "item_money", out var moneyRow))
            {
                throw new InvalidDataException("ItemConfig missing item_money row.");
            }

            if (!disasterTable.TryFindById("DisasterId", "DI0001", out var disasterRow))
            {
                throw new InvalidDataException("DisasterConfig missing DI0001 row.");
            }

            Debug.Log(
                $"Config loader smoke test passed. Item={moneyRow.GetString("ItemName")}, DisasterRounds={disasterRow.GetInt("TotalRound")}");
        }
    }
}
