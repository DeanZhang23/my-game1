using System;
using System.Collections.Generic;
using System.Globalization;

namespace Echoes.Simulation
{
    public sealed class BattleEvent
    {
        private readonly List<KeyValuePair<string, string>> _fields;

        public BattleEvent(int seq, string type, List<KeyValuePair<string, string>> fields)
        {
            Seq = seq;
            Type = type;
            _fields = fields;
        }

        public int Seq { get; private set; }
        public string Type { get; private set; }
        public IList<KeyValuePair<string, string>> Fields { get { return _fields; } }

        public string Get(string key)
        {
            for (int i = 0; i < _fields.Count; i++)
            {
                if (_fields[i].Key == key)
                    return _fields[i].Value;
            }
            return null;
        }
    }

    public static class EventFormat
    {
        public static string Dec(decimal value)
        {
            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }

        public static string Int(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string Bool(bool value)
        {
            return value ? "true" : "false";
        }

        public static string Ms(int ms)
        {
            int sign = ms < 0 ? -1 : 1;
            int abs = Math.Abs(ms);
            int whole = abs / 1000;
            int frac = abs % 1000;
            string text = whole.ToString(CultureInfo.InvariantCulture)
                + "."
                + frac.ToString("000", CultureInfo.InvariantCulture);
            return sign < 0 ? "-" + text : text;
        }
    }
}
