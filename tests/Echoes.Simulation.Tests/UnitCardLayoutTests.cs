using System;
using System.IO;
using Echoes.Presentation;
using Xunit;

namespace Echoes.Simulation.Tests
{
    public class UnitCardLayoutTests
    {
        [Fact]
        public void CardStaysOnItsSlotWhileTheSpriteFollowsTheBody()
        {
            const float slotX = -128f;
            const float slotY = 0f;
            UnitLayerPlacement home = UnitCardLayout.Place(slotX, slotY, slotX, slotY);
            Assert.False(home.LeftSlot);
            Assert.Equal(slotX, home.CardX);
            Assert.Equal(slotY, home.CardY);
            Assert.Equal(slotX + UnitCardLayout.PortraitRestX, home.SpriteX);
            Assert.Equal(slotY + UnitCardLayout.PortraitRestY, home.SpriteY);
            Assert.Equal(-26f, UnitCardLayout.PortraitRestX);
            Assert.Equal(12f, UnitCardLayout.PortraitRestY);

            // One sim unit (64 px) toward the midline. The card does not follow.
            UnitLayerPlacement away = UnitCardLayout.Place(slotX, slotY, slotX + 64f, slotY);
            Assert.True(away.LeftSlot);
            Assert.Equal(home.CardX, away.CardX);
            Assert.Equal(home.CardY, away.CardY);
            Assert.Equal(home.SpriteX + 64f, away.SpriteX);
            Assert.Equal(home.SpriteY, away.SpriteY);
        }

        [Fact]
        public void EachCardStaysOnItsOwnSlotWhenBodiesMeet()
        {
            UnitLayerPlacement ally = UnitCardLayout.Place(-128f, 0f, 0f, 0f);
            UnitLayerPlacement enemy = UnitCardLayout.Place(128f, 96f, 0f, 0f);
            Assert.True(ally.LeftSlot);
            Assert.True(enemy.LeftSlot);
            Assert.Equal(-128f, ally.CardX);
            Assert.Equal(0f, ally.CardY);
            Assert.Equal(128f, enemy.CardX);
            Assert.Equal(96f, enemy.CardY);
            Assert.Equal(ally.SpriteX, enemy.SpriteX);
            Assert.Equal(ally.SpriteY, enemy.SpriteY);
            Assert.Equal(UnitCardLayout.PortraitRestX, ally.SpriteX);
            Assert.Equal(UnitCardLayout.PortraitRestY, ally.SpriteY);
        }

        [Fact]
        public void App_MovesThePortraitAndLeavesTheCardOnTheSlot()
        {
            string app = File.ReadAllText(Path.Combine(RepoRoot(), "Assets/Scripts/Presentation/TestArenaApp.cs"));
            int at = app.IndexOf("private void SyncCombatTokens()", StringComparison.Ordinal);
            Assert.True(at >= 0);
            int end = app.IndexOf("private bool Moving(", at, StringComparison.Ordinal);
            Assert.True(end > at);
            string sync = app.Substring(at, end - at);
            Assert.Contains("UnitCardLayout.Place", sync);
            Assert.Contains("place.CardX", sync);
            Assert.Contains("place.CardY", sync);
            Assert.Contains("place.LeftSlot", sync);
            Assert.Contains("place.SpriteX", sync);
            Assert.Contains("place.SpriteY", sync);
            Assert.Contains("_spriteLayer.SetAsLastSibling()", sync);
            Assert.DoesNotContain("anchoredPosition = screen - BoardPos", sync);
            Assert.DoesNotContain("rt.SetAsLastSibling()", sync);
            Assert.Contains("new Vector2(UnitCardLayout.PortraitRestX, UnitCardLayout.PortraitRestY), 32f", app);
            Assert.Contains("preserveAspect = true", app);
            Assert.Contains("Do not call SetNativeSize", app);
            Assert.DoesNotContain("SetNativeSize()", app);
            Assert.Contains("Box(_board, \"CombatSprites\", Vector2.zero, Vector2.zero, Clear, false)", app);
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "docs", "gdd", "m0-test-arena.md")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Repo root not found.");
        }
    }
}
