using System;
using System.Collections.Generic;

namespace Echoes.Simulation
{
    public sealed class SkillDef
    {
        public string Id;
        public string NameKey;
        public DamageSchool School;
        public decimal Ratio;
        public decimal EnergyCost;
        public decimal CooldownSeconds;
        public bool IsBasic;
    }

    public sealed class UnitDef
    {
        public string Id;
        public string NameKey;
        public int MaxHp;
        public decimal Attack;
        public decimal SpellPower;
        public decimal Armor;
        public decimal MagicResist;
        public decimal CritRate;
        public decimal CritDamage;
        public decimal BaseAttackInterval;
        public decimal EnergyPerSecond;
        public int MaxEnergy;
        public string Role;
        public decimal MoveSpeed;
        public decimal AttackRange;
        public SkillDef Basic;
        public SkillDef Skill;
    }

    public struct CombatantView
    {
        public string SlotId;
        public string UnitId;
        public decimal X;
        public decimal Y;
        public bool Alive;
        public int Hp;
        public int MaxHp;
        public int Shield;
        public bool IsAlly;
        public string Role;
        public decimal Attack;
        public decimal Armor;
        public decimal Energy;
        public int Keen;
        public decimal SkillDamageMultiplier;
        public int IntervalMs;
    }

    public sealed class FaceEffectSpec
    {
        public string FaceId;
        public string TargetKind;
        public string HeroId;
        public string HeroId2;
    }

    public sealed class Placement
    {
        public string SlotId;
        public UnitDef Unit;
    }

    public sealed class EncounterDef
    {
        public string Id;
        public int CountdownMs;
        public int TimeoutMs;
        public List<Placement> Allies = new List<Placement>();
        public List<Placement> Enemies = new List<Placement>();
    }

    public sealed class FaceResolution
    {
        public string ComboId = ComboIds.None;
        public string ComboFace = "none";
        public decimal Coefficient = 1m;
        public decimal AttackMultiplier = 1m;
        public decimal ArmorBonus;
        public decimal ShieldLifeRatio;
        public int BackEnergy;
        public decimal SkillDamageMultiplier = 1m;
        public decimal AttackSpeedBonus;
        public decimal CooldownReductionSeconds;
        public int Gold;
        public decimal SacrificeFraction;
        public int KeenPerFront;
        public int WildTokens;
        public int WildUsed;
        public string WildFace = "none";
        public FaceEffectSpec[] Effects = new FaceEffectSpec[0];
    }
}
