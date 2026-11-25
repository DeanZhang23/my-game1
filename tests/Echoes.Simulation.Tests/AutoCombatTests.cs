using System.Collections.Generic;
using Xunit;

namespace Echoes.Simulation.Tests
{
    public class AutoCombatTests
    {
        [Fact]
        public void PlaceholderStats_AddRoleSpeedAndRange_AndKeepTheOldPanel()
        {
            UnitDef melee = M0Catalog.Melee();
            Assert.Equal(UnitRoles.Melee, melee.Role);
            Assert.Equal(3.0m, melee.MoveSpeed);
            Assert.Equal(0.9m, melee.AttackRange);
            Assert.Equal(120, melee.MaxHp);
            Assert.Equal(18m, melee.Attack);
            Assert.Equal(1.20m, melee.BaseAttackInterval);

            UnitDef caster = M0Catalog.Caster();
            Assert.Equal(UnitRoles.Ranged, caster.Role);
            Assert.Equal(2.5m, caster.MoveSpeed);
            Assert.Equal(3.5m, caster.AttackRange);
            Assert.Equal(80, caster.MaxHp);
            Assert.Equal(22m, caster.SpellPower);
            Assert.Equal(1.50m, caster.BaseAttackInterval);

            UnitDef enemy = M0Catalog.Enemy();
            Assert.Equal(UnitRoles.Melee, enemy.Role);
            Assert.Equal(3.0m, enemy.MoveSpeed);
            Assert.Equal(0.9m, enemy.AttackRange);
            Assert.Equal(100, enemy.MaxHp);
            Assert.Equal(14m, enemy.Attack);
        }

        [Fact]
        public void FieldScale_FrontGapIs4_AndAdjacentCentersAre1_5()
        {
            decimal ax;
            decimal ay;
            decimal bx;
            decimal by;
            FieldLayout.PositionOf("ally_c2", out ax, out ay);
            FieldLayout.PositionOf("enemy_c2", out bx, out by);
            Assert.Equal(0m, ax);
            Assert.Equal(0m, ay);
            Assert.Equal(4m, bx);
            Assert.Equal(0m, by);
            Assert.Equal(4m, Dist(ax, ay, bx, by));

            FieldLayout.PositionOf("ally_c1", out bx, out by);
            Assert.Equal(1.5m, Dist(ax, ay, bx, by));
            FieldLayout.PositionOf("ally_b2", out bx, out by);
            Assert.Equal(1.5m, Dist(ax, ay, bx, by));
            FieldLayout.PositionOf("ally_a2", out bx, out by);
            Assert.Equal(-3m, bx);
            Assert.Equal(3m, Dist(ax, ay, bx, by));

            FieldLayout.PositionOf("enemy_c1", out bx, out by);
            Assert.Equal(4m, bx);
            Assert.Equal(1.5m, by);
            Assert.True(Dist(0m, 0m, bx, by) > 0.9m);

            decimal x = -9m;
            decimal y = 4m;
            FieldLayout.Clamp(ref x, ref y);
            Assert.Equal(FieldLayout.MinX, x);
            Assert.Equal(FieldLayout.MaxY, y);
            Assert.Equal(-3m, FieldLayout.MinX);
            Assert.Equal(7m, FieldLayout.MaxX);
        }

        [Fact]
        public void Targeting_NearerWins_ThenLowerHp_ThenSlotId()
        {
            BattleSimulator nearer = Begin(Allies(M0Catalog.Melee(), "ally_c2"), new List<Placement>
            {
                Enemy("enemy_c2", 100),
                Enemy("enemy_c1", 1)
            });
            Assert.Equal("enemy_c2", TargetOf(nearer, "ally_c2"));

            BattleSimulator wounded = Begin(Allies(M0Catalog.Melee(), "ally_c2"), new List<Placement>
            {
                Enemy("enemy_c1", 100),
                Enemy("enemy_c3", 50)
            });
            Assert.Equal("enemy_c3", TargetOf(wounded, "ally_c2"));

            BattleSimulator slot = Begin(Allies(M0Catalog.Melee(), "ally_c2"), new List<Placement>
            {
                Enemy("enemy_c3", 100),
                Enemy("enemy_c1", 100)
            });
            Assert.Equal("enemy_c1", TargetOf(slot, "ally_c2"));
        }

        [Fact]
        public void Retarget_OnDeathIsImmediate_AndEveryQuarterSecondWhenDistanceChanges()
        {
            UnitDef melee = M0Catalog.Melee();
            melee.AttackRange = 100m;
            BattleSimulator kill = Begin(Allies(melee, "ally_c2"), new List<Placement>
            {
                Enemy("enemy_c1", 1),
                Enemy("enemy_c2", 1)
            });
            Assert.Equal("enemy_c2", TargetOf(kill, "ally_c2"));
            kill.PumpMs(1200);
            BattleEvent switched = LastTarget(kill, "ally_c2");
            Assert.Equal("1.200", switched.Get("time"));
            Assert.Equal("enemy_c1", switched.Get("targetSlot"));

            UnitDef watcher = M0Catalog.Melee();
            watcher.MoveSpeed = 0m;
            watcher.AttackRange = 0.5m;
            watcher.Attack = 0m;
            UnitDef parked = M0Catalog.Enemy();
            parked.MoveSpeed = 0m;
            parked.MaxHp = 5000;
            UnitDef runner = M0Catalog.Enemy();
            runner.MoveSpeed = 20m;
            runner.MaxHp = 5000;
            runner.Attack = 0m;
            BattleSimulator chase = Begin(Allies(watcher, "ally_c2"), new List<Placement>
            {
                new Placement { SlotId = "enemy_b2", Unit = parked },
                new Placement { SlotId = "enemy_a1", Unit = runner }
            });
            Assert.Equal("enemy_b2", TargetOf(chase, "ally_c2"));
            chase.PumpMs(250);
            BattleEvent next = LastTarget(chase, "ally_c2");
            Assert.Equal("0.250", next.Get("time"));
            Assert.Equal("enemy_a1", next.Get("targetSlot"));
        }

        [Fact]
        public void Melee_WalksIntoRange_AndStops()
        {
            BattleSimulator sim = Begin(Allies(M0Catalog.Melee(), "ally_c2"), new List<Placement>
            {
                Enemy("enemy_c2", 5000)
            });
            CombatantView startAlly = Pose(sim, "ally_c2");
            CombatantView startEnemy = Pose(sim, "enemy_c2");
            Assert.True(Dist(startAlly.X, startAlly.Y, startEnemy.X, startEnemy.Y) > 0.9m);

            sim.PumpMs(200);
            CombatantView midAlly = Pose(sim, "ally_c2");
            CombatantView midEnemy = Pose(sim, "enemy_c2");
            decimal mid = Dist(midAlly.X, midAlly.Y, midEnemy.X, midEnemy.Y);
            Assert.True(mid < 4m);
            Assert.True(mid > 0.9m);
            Assert.True(midAlly.X > startAlly.X);
            Assert.True(midEnemy.X < startEnemy.X);
            Assert.Equal(0, Count(sim, BattleEvents.UnitEngage));

            sim.PumpMs(600);
            BattleEvent engage = Find(sim, BattleEvents.UnitEngage);
            decimal engagedAt = decimal.Parse(engage.Get("time"), System.Globalization.CultureInfo.InvariantCulture);
            Assert.InRange(engagedAt, 0.400m, 0.800m);
            CombatantView ally = Pose(sim, "ally_c2");
            CombatantView enemy = Pose(sim, "enemy_c2");
            Assert.True(Dist(ally.X, ally.Y, enemy.X, enemy.Y) <= 0.9m + 0.02m);
            Assert.True(ally.X > FieldLayout.MinX && ally.X < FieldLayout.MaxX);
        }

        [Fact]
        public void AutoCombat_DoesNotOpenATacticalWindow_AndDoesNotRollSetupDice()
        {
            UnitDef melee = M0Catalog.Melee();
            melee.MaxHp = 50000;
            melee.Attack = 1m;
            UnitDef enemy = M0Catalog.Enemy();
            enemy.MaxHp = 50000;
            enemy.Attack = 1m;
            var encounter = new EncounterDef
            {
                Id = "encounter_window_gone",
                CountdownMs = 3000,
                TimeoutMs = 20000,
                Allies = Allies(melee, "ally_c2"),
                Enemies = new List<Placement> { new Placement { SlotId = "enemy_c2", Unit = enemy } }
            };
            var sim = new BattleSimulator();
            sim.Start(encounter, 79);
            Assert.Equal(BattlePhase.Setup, sim.Phase);
            BattleEvent only = Assert.Single(sim.Events);
            Assert.Equal(BattleEvents.BattleStart, only.Type);
            sim.Commit();
            Assert.Equal(BattlePhase.Countdown, sim.Phase);
            sim.BeginAutoCombat();
            Assert.Equal(BattlePhase.AutoCombat, sim.Phase);
            sim.PumpMs(8000);
            Assert.Equal(BattlePhase.AutoCombat, sim.Phase);
            Assert.Equal(0, Count(sim, "tactical_window_opened"));
            Assert.Equal(0, Count(sim, BattleEvents.DiceRoll));
            Assert.Equal(0, Count(sim, BattleEvents.DiceLock));
            Assert.Equal(0, Count(sim, BattleEvents.DiceReroll));
            Assert.Equal(0, Count(sim, BattleEvents.DiceCommit));
        }

        [Fact]
        public void SkillClock_StillUsesEnergyAndCooldown_WithoutAPreBattleFeather()
        {
            UnitDef caster = M0Catalog.Caster();
            caster.EnergyPerSecond = 100m;
            caster.AttackRange = 100m;
            caster.MaxHp = 5000;
            UnitDef enemy = M0Catalog.Enemy();
            enemy.MaxHp = 50000;
            enemy.Attack = 1m;
            var encounter = new EncounterDef
            {
                Id = "encounter_skill_clock",
                CountdownMs = 3000,
                TimeoutMs = 20000,
                Allies = Allies(caster, "ally_a2"),
                Enemies = new List<Placement> { new Placement { SlotId = "enemy_c1", Unit = enemy } }
            };
            var sim = new BattleSimulator();
            sim.Start(encounter, 1);
            sim.Commit();
            sim.BeginAutoCombat();
            sim.PumpMs(12000);
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
            Assert.Equal("1.000", first.Get("time"));
            Assert.Equal("11.000", second.Get("time"));
        }

        [Fact]
        public void PumpSlicing_DoesNotChangeTheLog()
        {
            string chunked = GoldenSeedTests.Canonical(PlaySliced(1000));
            string fine = GoldenSeedTests.Canonical(PlaySliced(1));
            string steps = GoldenSeedTests.Canonical(PlaySliced(50));
            Assert.Equal(chunked, fine);
            Assert.Equal(chunked, steps);
        }

        private static BattleSimulator PlaySliced(int slice)
        {
            var sim = new BattleSimulator();
            sim.Start(M0Catalog.CreateEncounter(), 424242);
            sim.Commit();
            sim.BeginAutoCombat();
            int left = 2000;
            while (left > 0 && sim.Phase == BattlePhase.AutoCombat)
            {
                int step = left < slice ? left : slice;
                sim.PumpMs(step);
                left -= step;
            }
            return sim;
        }

        private static BattleSimulator Begin(List<Placement> allies, List<Placement> enemies)
        {
            var encounter = new EncounterDef
            {
                Id = "encounter_auto",
                CountdownMs = 3000,
                TimeoutMs = 150000,
                Allies = allies,
                Enemies = enemies
            };
            var sim = new BattleSimulator();
            sim.Start(encounter, 1);
            sim.Commit();
            sim.BeginAutoCombat();
            return sim;
        }

        private static List<Placement> Allies(UnitDef unit, string slot)
        {
            return new List<Placement> { new Placement { SlotId = slot, Unit = unit } };
        }

        private static Placement Enemy(string slot, int hp)
        {
            UnitDef enemy = M0Catalog.Enemy();
            enemy.MaxHp = hp;
            return new Placement { SlotId = slot, Unit = enemy };
        }

        private static string TargetOf(BattleSimulator sim, string slot)
        {
            return LastTarget(sim, slot).Get("targetSlot");
        }

        private static BattleEvent LastTarget(BattleSimulator sim, string slot)
        {
            BattleEvent found = null;
            for (int i = 0; i < sim.Events.Count; i++)
            {
                BattleEvent evt = sim.Events[i];
                if (evt.Type == BattleEvents.UnitTarget && evt.Get("slot") == slot)
                    found = evt;
            }
            Assert.NotNull(found);
            return found;
        }

        private static CombatantView Pose(BattleSimulator sim, string slot)
        {
            for (int i = 0; i < sim.CombatantCount; i++)
            {
                CombatantView view;
                Assert.True(sim.TryGetCombatant(i, out view));
                if (view.SlotId == slot)
                    return view;
            }
            Assert.Fail("Missing pose " + slot);
            return new CombatantView();
        }

        private static decimal Dist(decimal ax, decimal ay, decimal bx, decimal by)
        {
            decimal dx = ax - bx;
            decimal dy = ay - by;
            return FieldLayout.Sqrt(dx * dx + dy * dy);
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
