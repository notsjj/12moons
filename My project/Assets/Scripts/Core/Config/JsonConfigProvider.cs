using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace TwelveMoons.Core.Config
{
    public sealed class JsonConfigProvider : IConfigProvider
    {
        private readonly string rootDirectory;

        public JsonConfigProvider(string rootDirectory)
        {
            this.rootDirectory = rootDirectory;
        }

        public bool CanLoad(string tableName)
        {
            return File.Exists(GetPath(tableName));
        }

        public ConfigTable LoadTable(string tableName)
        {
            var path = GetPath(tableName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"JSON config table not found: {tableName}", path);
            }

            var array = JArray.Parse(File.ReadAllText(path, Encoding.UTF8));
            var fields = new List<string>();
            var rows = new List<ConfigRow>();

            foreach (var token in array.OfType<JObject>())
            {
                var values = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (var property in token.Properties())
                {
                    if (!fields.Contains(property.Name))
                    {
                        fields.Add(property.Name);
                    }

                    values[property.Name] = property.Value.Type == JTokenType.Null
                        ? string.Empty
                        : property.Value.ToString();
                }

                rows.Add(new ConfigRow(values));
            }

            return new ConfigTable(tableName, fields, rows);
        }

        private string GetPath(string tableName)
        {
            return Path.Combine(rootDirectory, $"{tableName}.json");
        }
    }
}
