using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Echoes.Simulation.Tests
{
    public class MidCombatDiceTests
    {
        [Fact]
        public void DamageRemainder_KeepsTheLeftoverAcrossHits()
        {
            var pool = new FatePool();
            FateGrant first = pool.AddDamage(16);
            Assert.Equal(1, first.Gained);
            Assert.Equal(1, pool.Pool);
            Assert.Equal(6, pool.DamageRemainder);
            FateGrant second = pool.AddDamage(16);
            Assert.Equal(2, second.Gained);
            Assert.Equal(3, pool.Pool);
            Assert.Equal(2, pool.DamageRemainder);

            decimal chunked = 0m;
            var stepped = new FatePool();
            for (int i = 0; i < 20; i++)
                stepped.AddSkillEnergy(ref chunked, 0.4m);
            Assert.Equal(1, stepped.Pool);
            Assert.Equal(3m, chunked);
            decimal whole = 0m;
            var once = new FatePool();
            once.AddSkillEnergy(ref whole, 8m);
            Assert.Equal(1, once.Pool);
            Assert.Equal(3m, whole);
        }

        [Fact]
        public void AlliedHits_KeepDamageRemainder_AndIgnoreEnemyDamage()
        {
            BattleSimulator sim = Begin(
                Allies(Melee(5000, 18m, 0m, 100m), "ally_c2"),
                Foes("enemy_c2", 5000, 14m, 100m));
            sim.PumpMs(1200);
            Assert.Equal(1, sim.FateEnergy);
            Assert.Equal(6, sim.DamageFateRemainder);
            sim.PumpMs(1200);
            Assert.Equal(3, sim.FateEnergy);
            Assert.Equal(2, sim.DamageFateRemainder);

            BattleEvent second = LastFate(sim, FateSources.Damage);
            Assert.Equal("2", second.Get("gained"));
            Assert.Equal("2", second.Get("remainder"));
            Assert.Equal("3", second.Get("pool"));
            Assert.Equal("ally_c2", second.Get("slot"));
            Assert.Equal(0, CountFate(sim, FateSources.Kill));
            Assert.Equal(0, CountEnemyFate(sim));
        }

        [Fact]
        public void KillBonus_IsSeparateFromTheDamageRemainder()
        {
            BattleSimulator sim = Begin(
                Allies(Melee(5000, 18m, 0m, 100m), "ally_c2"),
                Foes("enemy_c2", 16, 1m, 100m));
            sim.PumpMs(1200);
            Assert.Equal(16, sim.FateEnergy);
            Assert.Equal(6, sim.DamageFateRemainder);
            BattleEvent kill = LastFate(sim, FateSources.Kill);
            Assert.Equal("15", kill.Get("gained"));
            Assert.Equal("ally_c2", kill.Get("slot"));
            Assert.Null(kill.Get("remainder"));
        }

        [Fact]
        public void SkillEnergy_GrantsOneFatePerFive_PerAlly_NotForEnemies()
        {
            UnitDef melee = Melee(5000, 1m, 8m, 0.1m);
            melee.MoveSpeed = 0m;
            UnitDef enemy = FoeUnit(5000, 1m, 0.1m);
            enemy.MoveSpeed = 0m;
            BattleSimulator sim = Begin(
                Allies(melee, "ally_c2"),
                new List<Placement> { new Placement { SlotId = "enemy_c2", Unit = enemy } });
            sim.PumpMs(1000);
            Assert.Equal(1, sim.FateEnergy);
            Assert.Equal(3m, sim.SkillFateRemainder("ally_c2"));
            Assert.Equal(0, CountEnemyFate(sim));
            sim.PumpMs(1000);
            Assert.Equal(3, sim.FateEnergy);
            Assert.Equal(1m, sim.SkillFateRemainder("ally_c2"));
        }

        [Fact]
        public void Fate_StartsAtZero_AndDoesNotGrowBeforeAutoCombat()
        {
            var sim = new BattleSimulator();
            sim.Start(M0Catalog.CreateEncounter(), 3);
            sim.SetFateEnergy(40);
            Assert.False(sim.CanRoll);
            Assert.Throws<InvalidOperationException>(() => sim.Roll());
            sim.Commit();
            sim.PumpMs(5000);
            Assert.Equal(BattlePhase.Countdown, sim.Phase);
            Assert.Equal(0, sim.NowMs);
            Assert.Equal(40, sim.FateEnergy);
            Assert.False(sim.CanRoll);
            sim.BeginAutoCombat();
            Assert.Equal(0, sim.FateEnergy);
            Assert.Equal(BattlePhase.AutoCombat, sim.Phase);
        }

        [Fact]
        public void Energy49_CannotRoll_50SpendsToZero_And56LeavesTheRest()
        {
            BattleSimulator low = HitFor(490m);
            Assert.Equal(49, low.FateEnergy);
            Assert.False(low.CanRoll);
            Assert.Throws<InvalidOperationException>(() => low.Roll());
            Assert.Equal(49, low.FateEnergy);
            Assert.Equal(0, Count(low, BattleEvents.DiceRoll));

            BattleSimulator exact = HitFor(500m);
            Assert.True(exact.CanRoll);
            exact.Roll();
            Assert.Equal(0, exact.FateEnergy);
            Assert.True(exact.HasRolled);
            Assert.False(exact.CanRoll);
            BattleEvent roll = Find(exact, BattleEvents.DiceRoll);
            Assert.Equal("50", roll.Get("cost"));
            Assert.Equal("0", roll.Get("pool"));
            Assert.Equal(0, FaceIds.IndexOf(roll.Get("die0")) >= 0 ? 0 : 1);
            Assert.True(FaceIds.IndexOf(roll.Get("die1")) >= 0);
            Assert.True(FaceIds.IndexOf(roll.Get("die2")) >= 0);

            BattleSimulator extra = HitFor(560m);
            extra.RollKnown(FaceIds.Sword, FaceIds.Shield, FaceIds.Feather);
            Assert.Equal(6, extra.FateEnergy);
            int pool = extra.FateEnergy;
            extra.Confirm();
            Assert.Equal(pool, extra.FateEnergy);
            BattleEvent commit = Find(extra, BattleEvents.DiceCommit);
            Assert.Equal("0", commit.Get("cost"));
            Assert.Equal("6", commit.Get("pool"));
            Assert.Equal("player", commit.Get("reason"));
        }

        [Fact]
        public void Overflow_StopsAt100_AndASecondRollIsRefused()
        {
            BattleSimulator sim = HitFor(2000m);
            Assert.Equal(100, sim.FateEnergy);
            BattleEvent grant = LastFate(sim, FateSources.Damage);
            Assert.Equal("200", grant.Get("gained"));
            Assert.Equal("100", grant.Get("added"));
            Assert.Equal("100", grant.Get("discarded"));
            sim.Roll();
            Assert.Equal(50, sim.FateEnergy);
            sim.SetFateEnergy(100);
            Assert.False(sim.CanRoll);
            Assert.Throws<InvalidOperationException>(() => sim.Roll());
            Assert.Equal(100, sim.FateEnergy);
            Assert.Equal(0, Count(sim, BattleEvents.DiceReroll));
            Assert.Equal(0, Count(sim, BattleEvents.DiceLock));
            Assert.Equal(1, Count(sim, BattleEvents.DiceRoll));
        }

        [Fact]
        public void ThereIsNoRerollOrLockEntryPoint()
        {
            MethodInfo[] methods = typeof(BattleSimulator).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            for (int i = 0; i < methods.Length; i++)
            {
                string name = methods[i].Name.ToLowerInvariant();
                Assert.DoesNotContain("reroll", name);
                Assert.DoesNotContain("lock", name);
                Assert.DoesNotContain("crack", name);
                Assert.DoesNotContain("tactical", name);
            }
        }

        [Fact]
        public void M0Faces_UseRoleTargets_NotHeroLinks()
        {
            FaceResolution mixed = FaceEffects.Resolve(FaceIds.Sword, FaceIds.Shield, FaceIds.Star);
            Assert.Equal(EffectTargets.AllMelee, Effect(mixed, FaceIds.Sword).TargetKind);
            Assert.Equal(EffectTargets.AllMelee, Effect(mixed, FaceIds.Shield).TargetKind);
            Assert.Equal(EffectTargets.AllRanged, Effect(mixed, FaceIds.Star).TargetKind);
            Assert.Null(Effect(mixed, FaceIds.Sword).HeroId);
            Assert.Null(Effect(mixed, FaceIds.Sword).HeroId2);

            FaceResolution team = FaceEffects.Resolve(FaceIds.Feather, FaceIds.Coin, FaceIds.Skull);
            Assert.Equal(EffectTargets.AllAllies, Effect(team, FaceIds.Feather).TargetKind);
            Assert.Equal(EffectTargets.Battle, Effect(team, FaceIds.Coin).TargetKind);
            Assert.Equal(EffectTargets.AllAllies, Effect(team, FaceIds.Skull).TargetKind);
        }

        [Fact]
        public void SwordAndStar_BuffOnlyTheMatchingLivingAllies()
        {
            BattleSimulator sim = Begin(DefaultAllies(), DefaultFoes());
            sim.SetFateEnergy(50);
            sim.RollKnown(FaceIds.Sword, FaceIds.Shield, FaceIds.Star);
            Assert.Equal(0, sim.FateEnergy);
            sim.Confirm();

            CombatantView melee = Pose(sim, "ally_c2");
            CombatantView caster = Pose(sim, "ally_a2");
            CombatantView enemy = Pose(sim, "enemy_c2");
            Assert.Equal(18m * 1.12m, melee.Attack);
            Assert.Equal(16m, melee.Armor);
            Assert.Equal(1, melee.Keen);
            Assert.Equal(18, melee.Shield);
            Assert.Equal(0m, melee.Energy);
            Assert.Equal(1m, melee.SkillDamageMultiplier);
            Assert.Equal(4m, caster.Attack);
            Assert.Equal(2m, caster.Armor);
            Assert.Equal(0, caster.Keen);
            Assert.Equal(0, caster.Shield);
            Assert.Equal(20m, caster.Energy);
            Assert.Equal(1.10m, caster.SkillDamageMultiplier);
            Assert.Equal(14m, enemy.Attack);
            Assert.Equal(8m, enemy.Armor);
            Assert.Equal(0, enemy.Keen);
            Assert.Equal(0, enemy.Shield);

            Assert.Equal(4, sim.FateEnergy);
            BattleEvent commit = Find(sim, BattleEvents.DiceCommit);
            Assert.Equal("0", commit.Get("cost"));
            Assert.Equal("0", commit.Get("pool"));
            Assert.Equal("ally_a2,ally_c2", commit.Get("affected"));
            Assert.DoesNotContain("enemy_", commit.Get("affected"));
            BattleEvent combo = Find(sim, BattleEvents.ComboResolved);
            Assert.Equal("ally_c2", combo.Get("sword"));
            Assert.Equal("ally_c2", combo.Get("shield"));
            Assert.Equal("ally_a2", combo.Get("star"));
            Assert.Equal(UnitIds.Melee, combo.Get("swordUnits"));
            Assert.Equal(UnitIds.Caster, combo.Get("starUnits"));
            BattleEvent shield = Find(sim, BattleEvents.ShieldApplied);
            Assert.Equal("ally_c2", shield.Get("slot"));
            Assert.Equal("18", shield.Get("amount"));
            Assert.Equal("8.000", shield.Get("expire"));
        }

        [Fact]
        public void Sword_FizzlesWithoutLivingMelee_AndTheComboStillResolves()
        {
            BattleSimulator sim = Begin(
                Allies(M0Catalog.Caster(), "ally_a2"),
                Foes("enemy_c2", 5000, 1m, 0.9m));
            sim.SetFateEnergy(50);
            sim.RollKnown(FaceIds.Sword, FaceIds.Sword, FaceIds.Sword);
            sim.Confirm();
            CombatantView caster = Pose(sim, "ally_a2");
            Assert.Equal(4m, caster.Attack);
            Assert.Equal(0, caster.Keen);
            BattleEvent combo = Find(sim, BattleEvents.ComboResolved);
            Assert.Equal(ComboIds.Attunement, combo.Get("combo"));
            Assert.Equal(FaceIds.Sword, combo.Get("face"));
            Assert.Equal("1.8", combo.Get("coefficient"));
            Assert.Equal("", combo.Get("sword"));
            Assert.Equal("", combo.Get("swordUnits"));
            BattleEvent commit = Find(sim, BattleEvents.DiceCommit);
            Assert.Equal("", commit.Get("affected"));
            Assert.Equal("", commit.Get("units"));
            Assert.Equal(1m, Pose(sim, "enemy_c2").Attack);
            Assert.Equal(0, Pose(sim, "enemy_c2").Keen);
        }

        [Fact]
        public void DeadAllies_DoNotReceiveNewUnitEffects()
        {
            UnitDef fragile = Melee(1, 18m, 0m, 100m);
            UnitDef sturdy = Melee(500, 18m, 0m, 100m);
            var allies = new List<Placement>
            {
                new Placement { SlotId = "ally_c1", Unit = fragile },
                new Placement { SlotId = "ally_c2", Unit = sturdy }
            };
            BattleSimulator sim = Begin(allies, Foes("enemy_c1", 5000, 50m, 100m));
            sim.PumpMs(1200);
            Assert.False(Pose(sim, "ally_c1").Alive);
            sim.SetFateEnergy(50);
            sim.RollKnown(FaceIds.Sword, FaceIds.Coin, FaceIds.Feather);
            sim.Confirm();
            Assert.Equal(18m, Pose(sim, "ally_c1").Attack);
            Assert.Equal(0, Pose(sim, "ally_c1").Keen);
            Assert.Equal(18m * 1.12m, Pose(sim, "ally_c2").Attack);
            Assert.Equal(1, Pose(sim, "ally_c2").Keen);
            Assert.Equal("ally_c2", Find(sim, BattleEvents.DiceCommit).Get("affected"));
            Assert.Equal(50m, Pose(sim, "enemy_c1").Attack);
        }

        [Fact]
        public void Sacrifice_LeavesAtLeastOneHp_AndSkipsEnemies()
        {
            UnitDef glass = Melee(1, 1m, 0m, 100m);
            UnitDef tank = Melee(120, 1m, 0m, 100m);
            var allies = new List<Placement>
            {
                new Placement { SlotId = "ally_c1", Unit = glass },
                new Placement { SlotId = "ally_c2", Unit = tank }
            };
            BattleSimulator sim = Begin(allies, Foes("enemy_c2", 500, 1m, 0.1m));
            sim.SetFateEnergy(50);
            sim.RollKnown(FaceIds.Skull, FaceIds.Coin, FaceIds.Feather);
            sim.Confirm();
            Assert.Equal(1, Pose(sim, "ally_c1").Hp);
            Assert.Equal(114, Pose(sim, "ally_c2").Hp);
            Assert.Equal(500, Pose(sim, "enemy_c2").Hp);
            Assert.Equal("ally_c1,ally_c2", Find(sim, BattleEvents.DiceCommit).Get("affected"));
        }

        [Fact]
        public void Feather_ShortensTheFirstSkillCooldownOnce()
        {
            UnitDef caster = M0Catalog.Caster();
            caster.EnergyPerSecond = 100m;
            caster.AttackRange = 100m;
            caster.MaxHp = 5000;
            caster.CritRate = 0m;
            BattleSimulator sim = Begin(
                Allies(caster, "ally_a2"),
                Foes("enemy_c1", 50000, 1m, 100m));
            sim.SetFateEnergy(50);
            sim.RollKnown(FaceIds.Feather, FaceIds.Coin, FaceIds.Shield);
            sim.Confirm();
            Assert.Equal(CombatFormulas.IntervalMs(1.50m, 0.15m), Pose(sim, "ally_a2").IntervalMs);
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
            Assert.Equal("10.000", second.Get("time"));
        }

        [Fact]
        public void Combat_KeepsMovingAndAttackingThroughTheRoll()
        {
            BattleSimulator sim = Begin(
                Allies(M0Catalog.Melee(), "ally_c2"),
                Foes("enemy_c2", 5000, 1m, 0.9m));
            sim.PumpMs(100);
            CombatantView ally = Pose(sim, "ally_c2");
            CombatantView enemy = Pose(sim, "enemy_c2");
            int now = sim.NowMs;
            sim.SetFateEnergy(50);
            sim.Roll();
            Assert.Equal(now, sim.NowMs);
            Assert.Equal(ally.X, Pose(sim, "ally_c2").X);
            Assert.Equal(BattlePhase.AutoCombat, sim.Phase);
            Assert.False(sim.DiceCommitted);
            sim.PumpMs(150);
            CombatantView walked = Pose(sim, "ally_c2");
            CombatantView chased = Pose(sim, "enemy_c2");
            Assert.True(walked.X > ally.X);
            Assert.True(chased.X < enemy.X);
            Assert.False(sim.DiceCommitted);
            Assert.Equal(BattlePhase.AutoCombat, sim.Phase);
            sim.Confirm();
            sim.PumpMs(150);
            Assert.True(Pose(sim, "ally_c2").X > walked.X);
            Assert.True(Pose(sim, "enemy_c2").X < chased.X);
            Assert.Equal(BattlePhase.AutoCombat, sim.Phase);
            Assert.True(sim.NowMs > now);
        }

        [Fact]
        public void UnconfirmedRoll_CommitsOnTheSixthSecond_WithoutPausing()
        {
            UnitDef melee = Melee(50000, 1m, 0m, 100m);
            BattleSimulator sim = Begin(
                Allies(melee, "ally_c2"),
                Foes("enemy_c2", 50000, 1m, 100m),
                30000);
            sim.SetFateEnergy(50);
            sim.RollKnown(FaceIds.Coin, FaceIds.Feather, FaceIds.Shield);
            sim.PumpMs(5999);
            Assert.False(sim.DiceCommitted);
            Assert.Equal(BattlePhase.AutoCombat, sim.Phase);
            sim.PumpMs(1);
            Assert.True(sim.DiceCommitted);
            BattleEvent commit = Find(sim, BattleEvents.DiceCommit);
            Assert.Equal("timeout", commit.Get("reason"));
            Assert.Equal("6.000", commit.Get("time"));
            Assert.Equal("0", commit.Get("cost"));
            Assert.Equal("0", commit.Get("pool"));
            Assert.Equal(BattlePhase.AutoCombat, sim.Phase);
            Assert.Equal(0, sim.FateEnergy);
            Assert.True(Count(sim, BattleEvents.Damage) > 0);
            Assert.Equal(0, Count(sim, BattleEvents.DiceReroll));
            Assert.Equal(0, Count(sim, "tactical_window_opened"));
            Assert.Equal(0, Count(sim, BattleEvents.CrackGained));
        }

        [Fact]
        public void BattleEnd_ConfirmsFirst_AndCoinGoldRequiresVictory()
        {
            UnitDef melee = Melee(5000, 200m, 0m, 100m);
            BattleSimulator win = Begin(
                Allies(melee, "ally_c2"),
                new List<Placement>
                {
                    new Placement { SlotId = "enemy_c1", Unit = FoeUnit(1, 1m, 100m) },
                    new Placement { SlotId = "enemy_c2", Unit = FoeUnit(1, 1m, 100m) }
                });
            win.SetFateEnergy(50);
            win.RollKnown(FaceIds.Coin, FaceIds.Sword, FaceIds.Feather);
            Assert.False(win.DiceCommitted);
            win.RunToEnd();
            BattleEvent commit = Find(win, BattleEvents.DiceCommit);
            BattleEvent end = Find(win, BattleEvents.BattleEnd);
            Assert.Equal("resolve", commit.Get("reason"));
            Assert.True(commit.Seq < end.Seq);
            Assert.Equal("victory", end.Get("outcome"));
            Assert.Equal("6", end.Get("gold"));

            BattleSimulator loss = Begin(
                Allies(M0Catalog.Melee(), "ally_c2"),
                Foes("enemy_c2", 5000, 1m, 0.9m),
                200);
            loss.SetFateEnergy(50);
            loss.RollKnown(FaceIds.Coin, FaceIds.Sword, FaceIds.Feather);
            loss.RunToEnd();
            BattleEvent defeat = Find(loss, BattleEvents.BattleEnd);
            Assert.Equal("defeat", defeat.Get("outcome"));
            Assert.Equal("0", defeat.Get("gold"));
            Assert.Equal("resolve", Find(loss, BattleEvents.DiceCommit).Get("reason"));
        }

        private static BattleSimulator HitFor(decimal attack)
        {
            UnitDef melee = Melee(5000, attack, 0m, 100m);
            UnitDef enemy = FoeUnit(50000, 1m, 100m);
            enemy.Armor = 0m;
            BattleSimulator sim = Begin(
                Allies(melee, "ally_c2"),
                new List<Placement> { new Placement { SlotId = "enemy_c2", Unit = enemy } });
            sim.PumpMs(1200);
            return sim;
        }

        private static BattleSimulator Begin(List<Placement> allies, List<Placement> enemies)
        {
            return Begin(allies, enemies, 150000);
        }

        private static BattleSimulator Begin(List<Placement> allies, List<Placement> enemies, int timeoutMs)
        {
            var encounter = new EncounterDef
            {
                Id = "encounter_fate",
                CountdownMs = 3000,
                TimeoutMs = timeoutMs,
                Allies = allies,
                Enemies = enemies
            };
            var sim = new BattleSimulator();
            sim.Start(encounter, 1);
            sim.Commit();
            sim.BeginAutoCombat();
            return sim;
        }

        private static List<Placement> DefaultAllies()
        {
            return new List<Placement>
            {
                new Placement { SlotId = "ally_c2", Unit = M0Catalog.Melee() },
                new Placement { SlotId = "ally_a2", Unit = M0Catalog.Caster() }
            };
        }

        private static List<Placement> DefaultFoes()
        {
            return new List<Placement>
            {
                new Placement { SlotId = "enemy_c1", Unit = M0Catalog.Enemy() },
                new Placement { SlotId = "enemy_c2", Unit = M0Catalog.Enemy() }
            };
        }

        private static List<Placement> Allies(UnitDef unit, string slot)
        {
            return new List<Placement> { new Placement { SlotId = slot, Unit = unit } };
        }

        private static List<Placement> Foes(string slot, int hp, decimal attack, decimal range)
        {
            return new List<Placement> { new Placement { SlotId = slot, Unit = FoeUnit(hp, attack, range) } };
        }

        private static UnitDef Melee(int hp, decimal attack, decimal energyPerSecond, decimal range)
        {
            UnitDef unit = M0Catalog.Melee();
            unit.MaxHp = hp;
            unit.Attack = attack;
            unit.CritRate = 0m;
            unit.EnergyPerSecond = energyPerSecond;
            unit.AttackRange = range;
            return unit;
        }

        private static UnitDef FoeUnit(int hp, decimal attack, decimal range)
        {
            UnitDef unit = M0Catalog.Enemy();
            unit.MaxHp = hp;
            unit.Attack = attack;
            unit.CritRate = 0m;
            unit.AttackRange = range;
            return unit;
        }

        private static FaceEffectSpec Effect(FaceResolution resolved, string faceId)
        {
            for (int i = 0; i < resolved.Effects.Length; i++)
            {
                if (resolved.Effects[i].FaceId == faceId)
                    return resolved.Effects[i];
            }
            Assert.Fail("Missing effect " + faceId);
            return null;
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

        private static BattleEvent LastFate(BattleSimulator sim, string source)
        {
            BattleEvent found = null;
            for (int i = 0; i < sim.Events.Count; i++)
            {
                BattleEvent evt = sim.Events[i];
                if (evt.Type == BattleEvents.FateEnergy && evt.Get("source") == source)
                    found = evt;
            }
            Assert.NotNull(found);
            return found;
        }

        private static int CountFate(BattleSimulator sim, string source)
        {
            int count = 0;
            for (int i = 0; i < sim.Events.Count; i++)
            {
                BattleEvent evt = sim.Events[i];
                if (evt.Type == BattleEvents.FateEnergy && evt.Get("source") == source)
                    count++;
            }
            return count;
        }

        private static int CountEnemyFate(BattleSimulator sim)
        {
            int count = 0;
            for (int i = 0; i < sim.Events.Count; i++)
            {
                BattleEvent evt = sim.Events[i];
                if (evt.Type != BattleEvents.FateEnergy)
                    continue;
                string slot = evt.Get("slot");
                if (slot != null && slot.StartsWith("enemy_", StringComparison.Ordinal))
                    count++;
            }
            return count;
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
