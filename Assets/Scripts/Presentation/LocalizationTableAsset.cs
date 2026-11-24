using System;
using UnityEngine;

namespace Echoes.Presentation
{
    [Serializable]
    public sealed class LocEntry
    {
        public string key;
        public string zhHans;
        public string en;
    }

    [CreateAssetMenu(menuName = "Echoes/Localization Table")]
    public sealed class LocalizationTableAsset : ScriptableObject
    {
        public LocEntry[] entries = new LocEntry[0];

        public string Resolve(string key, string locale)
        {
            if (string.IsNullOrEmpty(key) || entries == null)
                return key;
            for (int i = 0; i < entries.Length; i++)
            {
                LocEntry entry = entries[i];
                if (entry == null || entry.key != key)
                    continue;
                if (locale == "en" && !string.IsNullOrEmpty(entry.en))
                    return entry.en;
                if (!string.IsNullOrEmpty(entry.zhHans))
                    return entry.zhHans;
                if (!string.IsNullOrEmpty(entry.en))
                    return entry.en;
            }
            return key;
        }
    }
}
