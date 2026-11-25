using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace Echoes.Simulation.Tests
{
    public class ContentParityTests
    {
        [Fact]
        public void ScriptableObjects_MatchTheM0Numbers()
        {
            string root = RepoRoot();
            string melee = Read(root, "Assets/Resources/M0/Units/unit_placeholder_melee.asset");
            Assert.Equal(120m, Field(melee, "maxHp"));
            Assert.Equal(18m, Field(melee, "attack"));
            Assert.Equal(0m, Field(melee, "spellPower"));
            Assert.Equal(8m, Field(melee, "armor"));
            Assert.Equal(4m, Field(melee, "magicResist"));
            Assert.Equal(0.05m, Field(melee, "critRate"));
            Assert.Equal(0.50m, Field(melee, "critDamage"));
            Assert.Equal(1.20m, Field(melee, "baseAttackInterval"));
            Assert.Equal(8m, Field(melee, "energyPerSecond"));
            Assert.Equal(100m, Field(melee, "maxEnergy"));
            Assert.Contains("id: unit_placeholder_melee", melee);

            string caster = Read(root, "Assets/Resources/M0/Units/unit_placeholder_caster.asset");
            Assert.Equal(80m, Field(caster, "maxHp"));
            Assert.Equal(4m, Field(caster, "attack"));
            Assert.Equal(22m, Field(caster, "spellPower"));
            Assert.Equal(2m, Field(caster, "armor"));
            Assert.Equal(8m, Field(caster, "magicResist"));
            Assert.Equal(0.05m, Field(caster, "critRate"));
            Assert.Equal(0.50m, Field(caster, "critDamage"));
            Assert.Equal(1.50m, Field(caster, "baseAttackInterval"));
            Assert.Equal(10m, Field(caster, "energyPerSecond"));

            string enemy = Read(root, "Assets/Resources/M0/Units/unit_placeholder_enemy.asset");
            Assert.Equal(100m, Field(enemy, "maxHp"));
            Assert.Equal(14m, Field(enemy, "attack"));
            Assert.Equal(8m, Field(enemy, "armor"));
            Assert.Contains("skill: {fileID: 0}", enemy);

            string cleave = Read(root, "Assets/Resources/M0/Skills/skill_placeholder_cleave.asset");
            Assert.Equal(1.6m, Field(cleave, "ratio"));
            Assert.Equal(100m, Field(cleave, "energyCost"));
            Assert.Equal(8m, Field(cleave, "cooldownSeconds"));
            string bolt = Read(root, "Assets/Resources/M0/Skills/skill_placeholder_bolt.asset");
            Assert.Equal(1.8m, Field(bolt, "ratio"));
            Assert.Equal(10m, Field(bolt, "cooldownSeconds"));
            string casterBasic = Read(root, "Assets/Resources/M0/Skills/skill_placeholder_caster_basic.asset");
            Assert.Equal(0.5m, Field(casterBasic, "ratio"));
            Assert.Equal(1m, Field(casterBasic, "school"));

            string encounter = Read(root, "Assets/Resources/M0/M0TestEncounter.asset");
            Assert.Contains("id: encounter_m0_test", encounter);
            Assert.Equal(3m, Field(encounter, "countdownSeconds"));
            Assert.Equal(8m, Field(encounter, "tacticalWindowDelaySeconds"));
            Assert.Equal(8m, Field(encounter, "tacticalWindowDurationSeconds"));
            Assert.Equal(150m, Field(encounter, "timeoutSeconds"));
            Assert.Contains("slotId: ally_c2", encounter);
            Assert.Contains("slotId: ally_a2", encounter);
            Assert.Contains("slotId: enemy_c1", encounter);
            Assert.Contains("slotId: enemy_c2", encounter);

            string faces = Read(root, "Assets/Resources/M0/M0FaceCatalog.asset");
            int sword = faces.IndexOf("face_sword", StringComparison.Ordinal);
            int shield = faces.IndexOf("face_shield", StringComparison.Ordinal);
            int star = faces.IndexOf("face_star", StringComparison.Ordinal);
            int feather = faces.IndexOf("face_feather", StringComparison.Ordinal);
            int coin = faces.IndexOf("face_coin", StringComparison.Ordinal);
            int skull = faces.IndexOf("face_skull", StringComparison.Ordinal);
            Assert.True(sword >= 0 && sword < shield && shield < star && star < feather && feather < coin && coin < skull);
        }

        [Fact]
        public void Localization_UsesOfficialDisplayNames()
        {
            string root = RepoRoot();
            string loc = Read(root, "Assets/Resources/M0/M0Localization.asset");
            int melee = loc.IndexOf("key: unit.placeholder_melee.name", StringComparison.Ordinal);
            int caster = loc.IndexOf("key: unit.placeholder_caster.name", StringComparison.Ordinal);
            int enemy = loc.IndexOf("key: unit.placeholder_enemy.name", StringComparison.Ordinal);
            int next = loc.IndexOf("key: skill.placeholder_melee_basic.name", StringComparison.Ordinal);
            Assert.True(melee >= 0 && melee < caster && caster < enemy && enemy < next);

            string meleeBlock = loc.Substring(melee, caster - melee);
            string casterBlock = loc.Substring(caster, enemy - caster);
            string enemyBlock = loc.Substring(enemy, next - enemy);
            Assert.Contains("zhHans: \"灰烬卫士\"", meleeBlock);
            Assert.Contains("en: \"Ashguard\"", meleeBlock);
            Assert.Contains("zhHans: \"裂隙术士\"", casterBlock);
            Assert.Contains("en: \"Riftmage\"", casterBlock);
            Assert.Contains("zhHans: \"锈蚀步卒\"", enemyBlock);
            Assert.Contains("en: \"Rustsoldier\"", enemyBlock);

            string meleeUnit = Read(root, "Assets/Resources/M0/Units/unit_placeholder_melee.asset");
            string casterUnit = Read(root, "Assets/Resources/M0/Units/unit_placeholder_caster.asset");
            string enemyUnit = Read(root, "Assets/Resources/M0/Units/unit_placeholder_enemy.asset");
            Assert.Contains("id: unit_placeholder_melee", meleeUnit);
            Assert.Contains("nameKey: unit.placeholder_melee.name", meleeUnit);
            Assert.Contains("id: unit_placeholder_caster", casterUnit);
            Assert.Contains("nameKey: unit.placeholder_caster.name", casterUnit);
            Assert.Contains("id: unit_placeholder_enemy", enemyUnit);
            Assert.Contains("nameKey: unit.placeholder_enemy.name", enemyUnit);
        }

        [Fact]
        public void UnitSprites_MapTheDefaultRosterToOfficialIdles()
        {
            string root = RepoRoot();
            string catalog = Read(root, "Assets/Resources/M0/M0UnitSprites.asset");
            int melee = catalog.IndexOf("unitId: unit_placeholder_melee", StringComparison.Ordinal);
            int caster = catalog.IndexOf("unitId: unit_placeholder_caster", StringComparison.Ordinal);
            int enemy = catalog.IndexOf("unitId: unit_placeholder_enemy", StringComparison.Ordinal);
            Assert.True(melee >= 0 && melee < caster && caster < enemy);
            Assert.Contains("guid: e1d00001e1d00001e1d00001e1d00001", catalog.Substring(melee, caster - melee));
            Assert.Contains("guid: e1d00004e1d00004e1d00004e1d00004", catalog.Substring(caster, enemy - caster));
            Assert.Contains("guid: e1d00009e1d00009e1d00009e1d00009", catalog.Substring(enemy));

            string scene = Read(root, "Assets/Scenes/TestArena.unity");
            Assert.Contains("guid: e1b00024e1b00024e1b00024e1b00024", scene);
        }

        [Fact]
        public void OfficialAllyIdles_UseTheUsageImportSettings()
        {
            string root = RepoRoot();
            string[] ids =
            {
                "ashguard", "oathblade", "lampsister", "riftmage",
                "misthunter", "boneapostle", "chainjudge", "bloodacolyte"
            };
            for (int i = 0; i < ids.Length; i++)
            {
                string meta = Read(root, "Assets/_Project/Art/Characters/chr_ally_" + ids[i] + "_idle_01.png.meta");
                Assert.Contains("textureType: 8", meta);
                Assert.Contains("spritePixelsToUnits: 64", meta);
                Assert.Contains("filterMode: 1", meta);
                Assert.Contains("alignment: 9", meta);
                Assert.Contains("spritePivot: {x: 0.5, y: 0.125}", meta);
                Assert.Contains("textureCompression: 0", meta);
                Assert.Contains("alphaIsTransparency: 1", meta);
            }

            string enemy = Read(root, "Assets/_Project/Art/Characters/chr_enemy_placeholder_idle.png.meta");
            Assert.Contains("textureType: 8", enemy);
            Assert.Contains("spritePixelsToUnits: 64", enemy);
            Assert.Contains("filterMode: 1", enemy);
            Assert.Contains("spritePivot: {x: 0.5, y: 0.15}", enemy);
        }

        [Fact]
        public void ScriptsAndScenes_DoNotHardcodePlayerFacingCopy()
        {
            string root = RepoRoot();
            var cjk = new Regex(@"\p{IsCJKUnifiedIdeographs}");
            string[] files = Directory.GetFiles(Path.Combine(root, "Assets"), "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                if (!file.EndsWith(".cs") && !file.EndsWith(".unity"))
                    continue;
                string text = File.ReadAllText(file);
                Assert.False(cjk.IsMatch(text), file);
            }
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
    }
}
