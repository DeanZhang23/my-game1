using Xunit;

namespace Echoes.Simulation.Tests
{
    public class DiceAndComboTests
    {
        [Fact]
        public void Rerolls_FreeThenCrackCap_CorrosionOnTheFourthPaid()
        {
            var sim = new BattleSimulator();
            sim.Start(M0Catalog.CreateEncounter(), 7);

            sim.Reroll();
            Assert.Equal(0, sim.Crack);
            Assert.False(sim.CorrosionActive);
            Assert.Equal(0, sim.FreeRerollsRemaining);

            sim.Reroll();
            Assert.Equal(1, sim.Crack);
            Assert.False(sim.CorrosionActive);

            sim.Reroll();
            Assert.Equal(2, sim.Crack);

            sim.Reroll();
            Assert.Equal(3, sim.Crack);
            Assert.True(sim.CorrosionActive);

            sim.Reroll();
            Assert.Equal(3, sim.Crack);
            Assert.True(sim.CorrosionActive);

            int cracks = 0;
            for (int i = 0; i < sim.Events.Count; i++)
            {
                if (sim.Events[i].Type == BattleEvents.CrackGained)
                    cracks++;
            }
            Assert.Equal(3, cracks);
        }

        [Fact]
        public void Reroll_WhenEveryDieIsLocked_DoesNotSpendFreeOrCrack()
        {
            var sim = new BattleSimulator();
            sim.Start(M0Catalog.CreateEncounter(), 11);
            string a = sim.Face(0);
            string b = sim.Face(1);
            string c = sim.Face(2);
            sim.SetLock(0, true);
            sim.SetLock(1, true);
            sim.SetLock(2, true);

            sim.Reroll();

            Assert.Equal(a, sim.Face(0));
            Assert.Equal(b, sim.Face(1));
            Assert.Equal(c, sim.Face(2));
            Assert.Equal(1, sim.FreeRerollsRemaining);
            Assert.Equal(0, sim.Crack);
            Assert.Equal("true", FindLast(sim, BattleEvents.DiceReroll).Get("ineffective"));
        }

        [Fact]
        public void LockedDie_Stays_AndIneffectiveReroll_DoesNotConsumeRng()
        {
            const int seed = 99;
            var expected = new DeterministicRng(seed);
            string die0 = FaceIds.Order[expected.NextInt(6)];
            string die1 = FaceIds.Order[expected.NextInt(6)];
            string die2 = FaceIds.Order[expected.NextInt(6)];

            var sim = new BattleSimulator();
            sim.Start(M0Catalog.CreateEncounter(), seed);
            Assert.Equal(die0, sim.Face(0));
            Assert.Equal(die1, sim.Face(1));
            Assert.Equal(die2, sim.Face(2));

            sim.SetLock(0, true);
            sim.Reroll();
            string rolled1 = FaceIds.Order[expected.NextInt(6)];
            string rolled2 = FaceIds.Order[expected.NextInt(6)];
            Assert.Equal(die0, sim.Face(0));
            Assert.Equal(rolled1, sim.Face(1));
            Assert.Equal(rolled2, sim.Face(2));

            var stalled = new BattleSimulator();
            stalled.Start(M0Catalog.CreateEncounter(), seed);
            stalled.SetLock(0, true);
            stalled.SetLock(1, true);
            stalled.SetLock(2, true);
            stalled.Reroll();
            stalled.SetLock(1, false);
            stalled.Reroll();

            var afterWaste = new DeterministicRng(seed);
            afterWaste.NextInt(6);
            afterWaste.NextInt(6);
            afterWaste.NextInt(6);
            string onlyDie1 = FaceIds.Order[afterWaste.NextInt(6)];
            Assert.Equal(die0, stalled.Face(0));
            Assert.Equal(onlyDie1, stalled.Face(1));
            Assert.Equal(die2, stalled.Face(2));
        }

        [Theory]
        [InlineData("face_sword", "face_sword", "face_sword", "combo_attunement", "face_sword", "1.8")]
        [InlineData("face_sword", "face_sword", "face_shield", "combo_twin", "face_sword", "1.35")]
        [InlineData("face_sword", "face_shield", "face_star", "combo_none", "none", "1")]
        public void Combos_FollowTheHighestTier(string a, string b, string c, string combo, string face, string mult)
        {
            FaceResolution resolved = FaceEffects.Resolve(a, b, c);
            Assert.Equal(combo, resolved.ComboId);
            Assert.Equal(face, resolved.ComboFace);
            Assert.Equal(decimal.Parse(mult), resolved.Coefficient);
        }

        [Fact]
        public void ThreeSwords_ScaleAttackOnce_AndStackKeenPerDie()
        {
            FaceResolution resolved = FaceEffects.Resolve(FaceIds.Sword, FaceIds.Sword, FaceIds.Sword);
            Assert.Equal(1m + 0.12m * 1.80m, resolved.AttackMultiplier);
            Assert.Equal(3, resolved.KeenPerFront);
            Assert.Equal(0, resolved.WildTokens);
        }

        [Fact]
        public void TwinSwords_ScaleAttack_AndLeaveTheThirdFaceUnscaled()
        {
            FaceResolution resolved = FaceEffects.Resolve(FaceIds.Sword, FaceIds.Sword, FaceIds.Shield);
            Assert.Equal(1m + 0.12m * 1.35m, resolved.AttackMultiplier);
            Assert.Equal(2, resolved.KeenPerFront);
            Assert.Equal(8m, resolved.ArmorBonus);
            Assert.Equal(0.15m, resolved.ShieldLifeRatio);
        }

        [Fact]
        public void WildToken_FillsTheEarliestFaceThatReachesTheHighestTier()
        {
            FaceResolution attune = FaceEffects.Resolve(FaceIds.Sword, FaceIds.Sword, FaceIds.Skull);
            Assert.Equal(ComboIds.Attunement, attune.ComboId);
            Assert.Equal(FaceIds.Sword, attune.ComboFace);
            Assert.Equal(1, attune.WildUsed);
            Assert.Equal(FaceIds.Sword, attune.WildFace);
            Assert.Equal(2, attune.KeenPerFront);
            Assert.Equal(0.05m, attune.SacrificeFraction);

            FaceResolution twin = FaceEffects.Resolve(FaceIds.Sword, FaceIds.Shield, FaceIds.Skull);
            Assert.Equal(ComboIds.Twin, twin.ComboId);
            Assert.Equal(FaceIds.Sword, twin.ComboFace);
            Assert.Equal(1, twin.WildUsed);

            FaceResolution skulls = FaceEffects.Resolve(FaceIds.Skull, FaceIds.Skull, FaceIds.Skull);
            Assert.Equal(ComboIds.Attunement, skulls.ComboId);
            Assert.Equal(FaceIds.Skull, skulls.ComboFace);
            Assert.Equal(0, skulls.WildUsed);
            Assert.Equal(3, skulls.WildTokens - skulls.WildUsed);
            Assert.Equal(0.05m * 1.80m, skulls.SacrificeFraction);
            Assert.Equal(10, FaceEffects.Resolve(FaceIds.Coin, FaceIds.Coin, FaceIds.Coin).Gold);
        }

        private static BattleEvent FindLast(BattleSimulator sim, string type)
        {
            BattleEvent found = null;
            for (int i = 0; i < sim.Events.Count; i++)
            {
                if (sim.Events[i].Type == type)
                    found = sim.Events[i];
            }
            Assert.NotNull(found);
            return found;
        }
    }
}
