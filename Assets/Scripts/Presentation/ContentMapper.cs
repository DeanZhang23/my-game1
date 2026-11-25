using System;
using System.Collections.Generic;
using Echoes.Simulation;

namespace Echoes.Presentation
{
    public static class ContentMapper
    {
        public static decimal Round(float value)
        {
            return Math.Round((decimal)value, 4, MidpointRounding.AwayFromZero);
        }

        public static EncounterDef ToEncounter(EncounterDefinitionAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException("asset");
            var encounter = new EncounterDef();
            encounter.Id = asset.id;
            encounter.CountdownMs = CombatFormulas.SecondsToMs(Round(asset.countdownSeconds));
            encounter.TimeoutMs = CombatFormulas.SecondsToMs(Round(asset.timeoutSeconds));
            encounter.Allies = ToPlacements(asset.allies);
            encounter.Enemies = ToPlacements(asset.enemies);
            return encounter;
        }

        private static List<Placement> ToPlacements(PlacementEntry[] entries)
        {
            var list = new List<Placement>();
            if (entries == null)
                return list;
            for (int i = 0; i < entries.Length; i++)
            {
                PlacementEntry entry = entries[i];
                if (entry == null || entry.unit == null)
                    continue;
                list.Add(new Placement
                {
                    SlotId = entry.slotId,
                    Unit = ToUnit(entry.unit)
                });
            }
            return list;
        }

        public static UnitDef ToUnit(UnitDefinitionAsset asset)
        {
            return new UnitDef
            {
                Id = asset.id,
                NameKey = asset.nameKey,
                MaxHp = asset.maxHp,
                Attack = Round(asset.attack),
                SpellPower = Round(asset.spellPower),
                Armor = Round(asset.armor),
                MagicResist = Round(asset.magicResist),
                CritRate = Round(asset.critRate),
                CritDamage = Round(asset.critDamage),
                BaseAttackInterval = Round(asset.baseAttackInterval),
                EnergyPerSecond = Round(asset.energyPerSecond),
                MaxEnergy = asset.maxEnergy,
                Role = UnitRoles.Require(string.IsNullOrEmpty(asset.role) ? UnitRoles.Melee : asset.role),
                MoveSpeed = Round(asset.moveSpeed),
                AttackRange = Round(asset.attackRange),
                Basic = ToSkill(asset.basicAttack),
                Skill = ToSkill(asset.skill)
            };
        }

        public static SkillDef ToSkill(SkillDefinitionAsset asset)
        {
            if (asset == null)
                return null;
            return new SkillDef
            {
                Id = asset.id,
                NameKey = asset.nameKey,
                School = asset.school == AssetSchool.Magic ? DamageSchool.Magic : DamageSchool.Physical,
                Ratio = Round(asset.ratio),
                EnergyCost = Round(asset.energyCost),
                CooldownSeconds = Round(asset.cooldownSeconds),
                IsBasic = asset.basic
            };
        }
    }
}
