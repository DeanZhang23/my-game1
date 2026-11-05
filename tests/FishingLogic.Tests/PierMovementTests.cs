using NiaIsland.Pier;
using Xunit;

namespace NiaIsland.Tests
{
    public class PierMovementTests
    {
        [Fact]
        public void MovesAndClampsInsidePier()
        {
            Assert.Equal(2f, PierMovement.Step(0f, 1f, 4f, 0.5f, -5.2f, 5.2f));
            Assert.Equal(-2f, PierMovement.Step(0f, -1f, 4f, 0.5f, -5.2f, 5.2f));
            Assert.Equal(-5.2f, PierMovement.Step(-5f, -1f, 4f, 1f, -5.2f, 5.2f));
            Assert.Equal(5.2f, PierMovement.Step(5f, 1f, 4f, 1f, -5.2f, 5.2f));
        }

        [Fact]
        public void ZeroAxisOrTime_StaysPut()
        {
            Assert.Equal(1.25f, PierMovement.Step(1.25f, 0f, 4f, 1f, -5f, 5f));
            Assert.Equal(1.25f, PierMovement.Step(1.25f, 1f, 4f, 0f, -5f, 5f));
            Assert.Equal(1.25f, PierMovement.Step(1.25f, 1f, 4f, -2f, -5f, 5f));
        }

        [Fact]
        public void SwappedBounds_StillClamp()
        {
            Assert.Equal(1f, PierMovement.Step(0f, 1f, 10f, 1f, 1f, -1f));
            Assert.Equal(-1f, PierMovement.Step(0f, -1f, 10f, 1f, 1f, -1f));
        }
    }
}
