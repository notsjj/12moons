using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TwelveMoons.Core.Config
{
    public sealed class CsvConfigProvider : IConfigProvider
    {
        private readonly string rootDirectory;

        public CsvConfigProvider(string rootDirectory)
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
                throw new FileNotFoundException($"CSV config table not found: {tableName}", path);
            }

            var lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0)
            {
                throw new InvalidDataException($"CSV config table is empty: {path}");
            }

            var fields = ParseCsvLine(lines[0]);
            var rows = new List<ConfigRow>();

            for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                if (string.IsNullOrWhiteSpace(lines[lineIndex]))
                {
                    continue;
                }

                var cells = ParseCsvLine(lines[lineIndex]);
                var values = new Dictionary<string, string>(StringComparer.Ordinal);

                for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
                {
                    var value = fieldIndex < cells.Count ? cells[fieldIndex] : string.Empty;
                    values[fields[fieldIndex]] = value;
                }

                rows.Add(new ConfigRow(values));
            }

            return new ConfigTable(tableName, fields, rows);
        }

        private string GetPath(string tableName)
        {
            return Path.Combine(rootDirectory, $"{tableName}.csv");
        }

        private static List<string> ParseCsvLine(string line)
        {
            var cells = new List<string>();
            var cell = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var character = line[i];

                if (character == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (character == ',' && !inQuotes)
                {
                    cells.Add(cell.ToString());
                    cell.Clear();
                }
                else
                {
                    cell.Append(character);
                }
            }

            cells.Add(cell.ToString());
            return cells;
        }
    }
}
