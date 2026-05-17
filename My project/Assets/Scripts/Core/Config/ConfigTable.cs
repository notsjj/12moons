using System;
using System.Collections.Generic;
using System.Linq;

namespace TwelveMoons.Core.Config
{
    [Serializable]
    public sealed class ConfigTable
    {
        private readonly List<string> fields;
        private readonly List<ConfigRow> rows;

        public ConfigTable(string tableName, IEnumerable<string> fields, IEnumerable<ConfigRow> rows)
        {
            TableName = tableName;
            this.fields = fields.ToList();
            this.rows = rows.ToList();
        }

        public string TableName { get; }

        public IReadOnlyList<string> Fields => fields;

        public IReadOnlyList<ConfigRow> Rows => rows;

        public bool TryFindById(string idFieldName, string id, out ConfigRow row)
        {
            row = rows.FirstOrDefault(candidate => candidate.GetString(idFieldName) == id);
            return row != null;
        }
    }
}
