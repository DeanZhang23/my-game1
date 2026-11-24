using UnityEngine;

namespace Echoes.Presentation
{
    [CreateAssetMenu(menuName = "Echoes/Unit Definition")]
    public sealed class UnitDefinitionAsset : ScriptableObject
    {
        public string id;
        public string nameKey;
        public int maxHp;
        public float attack;
        public float spellPower;
        public float armor;
        public float magicResist;
        public float critRate;
        public float critDamage;
        public float baseAttackInterval;
        public float energyPerSecond;
        public int maxEnergy = 100;
        public SkillDefinitionAsset basicAttack;
        public SkillDefinitionAsset skill;
    }
}
