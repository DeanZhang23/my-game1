using Xunit;

namespace Echoes.Simulation.Tests
{
    public class DiceAndComboTests
    {
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
    }
}
