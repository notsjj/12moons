using System;
using System.Collections.Generic;

namespace TwelveMoons.Core.Config
{
    [Serializable]
    public sealed class ConfigRow
    {
        private readonly Dictionary<string, string> values;

        public ConfigRow(IReadOnlyDictionary<string, string> values)
        {
            this.values = new Dictionary<string, string>(values);
        }

        public IReadOnlyDictionary<string, string> Values => values;

        public bool TryGetString(string fieldName, out string value)
        {
            return values.TryGetValue(fieldName, out value);
        }

        public string GetString(string fieldName, string defaultValue = "")
        {
            return values.TryGetValue(fieldName, out var value) ? value : defaultValue;
        }

        public int GetInt(string fieldName, int defaultValue = 0)
        {
            return values.TryGetValue(fieldName, out var value) && int.TryParse(value, out var parsed)
                ? parsed
                : defaultValue;
        }

        public bool GetBool(string fieldName, bool defaultValue = false)
        {
            if (!values.TryGetValue(fieldName, out var value))
            {
                return defaultValue;
            }

            if (bool.TryParse(value, out var parsed))
            {
                return parsed;
            }

            if (int.TryParse(value, out var numericValue))
            {
                return numericValue != 0;
            }

            return defaultValue;
        }
    }
}
