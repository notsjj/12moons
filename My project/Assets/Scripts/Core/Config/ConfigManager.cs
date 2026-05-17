using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TwelveMoons.Core.Config
{
    public sealed class ConfigManager : MonoBehaviour
    {
        [Header("Config Source")]
        [SerializeField] private string relativeConfigDirectory = "Configs";
        [SerializeField] private bool loadOnAwake = true;
        [SerializeField] private string[] preloadTables = Array.Empty<string>();

        private readonly Dictionary<string, ConfigTable> tables = new Dictionary<string, ConfigTable>(StringComparer.Ordinal);
        private readonly List<IConfigProvider> providers = new List<IConfigProvider>();

        public IReadOnlyDictionary<string, ConfigTable> Tables => tables;

        private void Awake()
        {
            BuildDefaultProviders();

            if (loadOnAwake)
            {
                LoadTables(preloadTables);
            }
        }

        [ContextMenu("Load Preload Tables")]
        public void LoadPreloadTables()
        {
            BuildDefaultProviders();
            LoadTables(preloadTables);
        }

        public void BuildDefaultProviders()
        {
            providers.Clear();

            var configRoot = Path.Combine(Application.streamingAssetsPath, relativeConfigDirectory);
            providers.Add(new CsvConfigProvider(configRoot));
            providers.Add(new JsonConfigProvider(configRoot));
        }

        public void LoadTables(IEnumerable<string> tableNames)
        {
            foreach (var tableName in tableNames)
            {
                LoadTable(tableName);
            }
        }

        public ConfigTable LoadTable(string tableName)
        {
            if (providers.Count == 0)
            {
                BuildDefaultProviders();
            }

            foreach (var provider in providers)
            {
                if (!provider.CanLoad(tableName))
                {
                    continue;
                }

                var table = provider.LoadTable(tableName);
                tables[tableName] = table;
                Debug.Log($"Loaded config table {tableName}: {table.Rows.Count} rows.", this);
                return table;
            }

            throw new FileNotFoundException($"No config provider can load table: {tableName}");
        }

        public bool TryGetTable(string tableName, out ConfigTable table)
        {
            if (tables.TryGetValue(tableName, out table))
            {
                return true;
            }

            try
            {
                table = LoadTable(tableName);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(exception.Message, this);
                table = null;
                return false;
            }
        }

        public bool TryFindRow(string tableName, string idFieldName, string id, out ConfigRow row)
        {
            row = null;
            return TryGetTable(tableName, out var table) && table.TryFindById(idFieldName, id, out row);
        }
    }
}
