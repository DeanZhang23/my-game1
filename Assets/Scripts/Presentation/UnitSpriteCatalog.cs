using System;
using UnityEngine;

namespace Echoes.Presentation
{
    [Serializable]
    public sealed class UnitSpriteEntry
    {
        public string unitId;
        // Idle still. Walk and attack are the 4-frame clips. unit_placeholder_enemy
        // uses the same complete set. An entry with an empty clip stays on this sprite.
        // Death is the 4-frame fall for the three default TestArena units only.
        public Sprite sprite;
        public Sprite[] walk;
        public Sprite[] attack;
        public Sprite[] death;
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
        // unit_placeholder_enemy meets this, so enemy_c1 and enemy_c2 play through it.
        // An incomplete entry stays on idle.
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

        // True only for a full 4-frame death one-shot. The other six official allies
        // have no death frames, so this stays false and they keep idle / walk / attack.
        public bool TryGetDeath(string unitId, out Sprite[] death)
        {
            death = null;
            UnitSpriteEntry entry = Find(unitId);
            if (entry == null || !Complete(entry.death))
                return false;
            death = entry.death;
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
