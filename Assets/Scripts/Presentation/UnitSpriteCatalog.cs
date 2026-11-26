using System;
using UnityEngine;

namespace Echoes.Presentation
{
    [Serializable]
    public sealed class UnitSpriteEntry
    {
        public string unitId;
        // Idle still. Walk and attack are the 4-frame clips. Leave them empty for
        // the enemy placeholder so it stays on this sprite.
        public Sprite sprite;
        public Sprite[] walk;
        public Sprite[] attack;
    }

    // Presentation-only map from a simulation unit id to idle, walk, and attack sprites.
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

        // True only when idle plus a full walk loop and a full attack one-shot are assigned.
        // The enemy placeholder fails this and the presentation keeps its idle.
        public bool TryGetPose(string unitId, out Sprite idle, out Sprite[] walk, out Sprite[] attack)
        {
            idle = null;
            walk = null;
            attack = null;
            UnitSpriteEntry entry = Find(unitId);
            if (entry == null || entry.sprite == null)
                return false;
            if (!Complete(entry.walk) || !Complete(entry.attack))
                return false;
            idle = entry.sprite;
            walk = entry.walk;
            attack = entry.attack;
            return true;
        }

        private UnitSpriteEntry Find(string unitId)
        {
            if (entries == null || string.IsNullOrEmpty(unitId))
                return null;
            for (int i = 0; i < entries.Length; i++)
            {
                UnitSpriteEntry entry = entries[i];
                if (entry != null && entry.unitId == unitId)
                    return entry;
            }
            return null;
        }

        private static bool Complete(Sprite[] frames)
        {
            if (frames == null || frames.Length != AllyPoseClock.FrameCount)
                return false;
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] == null)
                    return false;
            }
            return true;
        }
    }
}
