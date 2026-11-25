using System;
using UnityEngine;

namespace Echoes.Presentation
{
    [Serializable]
    public sealed class PlacementEntry
    {
        public string slotId;
        public UnitDefinitionAsset unit;
    }

    [CreateAssetMenu(menuName = "Echoes/Encounter Definition")]
    public sealed class EncounterDefinitionAsset : ScriptableObject
    {
        public string id;
        public float countdownSeconds = 3f;
        public float tacticalWindowDelaySeconds = 8f;
        public float tacticalWindowDurationSeconds = 8f;
        public float timeoutSeconds = 150f;
        public PlacementEntry[] allies;
        public PlacementEntry[] enemies;
    }
}
