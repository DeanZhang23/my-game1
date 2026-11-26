using System.Collections.Generic;

namespace Echoes.Simulation
{
    public static class M0Catalog
    {
        public const string EncounterId = "encounter_m0_test";

        public static SkillDef MeleeBasic()
        {
            return new SkillDef
            {
                Id = SkillIds.MeleeBasic,
                NameKey = "skill.placeholder_melee_basic.name",
                School = DamageSchool.Physical,
                Ratio = 1.0m,
                EnergyCost = 0m,
                CooldownSeconds = 0m,
                IsBasic = true
            };
        }

        public static SkillDef Cleave()
        {
            return new SkillDef
            {
                Id = SkillIds.Cleave,
                NameKey = "skill.placeholder_cleave.name",
                School = DamageSchool.Physical,
                Ratio = 1.6m,
                EnergyCost = 100m,
                CooldownSeconds = 8m,
                IsBasic = false
            };
        }

        public static SkillDef CasterBasic()
        {
            return new SkillDef
            {
                Id = SkillIds.CasterBasic,
                NameKey = "skill.placeholder_caster_basic.name",
                School = DamageSchool.Magic,
                Ratio = 0.5m,
                EnergyCost = 0m,
                CooldownSeconds = 0m,
                IsBasic = true
            };
        }

        public static SkillDef Bolt()
        {
            return new SkillDef
            {
                Id = SkillIds.Bolt,
                NameKey = "skill.placeholder_bolt.name",
                School = DamageSchool.Magic,
                Ratio = 1.8m,
                EnergyCost = 100m,
                CooldownSeconds = 10m,
                IsBasic = false
            };
        }

        public static SkillDef EnemyBasic()
        {
            return new SkillDef
            {
                Id = SkillIds.EnemyBasic,
                NameKey = "skill.placeholder_enemy_basic.name",
                School = DamageSchool.Physical,
                Ratio = 1.0m,
                EnergyCost = 0m,
                CooldownSeconds = 0m,
                IsBasic = true
            };
        }

        public static UnitDef Melee()
        {
            return new UnitDef
            {
                Id = UnitIds.Melee,
                NameKey = "unit.placeholder_melee.name",
                MaxHp = 120,
                Attack = 18m,
                SpellPower = 0m,
                Armor = 8m,
                MagicResist = 4m,
                CritRate = 0.05m,
                CritDamage = 0.50m,
                BaseAttackInterval = 1.20m,
                EnergyPerSecond = 8m,
                MaxEnergy = 100,
                Role = UnitRoles.Melee,
                MoveSpeed = 3.0m,
                AttackRange = 0.9m,
                Basic = MeleeBasic(),
                Skill = Cleave()
            };
        }

        public static UnitDef Caster()
        {
            return new UnitDef
            {
                Id = UnitIds.Caster,
                NameKey = "unit.placeholder_caster.name",
                MaxHp = 80,
                Attack = 4m,
                SpellPower = 22m,
                Armor = 2m,
                MagicResist = 8m,
                CritRate = 0.05m,
                CritDamage = 0.50m,
                BaseAttackInterval = 1.50m,
                EnergyPerSecond = 10m,
                MaxEnergy = 100,
                Role = UnitRoles.Ranged,
                MoveSpeed = 2.5m,
                AttackRange = 3.5m,
                Basic = CasterBasic(),
                Skill = Bolt()
            };
        }

        public static UnitDef Enemy()
        {
            return new UnitDef
            {
                Id = UnitIds.Enemy,
                NameKey = "unit.placeholder_enemy.name",
                MaxHp = 100,
                Attack = 14m,
                SpellPower = 0m,
                Armor = 8m,
                MagicResist = 4m,
                CritRate = 0.05m,
                CritDamage = 0.50m,
                BaseAttackInterval = 1.20m,
                EnergyPerSecond = 8m,
                MaxEnergy = 100,
                Role = UnitRoles.Melee,
                MoveSpeed = 3.0m,
                AttackRange = 0.9m,
                Basic = EnemyBasic(),
                Skill = null
            };
        }

        public static EncounterDef CreateEncounter()
        {
            return new EncounterDef
            {
                Id = EncounterId,
                CountdownMs = 3000,
                TimeoutMs = 150000,
                Allies = new List<Placement>
                {
                    new Placement { SlotId = "ally_c2", Unit = Melee() },
                    new Placement { SlotId = "ally_a2", Unit = Caster() }
                },
                Enemies = new List<Placement>
                {
                    new Placement { SlotId = "enemy_c1", Unit = Enemy() },
                    new Placement { SlotId = "enemy_c2", Unit = Enemy() }
                }
            };
        }

        // Setup reserve only. CreateEncounter does not include these, so an unplaced
        // reserve unit does not march and does not change the default lineup.
        public static UnitDef Oathblade()
        {
            return CopyOf(Melee(), UnitIds.Oathblade);
        }

        public static UnitDef Chainjudge()
        {
            return CopyOf(Melee(), UnitIds.Chainjudge);
        }

        public static UnitDef Lampsister()
        {
            return CopyOf(Caster(), UnitIds.Lampsister);
        }

        public static UnitDef Misthunter()
        {
            return CopyOf(Caster(), UnitIds.Misthunter);
        }

        public static UnitDef Boneapostle()
        {
            return CopyOf(Caster(), UnitIds.Boneapostle);
        }

        public static UnitDef Bloodacolyte()
        {
            return CopyOf(Caster(), UnitIds.Bloodacolyte);
        }

        public static UnitDef[] SetupReserve()
        {
            return new[]
            {
                Oathblade(),
                Chainjudge(),
                Lampsister(),
                Misthunter(),
                Boneapostle(),
                Bloodacolyte()
            };
        }

        private static UnitDef CopyOf(UnitDef source, string id)
        {
            return new UnitDef
            {
                Id = id,
                NameKey = "unit." + id.Substring("unit_".Length) + ".name",
                MaxHp = source.MaxHp,
                Attack = source.Attack,
                SpellPower = source.SpellPower,
                Armor = source.Armor,
                MagicResist = source.MagicResist,
                CritRate = source.CritRate,
                CritDamage = source.CritDamage,
                BaseAttackInterval = source.BaseAttackInterval,
                EnergyPerSecond = source.EnergyPerSecond,
                MaxEnergy = source.MaxEnergy,
                Role = source.Role,
                MoveSpeed = source.MoveSpeed,
                AttackRange = source.AttackRange,
                Basic = CopySkill(source.Basic),
                Skill = CopySkill(source.Skill)
            };
        }

        private static SkillDef CopySkill(SkillDef source)
        {
            if (source == null)
                return null;
            return new SkillDef
            {
                Id = source.Id,
                NameKey = source.NameKey,
                School = source.School,
                Ratio = source.Ratio,
                EnergyCost = source.EnergyCost,
                CooldownSeconds = source.CooldownSeconds,
                IsBasic = source.IsBasic
            };
        }
    }
}
