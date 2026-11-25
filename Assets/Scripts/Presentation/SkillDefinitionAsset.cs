using UnityEngine;

namespace Echoes.Presentation
{
    public enum AssetSchool
    {
        Physical = 0,
        Magic = 1
    }

    [CreateAssetMenu(menuName = "Echoes/Skill Definition")]
    public sealed class SkillDefinitionAsset : ScriptableObject
    {
        public string id;
        public string nameKey;
        public AssetSchool school;
        public float ratio;
        public float energyCost;
        public float cooldownSeconds;
        public bool basic;
    }
}
