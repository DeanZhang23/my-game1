using System;
using Xunit;

namespace Echoes.Simulation.Tests
{
    public class FormationTests
    {
        private static readonly string[] PositionFaces =
        {
            FaceIds.Sword, FaceIds.Shield, FaceIds.Star
        };

        [Fact]
        public void DefaultLineup_IsTheE1Roster_UntilSetupMovesIt()
        {
            var encounter = M0Catalog.CreateEncounter();
            var sim = new BattleSimulator();
            sim.Start(encounter, 1, PositionFaces);

            Assert.Equal(2, sim.AllyRosterCount);
            Assert.Equal("ally_c2", sim.AllySlotOf(UnitIds.Melee));
            Assert.Equal("ally_a2", sim.AllySlotOf(UnitIds.Caster));
            Assert.Equal(2, sim.PlacedAllyCount);
            Assert.True(sim.CanCommit);

            int events = sim.Events.Count;
            Assert.True(sim.TrySetAllySlot(UnitIds.Melee, "ally_c1"));
            Assert.Equal(events, sim.Events.Count);
            Assert.Equal("ally_c2", encounter.Allies[0].SlotId);
            Assert.Equal("ally_a2", encounter.Allies[1].SlotId);

            encounter.Allies[0].SlotId = "ally_b1";
            Assert.Equal("ally_c1", sim.AllySlotOf(UnitIds.Melee));

            sim.Commit();
            Assert.Equal(UnitIds.Melee, Spawn(sim, "ally_c1").Get("unitId"));
            Assert.Equal(UnitIds.Caster, Spawn(sim, "ally_a2").Get("unitId"));
            Assert.Equal(UnitIds.Enemy, Spawn(sim, "enemy_c1").Get("unitId"));
            Assert.Equal(UnitIds.Enemy, Spawn(sim, "enemy_c2").Get("unitId"));
        }

        [Fact]
        public void FrontAndBack_GetPositionEffects_MidRowKeepsTeamEffectsOnly()
        {
            Assert.Equal(FormationRow.Front, Slots.RowOf("ally_c1"));
            Assert.Equal(FormationRow.Back, Slots.RowOf("ally_a3"));
            Assert.Equal(FormationRow.Mid, Slots.RowOf("ally_b2"));
            Assert.Equal(FormationRow.Front, Slots.RowOf("enemy_c1"));
            Assert.False(Slots.IsAllySlot("enemy_c1"));
            Assert.False(Slots.IsAllySlot("ally_c4"));

            BattleSimulator placed = Start();
            Assert.True(placed.TrySetAllySlot(UnitIds.Melee, "ally_c1"));
            Assert.True(placed.TrySetAllySlot(UnitIds.Caster, "ally_a3"));
            placed.Commit();

            BattleEvent melee = Spawn(placed, "ally_c1");
            BattleEvent caster = Spawn(placed, "ally_a3");
            Assert.Equal("front", melee.Get("row"));
            Assert.Equal("1", melee.Get("keen"));
            Assert.Equal("0", melee.Get("energy"));
            Assert.Equal("back", caster.Get("row"));
            Assert.Equal("0", caster.Get("keen"));
            Assert.Equal("20", caster.Get("energy"));
            Assert.Equal(EventFormat.Dec(18m * 1.12m), melee.Get("attack"));
            Assert.Equal(EventFormat.Dec(16m), melee.Get("armor"));
            placed.BeginAutoCombat();
            placed.PumpMs(1200);
            Assert.Equal("ally_c1", First(placed, BattleEvents.Damage).Get("sourceSlot"));
            Assert.Equal(1, Count(placed, BattleEvents.ShieldApplied));
            BattleEvent shield = First(placed, BattleEvents.ShieldApplied);
            Assert.Equal("ally_c1", shield.Get("slot"));
            Assert.Equal("18", shield.Get("amount"));

            BattleSimulator mid = Start();
            Assert.True(mid.TrySetAllySlot(UnitIds.Melee, "ally_b2"));
            Assert.True(mid.TrySetAllySlot(UnitIds.Caster, "ally_b1"));
            Assert.Equal("ally_b1", mid.AllySlotOf(UnitIds.Caster));
            Assert.Equal("ally_b2", mid.AllySlotOf(UnitIds.Melee));
            mid.Commit();

            BattleEvent midMelee = Spawn(mid, "ally_b2");
            BattleEvent midCaster = Spawn(mid, "ally_b1");
            Assert.Equal("mid", midMelee.Get("row"));
            Assert.Equal("0", midMelee.Get("keen"));
            Assert.Equal("0", midMelee.Get("energy"));
            Assert.Equal(EventFormat.Dec(18m * 1.12m), midMelee.Get("attack"));
            Assert.Equal(EventFormat.Dec(16m), midMelee.Get("armor"));
            Assert.Equal("mid", midCaster.Get("row"));
            Assert.Equal("0", midCaster.Get("keen"));
            Assert.Equal("0", midCaster.Get("energy"));
            Assert.Equal(EventFormat.Dec(4m * 1.12m), midCaster.Get("attack"));
            Assert.Equal(EventFormat.Dec(10m), midCaster.Get("armor"));
            mid.BeginAutoCombat();
            Assert.Equal(0, Count(mid, BattleEvents.ShieldApplied));
        }

        [Fact]
        public void EmptyFormation_CannotMarch_AndOneAllyCan()
        {
            BattleSimulator sim = Start();
            Assert.True(sim.TrySetAllySlot(UnitIds.Melee, null));
            Assert.True(sim.TrySetAllySlot(UnitIds.Caster, ""));
            Assert.Equal(2, sim.AllyRosterCount);
            Assert.Equal(0, sim.PlacedAllyCount);
            Assert.False(sim.CanCommit);
            Assert.Null(sim.AllySlotOf(UnitIds.Melee));
            Assert.Null(sim.AllySlotOf(UnitIds.Caster));

            sim.Reroll();
            int events = sim.Events.Count;
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => sim.Commit());
            Assert.Contains("ally", error.Message, StringComparison.Ordinal);
            Assert.Equal(BattlePhase.Setup, sim.Phase);
            Assert.Equal(events, sim.Events.Count);
            Assert.Equal(0, Count(sim, BattleEvents.DiceCommit));

            Assert.True(sim.TrySetAllySlot(UnitIds.Caster, "ally_b2"));
            Assert.Equal(1, sim.PlacedAllyCount);
            Assert.True(sim.CanCommit);
            sim.Commit();
            Assert.Equal(BattlePhase.Countdown, sim.Phase);
            Assert.Equal(UnitIds.Caster, Spawn(sim, "ally_b2").Get("unitId"));
            Assert.Equal("0", Spawn(sim, "ally_b2").Get("energy"));
            Assert.Equal("0", Spawn(sim, "ally_b2").Get("keen"));
            Assert.Null(FindSpawn(sim, "ally_c2"));
        }

        [Fact]
        public void Swap_StaysOneUnitPerCell_AndRejectsEnemySlots()
        {
            BattleSimulator sim = Start();
            Assert.True(sim.TrySetAllySlot(UnitIds.Melee, "ally_a2"));
            Assert.Equal("ally_a2", sim.AllySlotOf(UnitIds.Melee));
            Assert.Equal("ally_c2", sim.AllySlotOf(UnitIds.Caster));
            Assert.Equal(2, sim.PlacedAllyCount);

            Assert.False(sim.TrySetAllySlot(UnitIds.Melee, "enemy_c1"));
            Assert.False(sim.TrySetAllySlot(UnitIds.Melee, "ally_c9"));
            Assert.False(sim.TrySetAllySlot("unit_missing", "ally_b1"));
            Assert.Equal("ally_a2", sim.AllySlotOf(UnitIds.Melee));
            Assert.Equal("ally_c2", sim.AllySlotOf(UnitIds.Caster));
        }

        [Fact]
        public void Formation_FreezesFromCountdownOnward()
        {
            BattleSimulator sim = Start();
            sim.Commit();
            Assert.Equal(BattlePhase.Countdown, sim.Phase);
            Assert.False(sim.CanCommit);
            Assert.False(sim.TrySetAllySlot(UnitIds.Melee, "ally_b2"));
            Assert.False(sim.TrySetAllySlot(UnitIds.Caster, null));
            Assert.Equal("ally_c2", sim.AllySlotOf(UnitIds.Melee));
            Assert.Equal("ally_a2", sim.AllySlotOf(UnitIds.Caster));

            sim.BeginAutoCombat();
            Assert.Equal(BattlePhase.AutoCombat, sim.Phase);
            Assert.False(sim.TrySetAllySlot(UnitIds.Melee, "ally_a1"));
            Assert.Equal("ally_c2", sim.AllySlotOf(UnitIds.Melee));
            Assert.Equal("ally_c2", Spawn(sim, "ally_c2").Get("slot"));
        }

        private static BattleSimulator Start()
        {
            var sim = new BattleSimulator();
            sim.Start(M0Catalog.CreateEncounter(), 1, PositionFaces);
            return sim;
        }

        private static BattleEvent Spawn(BattleSimulator sim, string slot)
        {
            BattleEvent found = FindSpawn(sim, slot);
            Assert.NotNull(found);
            return found;
        }

        private static BattleEvent FindSpawn(BattleSimulator sim, string slot)
        {
            for (int i = 0; i < sim.Events.Count; i++)
            {
                BattleEvent evt = sim.Events[i];
                if (evt.Type == BattleEvents.UnitSpawned && evt.Get("slot") == slot)
                    return evt;
            }
            return null;
        }

        private static BattleEvent First(BattleSimulator sim, string type)
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
