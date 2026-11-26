using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Echoes.Simulation.Tests
{
    public class OfficialAllyReserveTests
    {
        private const string GoldenSha256 = "a4707d49ea7acb0a3e524b6c7aa50ebc10aa1cf403621fecd89080ef5887bcf3";

        [Fact]
        public void Catalog_CopiesTheTemplates_AndLeavesTheDefaultEncounterAlone()
        {
            UnitDef[] reserve = M0Catalog.SetupReserve();
            Assert.Equal(UnitIds.Reserve.Length, reserve.Length);
            Assert.Equal(6, reserve.Length);

            var seen = new HashSet<string>();
            for (int i = 0; i < reserve.Length; i++)
            {
                UnitDef unit = reserve[i];
                Assert.Equal(UnitIds.Reserve[i], unit.Id);
                Assert.True(seen.Add(unit.Id));
                Assert.NotEqual(UnitIds.Melee, unit.Id);
                Assert.NotEqual(UnitIds.Caster, unit.Id);
                Assert.NotEqual(UnitIds.Enemy, unit.Id);
                Assert.Equal("unit." + unit.Id.Substring("unit_".Length) + ".name", unit.NameKey);

                bool meleeCopy = unit.Id == UnitIds.Oathblade || unit.Id == UnitIds.Chainjudge;
                UnitDef template = meleeCopy ? M0Catalog.Melee() : M0Catalog.Caster();
                Assert.Equal(meleeCopy ? UnitRoles.Melee : UnitRoles.Ranged, unit.Role);
                AssertCopiedFrom(template, unit);
            }

            EncounterDef encounter = M0Catalog.CreateEncounter();
            Assert.Equal(2, encounter.Allies.Count);
            Assert.Equal("ally_c2", encounter.Allies[0].SlotId);
            Assert.Equal(UnitIds.Melee, encounter.Allies[0].Unit.Id);
            Assert.Equal("ally_a2", encounter.Allies[1].SlotId);
            Assert.Equal(UnitIds.Caster, encounter.Allies[1].Unit.Id);
            Assert.Equal(2, encounter.Enemies.Count);
            Assert.Equal("enemy_c1", encounter.Enemies[0].SlotId);
            Assert.Equal("enemy_c2", encounter.Enemies[1].SlotId);
            Assert.Equal(UnitIds.Enemy, encounter.Enemies[0].Unit.Id);
            Assert.Equal(UnitIds.Enemy, encounter.Enemies[1].Unit.Id);
            for (int i = 0; i < encounter.Allies.Count; i++)
                Assert.DoesNotContain(encounter.Allies[i].Unit.Id, UnitIds.Reserve);
        }

        [Fact]
        public void SetupReserve_StartsBenched_AndEachUnitCanMarch()
        {
            BattleSimulator bench = StartWithReserve();
            Assert.Equal(8, bench.AllyRosterCount);
            Assert.Equal(2, bench.PlacedAllyCount);
            Assert.Equal("ally_c2", bench.AllySlotOf(UnitIds.Melee));
            Assert.Equal("ally_a2", bench.AllySlotOf(UnitIds.Caster));
            UnitDef[] reserve = M0Catalog.SetupReserve();
            for (int i = 0; i < reserve.Length; i++)
            {
                Assert.Equal(reserve[i].Id, bench.AllyUnitIdAt(i + 2));
                Assert.Null(bench.AllySlotAt(i + 2));
                Assert.False(bench.TryAddReserve(reserve[i]));
            }

            string[] slots =
            {
                "ally_a1", "ally_b1", "ally_c1", "ally_a3", "ally_b3", "ally_c3"
            };
            for (int i = 0; i < reserve.Length; i++)
            {
                UnitDef unit = reserve[i];
                BattleSimulator sim = StartWithReserve();
                Assert.True(sim.TrySetAllySlot(unit.Id, slots[i]), unit.Id);
                Assert.Equal(slots[i], sim.AllySlotOf(unit.Id));
                Assert.Equal("ally_c2", sim.AllySlotOf(UnitIds.Melee));
                Assert.Equal("ally_a2", sim.AllySlotOf(UnitIds.Caster));
                Assert.Equal(3, sim.PlacedAllyCount);
                Assert.True(sim.CanCommit);
                sim.Commit();
                Assert.Equal(BattlePhase.Countdown, sim.Phase);

                BattleEvent spawned = Spawn(sim, slots[i]);
                Assert.Equal(unit.Id, spawned.Get("unitId"));
                Assert.Equal(unit.Role, spawned.Get("role"));
                Assert.Equal(EventFormat.Int(unit.MaxHp), spawned.Get("maxHp"));
                Assert.Equal(EventFormat.Dec(unit.Attack), spawned.Get("attack"));
                Assert.Equal(EventFormat.Dec(unit.Armor), spawned.Get("armor"));
                Assert.Equal(EventFormat.Dec(unit.MoveSpeed), spawned.Get("moveSpeed"));
                Assert.Equal(EventFormat.Dec(unit.AttackRange), spawned.Get("attackRange"));
                Assert.Equal(UnitIds.Melee, Spawn(sim, "ally_c2").Get("unitId"));
                Assert.Equal(UnitIds.Caster, Spawn(sim, "ally_a2").Get("unitId"));
                Assert.Equal(UnitIds.Enemy, Spawn(sim, "enemy_c1").Get("unitId"));
                Assert.Equal(UnitIds.Enemy, Spawn(sim, "enemy_c2").Get("unitId"));
            }
        }

        [Fact]
        public void PlacementRules_StillHoldWhenTheReserveIsPresent()
        {
            BattleSimulator sim = StartWithReserve();
            Assert.True(sim.TrySetAllySlot(UnitIds.Oathblade, "ally_c2"));
            Assert.Equal("ally_c2", sim.AllySlotOf(UnitIds.Oathblade));
            Assert.Null(sim.AllySlotOf(UnitIds.Melee));
            Assert.Equal("ally_a2", sim.AllySlotOf(UnitIds.Caster));
            Assert.Equal(2, sim.PlacedAllyCount);
            Assert.False(sim.TrySetAllySlot(UnitIds.Chainjudge, "enemy_c1"));
            Assert.False(sim.TrySetAllySlot(UnitIds.Chainjudge, "ally_c9"));
            Assert.Null(sim.AllySlotOf(UnitIds.Chainjudge));

            string[] open = { "ally_c1", "ally_c3", "ally_b2", "ally_b3" };
            Assert.True(sim.TrySetAllySlot(UnitIds.Chainjudge, open[0]));
            Assert.True(sim.TrySetAllySlot(UnitIds.Lampsister, open[1]));
            Assert.True(sim.TrySetAllySlot(UnitIds.Misthunter, open[2]));
            Assert.Equal(BattleSimulator.MaxPlacedAllies, sim.PlacedAllyCount);
            Assert.False(sim.TrySetAllySlot(UnitIds.Boneapostle, open[3]));
            Assert.Null(sim.AllySlotOf(UnitIds.Boneapostle));
            Assert.Equal(BattleSimulator.MaxPlacedAllies, sim.PlacedAllyCount);

            Assert.True(sim.TrySetAllySlot(UnitIds.Boneapostle, "ally_a2"));
            Assert.Equal("ally_a2", sim.AllySlotOf(UnitIds.Boneapostle));
            Assert.Null(sim.AllySlotOf(UnitIds.Caster));
            Assert.Equal(BattleSimulator.MaxPlacedAllies, sim.PlacedAllyCount);
            Assert.True(sim.CanCommit);

            BattleSimulator empty = StartWithReserve();
            Assert.True(empty.TrySetAllySlot(UnitIds.Melee, null));
            Assert.True(empty.TrySetAllySlot(UnitIds.Caster, ""));
            Assert.Equal(0, empty.PlacedAllyCount);
            Assert.Equal(8, empty.AllyRosterCount);
            Assert.False(empty.CanCommit);
            Assert.Throws<InvalidOperationException>(() => empty.Commit());
            Assert.Equal(BattlePhase.Setup, empty.Phase);

            BattleSimulator frozen = StartWithReserve();
            frozen.Commit();
            Assert.Equal(BattlePhase.Countdown, frozen.Phase);
            Assert.False(frozen.TrySetAllySlot(UnitIds.Oathblade, "ally_b1"));
            Assert.False(frozen.TrySetAllySlot(UnitIds.Melee, null));
            Assert.False(frozen.TryAddReserve(M0Catalog.Oathblade()));
            Assert.Null(frozen.AllySlotOf(UnitIds.Oathblade));
            Assert.Equal("ally_c2", frozen.AllySlotOf(UnitIds.Melee));
            Assert.Equal("ally_a2", frozen.AllySlotOf(UnitIds.Caster));
        }

        [Fact]
        public void UnplacedReserve_DoesNotChangeGoldenSeed424242()
        {
            string path = GoldenPath();
            string hash = Sha256(path);
            Assert.Equal(GoldenSha256, hash);

            BattleSimulator sim = StartWithReserve();
            Assert.Equal(2, sim.PlacedAllyCount);
            sim.Commit();
            sim.BeginAutoCombat();
            bool rolled = false;
            int guard = 0;
            while (sim.Phase != BattlePhase.Resolve)
            {
                if (++guard > 200000)
                    throw new InvalidOperationException("Battle did not finish.");
                if (!rolled && sim.CanRoll)
                {
                    sim.Roll();
                    sim.Confirm();
                    rolled = true;
                }
                if (sim.Phase != BattlePhase.AutoCombat)
                    break;
                sim.PumpMs(50);
            }

            Assert.Equal(4, Count(sim, BattleEvents.UnitSpawned));
            for (int i = 0; i < UnitIds.Reserve.Length; i++)
                Assert.Null(FindSpawn(sim, UnitIds.Reserve[i]));
            BattleEvent end = Find(sim, BattleEvents.BattleEnd);
            Assert.Equal("defeat", end.Get("outcome"));
            Assert.Equal("10.800", end.Get("duration"));
            Assert.Equal("0", end.Get("gold"));

            string expected = File.ReadAllText(path).Replace("\r\n", "\n");
            Assert.Equal(expected, GoldenSeedTests.Canonical(sim));
        }

        [Fact]
        public void Assets_Localization_AndSprites_WireTheSixAllies()
        {
            string root = RepoRoot();
            string loc = Read(root, "Assets/Resources/M0/M0Localization.asset");
            string sprites = Read(root, "Assets/Resources/M0/M0UnitSprites.asset");
            string encounter = Read(root, "Assets/Resources/M0/M0TestEncounter.asset");
            string doc = Read(root, "docs/gdd/m0-official-allies-testarena.md");

            Assert.Contains("zhHans: \"灰烬卫士\"", loc);
            Assert.Contains("en: \"Ashguard\"", loc);
            Assert.Contains("zhHans: \"裂隙术士\"", loc);
            Assert.Contains("en: \"Riftmage\"", loc);
            Assert.Contains("zhHans: \"锈蚀步卒\"", loc);
            Assert.Contains("en: \"Rustsoldier\"", loc);
            Assert.Contains("unitId: unit_placeholder_melee", sprites);
            Assert.Contains("guid: e1d00001e1d00001e1d00001e1d00001", sprites);
            Assert.Contains("unitId: unit_placeholder_caster", sprites);
            Assert.Contains("guid: e1d00004e1d00004e1d00004e1d00004", sprites);
            Assert.Contains("unitId: unit_placeholder_enemy", sprites);
            Assert.Contains("guid: e1d00009e1d00009e1d00009e1d00009", sprites);
            Assert.DoesNotContain("unit_oathblade", encounter);
            Assert.Contains("slotId: ally_c2", encounter);
            Assert.Contains("slotId: ally_a2", encounter);

            var rows = new[]
            {
                Row(UnitIds.Oathblade, "烬誓剑士", "Oathblade", "melee", "unit_placeholder_melee", "e1d00002e1d00002e1d00002e1d00002", "e1b00001e1b00001e1b00001e1b00001", "e1b00002e1b00002e1b00002e1b00002"),
                Row(UnitIds.Chainjudge, "锁链判官", "Chainjudge", "melee", "unit_placeholder_melee", "e1d00007e1d00007e1d00007e1d00007", "e1b00001e1b00001e1b00001e1b00001", "e1b00002e1b00002e1b00002e1b00002"),
                Row(UnitIds.Lampsister, "墓灯修女", "Lampsister", "ranged", "unit_placeholder_caster", "e1d00003e1d00003e1d00003e1d00003", "e1b00003e1b00003e1b00003e1b00003", "e1b00004e1b00004e1b00004e1b00004"),
                Row(UnitIds.Misthunter, "雾行猎手", "Misthunter", "ranged", "unit_placeholder_caster", "e1d00005e1d00005e1d00005e1d00005", "e1b00003e1b00003e1b00003e1b00003", "e1b00004e1b00004e1b00004e1b00004"),
                Row(UnitIds.Boneapostle, "骨匣使徒", "Boneapostle", "ranged", "unit_placeholder_caster", "e1d00006e1d00006e1d00006e1d00006", "e1b00003e1b00003e1b00003e1b00003", "e1b00004e1b00004e1b00004e1b00004"),
                Row(UnitIds.Bloodacolyte, "血契侍从", "Bloodacolyte", "ranged", "unit_placeholder_caster", "e1d00008e1d00008e1d00008e1d00008", "e1b00003e1b00003e1b00003e1b00003", "e1b00004e1b00004e1b00004e1b00004")
            };

            for (int i = 0; i < rows.Length; i++)
            {
                AllyRow row = rows[i];
                string nameKey = "unit." + row.Id.Substring("unit_".Length) + ".name";
                int keyAt = loc.IndexOf("key: " + nameKey, StringComparison.Ordinal);
                Assert.True(keyAt >= 0, nameKey);
                string block = loc.Substring(keyAt, Math.Min(180, loc.Length - keyAt));
                Assert.Contains("zhHans: \"" + row.Zh + "\"", block);
                Assert.Contains("en: \"" + row.En + "\"", block);

                int spriteAt = sprites.IndexOf("unitId: " + row.Id, StringComparison.Ordinal);
                Assert.True(spriteAt >= 0, row.Id);
                string spriteBlock = sprites.Substring(spriteAt, Math.Min(160, sprites.Length - spriteAt));
                Assert.Contains("guid: " + row.SpriteGuid, spriteBlock);

                string asset = Read(root, "Assets/Resources/M0/Units/" + row.Id + ".asset");
                string template = Read(root, "Assets/Resources/M0/Units/" + row.Template + ".asset");
                Assert.Contains("id: " + row.Id, asset);
                Assert.Contains("nameKey: " + nameKey, asset);
                Assert.Contains("role: " + row.Role, asset);
                Assert.Contains("guid: " + row.BasicGuid, asset);
                Assert.Contains("guid: " + row.SkillGuid, asset);
                Assert.DoesNotContain("id: " + row.Template, asset);
                string[] fields =
                {
                    "maxHp", "attack", "spellPower", "armor", "magicResist", "critRate", "critDamage",
                    "baseAttackInterval", "energyPerSecond", "maxEnergy", "moveSpeed", "attackRange"
                };
                for (int f = 0; f < fields.Length; f++)
                    Assert.Equal(Field(template, fields[f]), Field(asset, fields[f]));

                Assert.Contains(row.Id, doc);
                Assert.Contains(row.Zh, doc);
                Assert.Contains(row.En, doc);
                Assert.Contains(row.Template, doc);
            }
        }

        private static void AssertCopiedFrom(UnitDef template, UnitDef copy)
        {
            Assert.NotEqual(template.Id, copy.Id);
            Assert.NotSame(template, copy);
            Assert.Equal(template.Role, copy.Role);
            Assert.Equal(template.MaxHp, copy.MaxHp);
            Assert.Equal(template.Attack, copy.Attack);
            Assert.Equal(template.SpellPower, copy.SpellPower);
            Assert.Equal(template.Armor, copy.Armor);
            Assert.Equal(template.MagicResist, copy.MagicResist);
            Assert.Equal(template.CritRate, copy.CritRate);
            Assert.Equal(template.CritDamage, copy.CritDamage);
            Assert.Equal(template.BaseAttackInterval, copy.BaseAttackInterval);
            Assert.Equal(template.EnergyPerSecond, copy.EnergyPerSecond);
            Assert.Equal(template.MaxEnergy, copy.MaxEnergy);
            Assert.Equal(template.MoveSpeed, copy.MoveSpeed);
            Assert.Equal(template.AttackRange, copy.AttackRange);
            AssertSkill(template.Basic, copy.Basic);
            AssertSkill(template.Skill, copy.Skill);
        }

        private static void AssertSkill(SkillDef template, SkillDef copy)
        {
            Assert.NotNull(template);
            Assert.NotNull(copy);
            Assert.NotSame(template, copy);
            Assert.Equal(template.Id, copy.Id);
            Assert.Equal(template.NameKey, copy.NameKey);
            Assert.Equal(template.School, copy.School);
            Assert.Equal(template.Ratio, copy.Ratio);
            Assert.Equal(template.EnergyCost, copy.EnergyCost);
            Assert.Equal(template.CooldownSeconds, copy.CooldownSeconds);
            Assert.Equal(template.IsBasic, copy.IsBasic);
        }

        private static BattleSimulator StartWithReserve()
        {
            var sim = new BattleSimulator();
            sim.Start(M0Catalog.CreateEncounter(), GoldenSeedTests.Seed);
            UnitDef[] reserve = M0Catalog.SetupReserve();
            for (int i = 0; i < reserve.Length; i++)
                Assert.True(sim.TryAddReserve(reserve[i]));
            return sim;
        }

        private static BattleEvent Spawn(BattleSimulator sim, string slot)
        {
            BattleEvent found = FindSpawn(sim, slot);
            Assert.NotNull(found);
            return found;
        }

        private static BattleEvent FindSpawn(BattleSimulator sim, string idOrSlot)
        {
            for (int i = 0; i < sim.Events.Count; i++)
            {
                BattleEvent evt = sim.Events[i];
                if (evt.Type != BattleEvents.UnitSpawned)
                    continue;
                if (evt.Get("slot") == idOrSlot || evt.Get("unitId") == idOrSlot)
                    return evt;
            }
            return null;
        }

        private static BattleEvent Find(BattleSimulator sim, string type)
        {
            for (int i = 0; i < sim.Events.Count; i++)
            {
                if (sim.Events[i].Type == type)
                    return sim.Events[i];
            }
            return null;
        }

        private static int Count(BattleSimulator sim, string type)
        {
            int count = 0;
            for (int i = 0; i < sim.Events.Count; i++)
            {
                if (sim.Events[i].Type == type)
                    count++;
            }
            return count;
        }

        private static AllyRow Row(string id, string zh, string en, string role, string template, string spriteGuid, string basicGuid, string skillGuid)
        {
            return new AllyRow
            {
                Id = id,
                Zh = zh,
                En = en,
                Role = role,
                Template = template,
                SpriteGuid = spriteGuid,
                BasicGuid = basicGuid,
                SkillGuid = skillGuid
            };
        }

        private static decimal Field(string text, string key)
        {
            var match = Regex.Match(text, "^\\s*" + key + ":\\s*([^\\s]+)", RegexOptions.Multiline);
            Assert.True(match.Success, key);
            return decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        private static string Read(string root, string relative)
        {
            return File.ReadAllText(Path.Combine(root, relative));
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "docs", "gdd", "m0-test-arena.md")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Repo root not found.");
        }

        private static string GoldenPath()
        {
            return Path.Combine(RepoRoot(), "tests", "Echoes.Simulation.Tests", "golden", "m0-midcombat-dice.txt");
        }

        private static string Sha256(string path)
        {
            byte[] hash = SHA256.HashData(File.ReadAllBytes(path));
            var builder = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private sealed class AllyRow
        {
            public string Id;
            public string Zh;
            public string En;
            public string Role;
            public string Template;
            public string SpriteGuid;
            public string BasicGuid;
            public string SkillGuid;
        }
    }
}
