using System.Collections.Generic;
using Xunit;

namespace Echoes.Simulation.Tests
{
    public class CombatRulesTests
    {
        [Fact]
        public void ThreeSwords_FirstMeleeHit_IncludesKeen_NotCorrosion()
        {
            BattleEvent hit = FirstDamage(RunPreset(new[] { FaceIds.Sword, FaceIds.Sword, FaceIds.Sword }, 3, false));
            Assert.Equal("ally_c2", hit.Get("sourceSlot"));
            Assert.Equal("basic", hit.Get("kind"));
            Assert.Equal("enemy_c1", hit.Get("targetSlot"));
            int expected = ExpectedMelee(1.20m, hit.Get("crit") == "true");
            Assert.Equal(expected.ToString(), hit.Get("amount"));
        }

        [Fact]
        public void Corrosion_MultipliesBothTheLiteralExamplePath()
        {
            BattleEvent hit = FirstDamage(RunPreset(new[] { FaceIds.Sword, FaceIds.Sword, FaceIds.Sword }, 5, true));
            decimal mod = 1.20m * 1.15m;
            int expected = CombatFormulas.ApplyCrit(
                CombatFormulas.PreCrit(18m * (1m + 0.12m * 1.80m), 1m, 8m, mod),
                hit.Get("crit") == "true",
                0.50m);
            Assert.Equal(expected.ToString(), hit.Get("amount"));
            Assert.Equal("true", hit.Get("keen"));
        }

        [Fact]
        public void ShieldFace_GrantsFrontShield_ThenExpires()
        {
            var sim = RunPreset(new[] { FaceIds.Shield, FaceIds.Coin, FaceIds.Coin }, 4, false);
            BattleEvent applied = Find(sim, BattleEvents.ShieldApplied);
            Assert.Equal("ally_c2", applied.Get("slot"));
            Assert.Equal("18", applied.Get("amount"));
            BattleEvent expired = Find(sim, BattleEvents.ShieldExpired);
            Assert.Equal("8.000", expired.Get("time"));
        }

        [Fact]
        public void Attunement_OpensOneWindow_ConfirmReturnsToCombat()
        {
            EncounterDef encounter = TankEncounter(9000);
            var sim = new BattleSimulator();
            sim.Start(encounter, 1, new[] { FaceIds.Sword, FaceIds.Sword, FaceIds.Sword });
            sim.Commit();
            sim.BeginAutoCombat();
            sim.RunToEnd(TacticalDecision.Confirm);

            Assert.Equal(1, Count(sim, BattleEvents.TacticalWindowOpened));
            Assert.Equal(1, Count(sim, BattleEvents.TacticalWindowConfirmed));
            Assert.Equal(0, Count(sim, BattleEvents.TacticalWindowSkipped));
            BattleEvent end = Find(sim, BattleEvents.BattleEnd);
            Assert.Equal("defeat", end.Get("outcome"));
            Assert.Equal("timeout", end.Get("reason"));
            Assert.Equal("9.000", end.Get("duration"));
        }

        [Fact]
        public void WindowTimeout_LogsSkipped()
        {
            EncounterDef encounter = TankEncounter(9000);
            var sim = new BattleSimulator();
            sim.Start(encounter, 1, new[] { FaceIds.Star, FaceIds.Star, FaceIds.Star });
            sim.Commit();
            sim.BeginAutoCombat();
            sim.PumpMs(8000);
            Assert.Equal(BattlePhase.TacticalWindow, sim.Phase);
            sim.TickTacticalWindow(8000);
            Assert.Equal(BattlePhase.AutoCombat, sim.Phase);
            Assert.Equal(1, Count(sim, BattleEvents.TacticalWindowSkipped));
        }

        [Fact]
        public void BattleEndingBeforeTheWindow_DoesNotOpenIt()
        {
            var enemy = M0Catalog.Enemy();
            enemy.MaxHp = 1;
            var encounter = M0Catalog.CreateEncounter();
            encounter.Enemies = new List<Placement>
            {
                new Placement { SlotId = "enemy_c1", Unit = enemy },
                new Placement { SlotId = "enemy_c2", Unit = CopyEnemy(1) }
            };
            var sim = new BattleSimulator();
            sim.Start(encounter, 2, new[] { FaceIds.Sword, FaceIds.Sword, FaceIds.Sword });
            sim.Commit();
            sim.BeginAutoCombat();
            sim.RunToEnd(TacticalDecision.Skip);
            Assert.Equal(0, Count(sim, BattleEvents.TacticalWindowOpened));
            Assert.Equal("victory", Find(sim, BattleEvents.BattleEnd).Get("outcome"));
        }

        [Fact]
        public void Timeout_IsDefeat_AndCoinGoldIsWithheld()
        {
            var encounter = M0Catalog.CreateEncounter();
            encounter.TimeoutMs = 500;
            var sim = new BattleSimulator();
            sim.Start(encounter, 2, new[] { FaceIds.Coin, FaceIds.Coin, FaceIds.Coin });
            sim.Commit();
            sim.BeginAutoCombat();
            sim.RunToEnd(TacticalDecision.Skip);
            BattleEvent end = Find(sim, BattleEvents.BattleEnd);
            Assert.Equal("defeat", end.Get("outcome"));
            Assert.Equal("timeout", end.Get("reason"));
            Assert.Equal("0", end.Get("gold"));
            Assert.Equal("0.500", end.Get("duration"));
        }

        [Fact]
        public void CoinGold_IsPaidOnlyWhenTheBattleIsWon()
        {
            var encounter = M0Catalog.CreateEncounter();
            encounter.Enemies = new List<Placement>
            {
                new Placement { SlotId = "enemy_c1", Unit = CopyEnemy(1) },
                new Placement { SlotId = "enemy_c2", Unit = CopyEnemy(1) }
            };
            var sim = new BattleSimulator();
            sim.Start(encounter, 2, new[] { FaceIds.Coin, FaceIds.Coin, FaceIds.Coin });
            sim.Commit();
            sim.BeginAutoCombat();
            sim.RunToEnd(TacticalDecision.Skip);
            BattleEvent end = Find(sim, BattleEvents.BattleEnd);
            Assert.Equal("victory", end.Get("outcome"));
            Assert.Equal("10", end.Get("gold"));
        }

        [Fact]
        public void SkullAttunement_SacrificesCurrentHp_AndLeavesOne()
        {
            var sim = new BattleSimulator();
            sim.Start(M0Catalog.CreateEncounter(), 1, new[] { FaceIds.Skull, FaceIds.Skull, FaceIds.Skull });
            sim.Commit();
            Assert.Equal("120", Spawn(sim, "ally_c2").Get("hp"));
            Assert.Equal("-10", Loss(sim, "ally_c2").Get("delta"));
            Assert.Equal("110", Loss(sim, "ally_c2").Get("hp"));
            Assert.Equal("73", Loss(sim, "ally_a2").Get("hp"));
        }

        [Fact]
        public void FrontAndBackEffects_SkipTheMiddleRow()
        {
            var encounter = M0Catalog.CreateEncounter();
            encounter.Allies = new List<Placement>
            {
                new Placement { SlotId = "ally_c2", Unit = M0Catalog.Melee() },
                new Placement { SlotId = "ally_b2", Unit = M0Catalog.Melee() },
                new Placement { SlotId = "ally_a2", Unit = M0Catalog.Caster() },
                new Placement { SlotId = "ally_b1", Unit = M0Catalog.Caster() }
            };
            var sim = new BattleSimulator();
            sim.Start(encounter, 1, new[] { FaceIds.Sword, FaceIds.Shield, FaceIds.Star });
            sim.Commit();
            Assert.Equal("1", Spawn(sim, "ally_c2").Get("keen"));
            Assert.Equal("0", Spawn(sim, "ally_b2").Get("keen"));
            Assert.Equal("20", Spawn(sim, "ally_a2").Get("energy"));
            Assert.Equal("0", Spawn(sim, "ally_b1").Get("energy"));
            sim.BeginAutoCombat();
            Assert.Equal("ally_c2", Find(sim, BattleEvents.ShieldApplied).Get("slot"));
            Assert.Equal(1, Count(sim, BattleEvents.ShieldApplied));
        }

        [Fact]
        public void ChecklistSeed79_IsShieldAttunement_AndOpensTheWindow()
        {
            var sim = new BattleSimulator();
            sim.Start(M0Catalog.CreateEncounter(), 79);
            Assert.Equal(FaceIds.Shield, sim.Face(0));
            Assert.Equal(FaceIds.Shield, sim.Face(1));
            Assert.Equal(FaceIds.Shield, sim.Face(2));
            sim.Commit();
            Assert.Equal(ComboIds.Attunement, sim.ComboId);
            sim.BeginAutoCombat();
            sim.RunToEnd(TacticalDecision.Confirm);
            Assert.Equal(1, Count(sim, BattleEvents.TacticalWindowOpened));
            Assert.Equal(1, Count(sim, BattleEvents.TacticalWindowConfirmed));
            Assert.NotNull(Find(sim, BattleEvents.BattleEnd));
        }

        [Fact]
        public void ChecklistSeed223_IsThreeSwords()
        {
            var sim = new BattleSimulator();
            sim.Start(M0Catalog.CreateEncounter(), 223);
            Assert.Equal(FaceIds.Sword, sim.Face(0));
            Assert.Equal(FaceIds.Sword, sim.Face(1));
            Assert.Equal(FaceIds.Sword, sim.Face(2));
        }

        [Fact]
        public void Feather_ReducesTheFirstSkillCooldown()
        {
            var caster = M0Catalog.Caster();
            caster.EnergyPerSecond = 100m;
            caster.Skill.EnergyCost = 0m;
            var encounter = M0Catalog.CreateEncounter();
            encounter.Allies = new List<Placement>
            {
                new Placement { SlotId = "ally_a2", Unit = caster }
            };
            encounter.Enemies = new List<Placement>
            {
                new Placement { SlotId = "enemy_c1", Unit = CopyEnemy(5000) }
            };
            encounter.TimeoutMs = 20000;
            var sim = new BattleSimulator();
            sim.Start(encounter, 1, new[] { FaceIds.Feather, FaceIds.Feather, FaceIds.Feather });
            sim.Commit();
            sim.BeginAutoCombat();
            sim.RunToEnd(TacticalDecision.Skip);
            BattleEvent first = null;
            BattleEvent second = null;
            for (int i = 0; i < sim.Events.Count; i++)
            {
                BattleEvent evt = sim.Events[i];
                if (evt.Type != BattleEvents.SkillCast || evt.Get("skillId") != SkillIds.Bolt)
                    continue;
                if (first == null)
                    first = evt;
                else
                {
                    second = evt;
                    break;
                }
            }
            Assert.NotNull(second);
            Assert.Equal("0.000", first.Get("time"));
            Assert.Equal("8.200", second.Get("time"));
        }

        private static int ExpectedMelee(decimal mod, bool crit)
        {
            decimal attack = 18m * (1m + 0.12m * 1.80m);
            return CombatFormulas.ApplyCrit(CombatFormulas.PreCrit(attack, 1m, 8m, mod), crit, 0.50m);
        }

        private static BattleSimulator RunPreset(string[] faces, int seed, bool corrosion)
        {
            var sim = new BattleSimulator();
            sim.Start(M0Catalog.CreateEncounter(), seed, faces);
            if (corrosion)
                sim.SetCrackStacksForTest(3);
            sim.Commit();
            sim.BeginAutoCombat();
            sim.RunToEnd(TacticalDecision.Skip);
            return sim;
        }

        private static EncounterDef TankEncounter(int timeoutMs)
        {
            var melee = M0Catalog.Melee();
            melee.MaxHp = 5000;
            melee.Attack = 1m;
            var enemy = M0Catalog.Enemy();
            enemy.MaxHp = 5000;
            enemy.Attack = 1m;
            return new EncounterDef
            {
                Id = "encounter_rules",
                CountdownMs = 3000,
                TacticalWindowDelayMs = 8000,
                TacticalWindowDurationMs = 8000,
                TimeoutMs = timeoutMs,
                Allies = new List<Placement>
                {
                    new Placement { SlotId = "ally_c2", Unit = melee },
                    new Placement { SlotId = "ally_a2", Unit = M0Catalog.Caster() }
                },
                Enemies = new List<Placement>
                {
                    new Placement { SlotId = "enemy_c1", Unit = enemy },
                    new Placement { SlotId = "enemy_c2", Unit = CopyEnemy(5000) }
                }
            };
        }

        private static UnitDef CopyEnemy(int hp)
        {
            var enemy = M0Catalog.Enemy();
            enemy.MaxHp = hp;
            return enemy;
        }

        private static BattleEvent FirstDamage(BattleSimulator sim)
        {
            return Find(sim, BattleEvents.Damage);
        }

        private static BattleEvent Find(BattleSimulator sim, string type)
        {
            for (int i = 0; i < sim.Events.Count; i++)
            {
                if (sim.Events[i].Type == type)
                    return sim.Events[i];
            }
            Assert.Fail("Missing event " + type);
            return null;
        }

        private static BattleEvent Spawn(BattleSimulator sim, string slot)
        {
            for (int i = 0; i < sim.Events.Count; i++)
            {
                BattleEvent evt = sim.Events[i];
                if (evt.Type == BattleEvents.UnitSpawned && evt.Get("slot") == slot)
                    return evt;
            }
            Assert.Fail("Missing spawn " + slot);
            return null;
        }

        private static BattleEvent Loss(BattleSimulator sim, string slot)
        {
            for (int i = 0; i < sim.Events.Count; i++)
            {
                BattleEvent evt = sim.Events[i];
                if (evt.Type == BattleEvents.HealOrHpLoss && evt.Get("slot") == slot)
                    return evt;
            }
            Assert.Fail("Missing hp loss " + slot);
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
    }
}
