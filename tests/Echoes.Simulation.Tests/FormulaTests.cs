using Xunit;

namespace Echoes.Simulation.Tests
{
    public class FormulaTests
    {
        [Fact]
        public void Section86_UnmodifiedPhysicalHit_FloorsTo16()
        {
            int damage = CombatFormulas.PreCrit(18m, 1.0m, 8m, 1.0m);
            Assert.Equal(16, damage);
        }

        [Fact]
        public void Section86_Crit_Floors16Times150_To24()
        {
            int pre = CombatFormulas.PreCrit(18m, 1.0m, 8m, 1.0m);
            Assert.Equal(16, pre);
            Assert.Equal(24, CombatFormulas.ApplyCrit(pre, true, 0.50m));
            Assert.Equal(16, CombatFormulas.ApplyCrit(pre, false, 0.50m));
        }

        [Fact]
        public void Section86_TwinSwordPanelStaysFloat_UntilFinalFloor()
        {
            decimal attack = 18m * (1m + 0.12m * 1.35m);
            Assert.Equal(20.916m, attack);
            Assert.Equal(19, CombatFormulas.PreCrit(attack, 1m, 8m, 1m));
        }

        [Fact]
        public void MagicHit_UsesSpellPowerAndResist()
        {
            int pre = CombatFormulas.PreCrit(22m, 0.5m, 4m, 1m);
            Assert.Equal(10, pre);
            Assert.Equal(15, CombatFormulas.ApplyCrit(pre, true, 0.50m));
        }

        [Fact]
        public void Damage_HasAFloorOfOne_AndNegativeDefenseClamps()
        {
            Assert.Equal(1, CombatFormulas.PreCrit(0m, 1m, 100m, 1m));
            Assert.Equal(18, CombatFormulas.PreCrit(18m, 1m, 0m, 1m));
            Assert.Equal(18, CombatFormulas.PreCrit(18m, 1m, -20m, 1m));
        }

        [Fact]
        public void AttackInterval_FeatherBonus_AndMinimum()
        {
            Assert.Equal(1.20m / 1.15m, CombatFormulas.AttackIntervalSeconds(1.20m, 0.15m));
            Assert.Equal(0.35m, CombatFormulas.AttackIntervalSeconds(1.20m, 100m));
            Assert.Equal(1200, CombatFormulas.IntervalMs(1.20m, 0m));
            Assert.Equal(1043, CombatFormulas.IntervalMs(1.20m, 0.15m));
            Assert.Equal(350, CombatFormulas.IntervalMs(0.10m, 0m));
        }

        [Fact]
        public void Shield_AbsorbsBeforeHp_AndCannotGoNegative()
        {
            int shield = 20;
            int hp = 100;
            int absorbed;
            int hpLost;
            CombatFormulas.ApplyShieldThenHp(ref shield, ref hp, 16, out absorbed, out hpLost);
            Assert.Equal(16, absorbed);
            Assert.Equal(0, hpLost);
            Assert.Equal(4, shield);
            Assert.Equal(100, hp);

            CombatFormulas.ApplyShieldThenHp(ref shield, ref hp, 10, out absorbed, out hpLost);
            Assert.Equal(4, absorbed);
            Assert.Equal(6, hpLost);
            Assert.Equal(0, shield);
            Assert.Equal(94, hp);
        }

        [Fact]
        public void Sacrifice_LeavesAtLeastOneHp()
        {
            Assert.Equal(6, CombatFormulas.SacrificeLoss(120, 0.05m));
            Assert.Equal(10, CombatFormulas.SacrificeLoss(120, 0.09m));
            Assert.Equal(0, CombatFormulas.SacrificeLoss(1, 0.05m));
            Assert.Equal(4, CombatFormulas.SacrificeLoss(5, 0.90m));
        }

        [Fact]
        public void Gold_FloorsComboScaledCoin()
        {
            Assert.Equal(6, CombatFormulas.Gold(6m));
            Assert.Equal(8, CombatFormulas.Gold(6m * 1.35m));
            Assert.Equal(10, CombatFormulas.Gold(6m * 1.80m));
        }
    }
}
