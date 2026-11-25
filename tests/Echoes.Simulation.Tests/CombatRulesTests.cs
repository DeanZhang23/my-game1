using System.Collections.Generic;
using Xunit;

namespace Echoes.Simulation.Tests
{
    public class CombatRulesTests
    {
        [Fact]
        public void FirstMeleeHit_UsesTheUnmodifiedFormula_OnTheNearestEnemy()
        {
            var sim = new BattleSimulator();
            sim.Start(M0Catalog.CreateEncounter(), 3);
            sim.Commit();
            sim.BeginAutoCombat();
            sim.RunToEnd();
            BattleEvent hit = Find(sim, BattleEvents.Damage);
            Assert.Equal("ally_c2", hit.Get("sourceSlot"));
            Assert.Equal("basic", hit.Get("kind"));
            Assert.Equal("enemy_c2", hit.Get("targetSlot"));
            Assert.Equal("false", hit.Get("keen"));
            int expected = CombatFormulas.ApplyCrit(
                CombatFormulas.PreCrit(18m, 1m, 8m, 1m),
                hit.Get("crit") == "true",
                0.50m);
            Assert.Equal(expected.ToString(), hit.Get("amount"));
            Assert.Equal(0, Count(sim, "tactical_window_opened"));
            Assert.Equal(0, Count(sim, "tactical_window_confirmed"));
            Assert.Equal(0, Count(sim, "tactical_window_skipped"));
        }

        [Fact]
        public void Timeout_IsDefeat_AndPaysNoGold()
        {
            var encounter = M0Catalog.CreateEncounter();
            encounter.TimeoutMs = 500;
            var sim = new BattleSimulator();
            sim.Start(encounter, 2);
            sim.Commit();
            sim.BeginAutoCombat();
            sim.RunToEnd();
            BattleEvent end = Find(sim, BattleEvents.BattleEnd);
            Assert.Equal("defeat", end.Get("outcome"));
            Assert.Equal("timeout", end.Get("reason"));
            Assert.Equal("0", end.Get("gold"));
            Assert.Equal("0.500", end.Get("duration"));
        }

        [Fact]
        public void Victory_PaysNoCoinGold_UntilMidCombatDiceExist()
        {
            var encounter = M0Catalog.CreateEncounter();
            encounter.Enemies = new List<Placement>
            {
                new Placement { SlotId = "enemy_c1", Unit = CopyEnemy(1) },
                new Placement { SlotId = "enemy_c2", Unit = CopyEnemy(1) }
            };
            var sim = new BattleSimulator();
            sim.Start(encounter, 2);
            sim.Commit();
            sim.BeginAutoCombat();
            sim.RunToEnd();
            BattleEvent end = Find(sim, BattleEvents.BattleEnd);
            Assert.Equal("victory", end.Get("outcome"));
            Assert.Equal("enemy_wiped", end.Get("reason"));
            Assert.Equal("0", end.Get("gold"));
        }

        private static UnitDef CopyEnemy(int hp)
        {
            UnitDef enemy = M0Catalog.Enemy();
            enemy.MaxHp = hp;
            return enemy;
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
