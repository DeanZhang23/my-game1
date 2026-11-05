using System.Collections.Generic;
using NiaIsland.Fishing;
using Xunit;

namespace NiaIsland.Tests
{
    public class FishingStateMachineTests
    {
        static FishingTiming DefaultTiming()
        {
            return new FishingTiming(0.45f, 1.15f, 1f, 0.7f);
        }

        static List<FishingState> Watch(FishingStateMachine machine)
        {
            var seen = new List<FishingState>();
            machine.Entered += seen.Add;
            return seen;
        }

        static void ReachBite(FishingStateMachine machine, FishingTiming timing)
        {
            Assert.True(machine.PressFishButton());
            Assert.Equal(FishingState.Cast, machine.State);
            machine.Tick(timing.CastDuration);
            Assert.Equal(FishingState.Waiting, machine.State);
            machine.Tick(timing.WaitingDuration);
            Assert.Equal(FishingState.BiteWindow, machine.State);
        }

        [Fact]
        public void StartsReady_AndIdleTickStaysReady()
        {
            var machine = new FishingStateMachine(DefaultTiming());
            machine.Tick(5f);
            Assert.Equal(FishingState.Ready, machine.State);
        }

        [Fact]
        public void HalfBiteWindow_StaysOpen()
        {
            var timing = DefaultTiming();
            var machine = new FishingStateMachine(timing);
            ReachBite(machine, timing);
            machine.Tick(timing.BiteWindowDuration * 0.5f);
            Assert.Equal(FishingState.BiteWindow, machine.State);
        }

        [Fact]
        public void NoTapDuringBiteWindow_MissThenReady()
        {
            var timing = DefaultTiming();
            var machine = new FishingStateMachine(timing);
            var seen = Watch(machine);

            ReachBite(machine, timing);
            machine.Tick(timing.BiteWindowDuration);
            Assert.Equal(FishingState.Miss, machine.State);
            machine.Tick(timing.ResultHoldDuration);
            Assert.Equal(FishingState.Ready, machine.State);

            Assert.Equal(
                new[]
                {
                    FishingState.Cast,
                    FishingState.Waiting,
                    FishingState.BiteWindow,
                    FishingState.Miss,
                    FishingState.Ready
                },
                seen);
        }

        [Fact]
        public void TapDuringBiteWindow_ResolvedThenReady_AndCanCastAgain()
        {
            var timing = DefaultTiming();
            var machine = new FishingStateMachine(timing);
            var seen = Watch(machine);

            ReachBite(machine, timing);
            Assert.True(machine.PressFishButton());
            Assert.Equal(FishingState.Resolved, machine.State);
            Assert.False(machine.PressFishButton());
            machine.Tick(timing.ResultHoldDuration);
            Assert.Equal(FishingState.Ready, machine.State);
            Assert.True(machine.PressFishButton());
            Assert.Equal(FishingState.Cast, machine.State);

            Assert.DoesNotContain(FishingState.Miss, seen);
            Assert.Contains(FishingState.Resolved, seen);
        }

        [Fact]
        public void ButtonDoesNothingOutsideReadyAndBiteWindow()
        {
            var timing = DefaultTiming();
            var machine = new FishingStateMachine(timing);
            Assert.True(machine.PressFishButton());
            Assert.False(machine.PressFishButton());
            Assert.Equal(FishingState.Cast, machine.State);

            machine.Tick(timing.CastDuration);
            Assert.False(machine.PressFishButton());
            Assert.Equal(FishingState.Waiting, machine.State);

            machine.Tick(timing.WaitingDuration);
            machine.Tick(timing.BiteWindowDuration);
            Assert.Equal(FishingState.Miss, machine.State);
            Assert.False(machine.PressFishButton());
        }

        [Fact]
        public void BiteWindowDuration_IsReadOnEachTick()
        {
            var timing = new FishingTiming(0.2f, 0.2f, 0.8f, 0.2f);
            var shortWindow = new FishingStateMachine(timing);
            ReachBite(shortWindow, timing);
            shortWindow.Tick(0.8f);
            Assert.Equal(FishingState.Miss, shortWindow.State);

            var longTiming = new FishingTiming(0.2f, 0.2f, 1.2f, 0.2f);
            var longWindow = new FishingStateMachine(longTiming);
            ReachBite(longWindow, longTiming);
            longWindow.Tick(0.5f);
            Assert.Equal(FishingState.BiteWindow, longWindow.State);

            // Same idea as editing the Inspector during Play: the open window uses the new length.
            longTiming.BiteWindowDuration = 0.5f;
            longWindow.Tick(0.01f);
            Assert.Equal(FishingState.Miss, longWindow.State);
        }

        [Fact]
        public void LargeTick_OnMissPath_ReturnsToReady()
        {
            var timing = DefaultTiming();
            var machine = new FishingStateMachine(timing);
            var seen = Watch(machine);
            Assert.True(machine.PressFishButton());
            var total = timing.CastDuration
                + timing.WaitingDuration
                + timing.BiteWindowDuration
                + timing.ResultHoldDuration;
            machine.Tick(total + 0.05f);
            Assert.Equal(FishingState.Ready, machine.State);
            Assert.Equal(
                new[]
                {
                    FishingState.Cast,
                    FishingState.Waiting,
                    FishingState.BiteWindow,
                    FishingState.Miss,
                    FishingState.Ready
                },
                seen);
        }

        [Fact]
        public void NegativeDelta_DoesNotLeaveCast()
        {
            var machine = new FishingStateMachine(DefaultTiming());
            machine.PressFishButton();
            machine.Tick(-1f);
            Assert.Equal(FishingState.Cast, machine.State);
            Assert.Equal(0f, machine.TimeInState);
        }
    }
}
