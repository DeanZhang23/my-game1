using System;
using UnityEngine;

namespace Echoes.Presentation
{
    [Serializable]
    public sealed class UnitSpriteEntry
    {
        public string unitId;
        public Sprite sprite;
    }

    // Presentation-only map from a simulation unit id to an idle sprite.
    // It does not add units, stats, or skills.
    [CreateAssetMenu(menuName = "Echoes/Unit Sprite Catalog")]
    public sealed class UnitSpriteCatalog : ScriptableObject
    {
        public UnitSpriteEntry[] entries;

        public Sprite SpriteFor(string unitId)
        {
            if (entries == null || string.IsNullOrEmpty(unitId))
                return null;
            for (int i = 0; i < entries.Length; i++)
            {
                UnitSpriteEntry entry = entries[i];
                if (entry != null && entry.unitId == unitId && entry.sprite != null)
                    return entry.sprite;
            }
            return null;
        }
    }
}
